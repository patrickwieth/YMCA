using Discord;
using Discord.WebSocket;

namespace Ymca.TournamentBot;

public sealed class DiscordTournamentBot : ITournamentNotifier, IAsyncDisposable
{
    readonly BotConfiguration config;
    readonly TournamentCoordinator coordinator;
    readonly JoinPageServer joinPage;
    readonly DiscordSocketClient client;
    readonly TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public DiscordTournamentBot(BotConfiguration config, TournamentCoordinator coordinator, JoinPageServer joinPage)
    {
        this.config = config;
        this.coordinator = coordinator;
        this.joinPage = joinPage;
        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds,
            LogGatewayIntentWarnings = false
        });

        client.Log += message =>
        {
            Console.WriteLine($"[{DateTime.Now:O}] Discord {message.Severity}: {message.Message} {message.Exception}");
            return Task.CompletedTask;
        };
        client.Ready += OnReadyAsync;
        client.SlashCommandExecuted += OnSlashCommandAsync;
        client.ButtonExecuted += OnButtonAsync;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        coordinator.SetNotifier(this);
        await client.LoginAsync(TokenType.Bot, config.DiscordToken);
        await client.StartAsync();
        await coordinator.StartAsync();

        using var registration = cancellationToken.Register(() => stopped.TrySetResult());
        await stopped.Task;
        await client.StopAsync();
    }

    async Task OnReadyAsync()
    {
        var guild = client.GetGuild(config.GuildId)
            ?? throw new InvalidOperationException($"Discord guild {config.GuildId} is unavailable.");

        var commands = new ApplicationCommandProperties[]
        {
            new SlashCommandBuilder()
                .WithName("register")
                .WithDescription("Register your OpenRA player name for the tournament.")
                .AddOption("openra-name", ApplicationCommandOptionType.String, "Your exact in-game player name", true)
                .Build(),
            new SlashCommandBuilder()
                .WithName("match")
                .WithDescription("Create and queue a tournament match.")
                .AddOption("player-one", ApplicationCommandOptionType.User, "First player", true)
                .AddOption("player-two", ApplicationCommandOptionType.User, "Second player", true)
                .AddOption("map-uid", ApplicationCommandOptionType.String, "OpenRA map UID", true)
                .AddOption("map-title", ApplicationCommandOptionType.String, "Human-readable map title", false)
                .Build(),
            new SlashCommandBuilder()
                .WithName("matches")
                .WithDescription("Show recent tournament matches.")
                .Build(),
            new SlashCommandBuilder()
                .WithName("resolve")
                .WithDescription("Resolve a disputed match as tournament admin.")
                .AddOption("match-id", ApplicationCommandOptionType.String, "Match ID", true)
                .AddOption("winner", ApplicationCommandOptionType.User, "Winning player", true)
                .Build(),
            new SlashCommandBuilder()
                .WithName("map-add")
                .WithDescription("Add a map to the shared tournament map pool.")
                .AddOption("map-uid", ApplicationCommandOptionType.String, "OpenRA map UID", true)
                .AddOption("map-title", ApplicationCommandOptionType.String, "Human-readable map title", true)
                .Build(),
            new SlashCommandBuilder()
                .WithName("map-remove")
                .WithDescription("Remove a map from the shared tournament map pool.")
                .AddOption("map-uid", ApplicationCommandOptionType.String, "OpenRA map UID", true)
                .Build(),
            new SlashCommandBuilder()
                .WithName("map-pool")
                .WithDescription("Show the shared tournament map pool.")
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-create")
                .WithDescription("Create a single- or double-elimination tournament.")
                .AddOption("name", ApplicationCommandOptionType.String, "Tournament name", true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("format")
                    .WithDescription("Elimination format")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true)
                    .AddChoice("Single Elimination", "single")
                    .AddChoice("Double Elimination", "double"))
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-join")
                .WithDescription("Join an open tournament.")
                .AddOption("tournament-id", ApplicationCommandOptionType.String, "Tournament ID, for example T001", true)
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-leave")
                .WithDescription("Leave a tournament before it starts.")
                .AddOption("tournament-id", ApplicationCommandOptionType.String, "Tournament ID", true)
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-start")
                .WithDescription("Close registration and start a tournament.")
                .AddOption("tournament-id", ApplicationCommandOptionType.String, "Tournament ID", true)
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-status")
                .WithDescription("Show tournament standings and current state.")
                .AddOption("tournament-id", ApplicationCommandOptionType.String, "Tournament ID", false)
                .Build()
        };

        await guild.BulkOverwriteApplicationCommandAsync(commands);
        Console.WriteLine($"Registered tournament commands in {guild.Name}.");
    }

    async Task OnSlashCommandAsync(SocketSlashCommand command)
    {
        try
        {
            switch (command.Data.Name)
            {
                case "register":
                    await RegisterAsync(command);
                    break;
                case "match":
                    await CreateMatchAsync(command);
                    break;
                case "matches":
                    await ShowMatchesAsync(command);
                    break;
                case "resolve":
                    await ResolveAsync(command);
                    break;
                case "map-add":
                    await AddMapAsync(command);
                    break;
                case "map-remove":
                    await RemoveMapAsync(command);
                    break;
                case "map-pool":
                    await ShowMapPoolAsync(command);
                    break;
                case "tournament-create":
                    await CreateTournamentAsync(command);
                    break;
                case "tournament-join":
                    await JoinTournamentAsync(command);
                    break;
                case "tournament-leave":
                    await LeaveTournamentAsync(command);
                    break;
                case "tournament-start":
                    await StartTournamentAsync(command);
                    break;
                case "tournament-status":
                    await ShowTournamentAsync(command);
                    break;
            }
        }
        catch (Exception ex)
        {
            if (command.HasResponded)
                await command.FollowupAsync($"Error: {ex.Message}", ephemeral: true);
            else
                await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    async Task RegisterAsync(SocketSlashCommand command)
    {
        var name = GetString(command, "openra-name");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 32)
            throw new InvalidOperationException("The OpenRA name must contain between 1 and 32 characters.");

        await coordinator.RegisterAsync(command.User.Id, command.User.GlobalName ?? command.User.Username, name);
        await command.RespondAsync($"Registered as OpenRA player **{Escape(name)}**.", ephemeral: true);
    }

    async Task CreateMatchAsync(SocketSlashCommand command)
    {
        EnsureAdmin(command.User);
        var playerOne = GetUser(command, "player-one");
        var playerTwo = GetUser(command, "player-two");
        var mapUid = GetString(command, "map-uid");
        var mapTitle = GetOptionalString(command, "map-title") ?? mapUid;

        var match = await coordinator.CreateMatchAsync(playerOne.Id, playerTwo.Id, mapUid, mapTitle);
        await command.RespondAsync($"Match **{match.Id}** queued.", ephemeral: true);
    }

    async Task ShowMatchesAsync(SocketSlashCommand command)
    {
        var matches = await coordinator.GetRecentMatchesAsync();
        var text = matches.Count == 0
            ? "No matches have been created."
            : string.Join('\n', matches.Select(match =>
                $"`{match.Id}` {Mention(match.PlayerOneDiscordId)} vs {Mention(match.PlayerTwoDiscordId)} — **{match.Status}**"));
        await command.RespondAsync(text, ephemeral: true);
    }

    async Task ResolveAsync(SocketSlashCommand command)
    {
        EnsureAdmin(command.User);
        var matchId = GetString(command, "match-id");
        var winner = GetUser(command, "winner");
        await coordinator.ResolveAsync(matchId, winner.Id);
        await command.RespondAsync($"Match **{matchId}** resolved for {winner.Mention}.", ephemeral: true);
    }

    async Task AddMapAsync(SocketSlashCommand command)
    {
        EnsureAdmin(command.User);
        var map = await coordinator.AddMapAsync(GetString(command, "map-uid"), GetString(command, "map-title"));
        await command.RespondAsync(
            $"Added **{Escape(map.Title)}** (`{map.Uid}`) to the tournament map pool.",
            ephemeral: true);
    }

    async Task RemoveMapAsync(SocketSlashCommand command)
    {
        EnsureAdmin(command.User);
        var map = await coordinator.RemoveMapAsync(GetString(command, "map-uid"));
        await command.RespondAsync(
            $"Removed **{Escape(map.Title)}** from the tournament map pool.",
            ephemeral: true);
    }

    async Task ShowMapPoolAsync(SocketSlashCommand command)
    {
        var maps = await coordinator.GetMapPoolAsync();
        var text = maps.Count == 0
            ? "The tournament map pool is empty."
            : "**Tournament map pool**\n" + string.Join('\n', maps.Select(map => $"• **{Escape(map.Title)}** — `{map.Uid}`"));
        await command.RespondAsync(text.Length <= 2000 ? text : text[..1997] + "...", ephemeral: true);
    }

    async Task CreateTournamentAsync(SocketSlashCommand command)
    {
        EnsureAdmin(command.User);
        var name = GetString(command, "name");
        var format = GetString(command, "format").Equals("double", StringComparison.OrdinalIgnoreCase)
            ? TournamentFormat.DoubleElimination
            : TournamentFormat.SingleElimination;
        var tournament = await coordinator.CreateTournamentAsync(name, format);

        await command.RespondAsync(
            $"Tournament **{Escape(tournament.Name)}** (`{tournament.Id}`) created in the announcements channel.",
            ephemeral: true);
        var joinButton = new ComponentBuilder()
            .WithButton("Join tournament", $"tournament:{tournament.Id}:join", ButtonStyle.Success)
            .Build();
        await SendAnnouncementAsync(
            $"🏆 **{Escape(tournament.Name)}** is open for registration!\n" +
            $"Format: **{FormatTournamentFormat(tournament.Format)}**\n" +
            "Each round uses one randomly drawn map from the shared map pool.\n\n" +
            "Register your exact YMCA player name with `/register`, then press the button below to enter.\n" +
            $"Tournament ID: `{tournament.Id}`",
            joinButton);
    }

    async Task JoinTournamentAsync(SocketSlashCommand command)
    {
        var tournament = await coordinator.JoinTournamentAsync(GetString(command, "tournament-id"), command.User.Id);
        await command.RespondAsync(
            $"You joined **{Escape(tournament.Name)}** (`{tournament.Id}`). Entrants: **{tournament.Entrants.Count}**.",
            ephemeral: true);
    }

    async Task LeaveTournamentAsync(SocketSlashCommand command)
    {
        var tournament = await coordinator.LeaveTournamentAsync(GetString(command, "tournament-id"), command.User.Id);
        await command.RespondAsync(
            $"You left **{Escape(tournament.Name)}** (`{tournament.Id}`).",
            ephemeral: true);
    }

    async Task StartTournamentAsync(SocketSlashCommand command)
    {
        EnsureAdmin(command.User);
        var tournament = await coordinator.StartTournamentAsync(GetString(command, "tournament-id"));
        await command.RespondAsync(
            $"Tournament **{Escape(tournament.Name)}** (`{tournament.Id}`) started with " +
            $"**{tournament.Entrants.Count}** players.");
    }

    async Task ShowTournamentAsync(SocketSlashCommand command)
    {
        var requestedId = GetOptionalString(command, "tournament-id");
        TournamentRecord? tournament;
        if (requestedId != null)
            tournament = await coordinator.GetTournamentAsync(requestedId);
        else
            tournament = (await coordinator.GetTournamentsAsync()).FirstOrDefault();

        if (tournament == null)
        {
            await command.RespondAsync("No tournament has been created.", ephemeral: true);
            return;
        }

        var eliminationLosses = tournament.Format == TournamentFormat.DoubleElimination ? 2 : 1;
        var entrants = tournament.Entrants.Count == 0
            ? "No entrants yet."
            : string.Join('\n', tournament.Entrants.Select(player =>
            {
                var losses = tournament.Losses.GetValueOrDefault(player);
                var state = tournament.Status == TournamentStatus.Registration
                    ? "registered"
                    : losses >= eliminationLosses ? "eliminated" : $"{losses} loss(es)";
                return $"• {Mention(player)} — {state}";
            }));
        var champion = tournament.ChampionDiscordId is ulong championId
            ? $"\nChampion: {Mention(championId)}"
            : "";
        var mapStatus = tournament.RoundNumber > 0
            ? $"Round {tournament.RoundNumber} map: **{Escape(tournament.MapTitle)}**"
            : $"Map pool: **{tournament.MapPool.Count}** map(s) will be snapshotted when the tournament starts";
        var text = $"**{Escape(tournament.Name)}** (`{tournament.Id}`)\n" +
            $"Format: **{FormatTournamentFormat(tournament.Format)}**\n" +
            $"Status: **{tournament.Status}**\n{mapStatus}\n" +
            $"Entrants: **{tournament.Entrants.Count}**\n{entrants}{champion}";
        await command.RespondAsync(text.Length <= 2000 ? text : text[..1997] + "...", ephemeral: true);
    }

    async Task OnButtonAsync(SocketMessageComponent component)
    {
        try
        {
            var parts = component.Data.CustomId.Split(':');
            if (parts.Length == 3 && parts[0] == "tournament" && parts[2] == "join")
            {
                var tournament = await coordinator.JoinTournamentAsync(parts[1], component.User.Id);
                await component.RespondAsync(
                    $"You joined **{Escape(tournament.Name)}** (`{tournament.Id}`). " +
                    $"Entrants: **{tournament.Entrants.Count}**.",
                    ephemeral: true);
                return;
            }

            if (parts.Length != 3 || parts[0] != "match" || !Enum.TryParse<PlayerReport>(parts[2], true, out var report))
                throw new InvalidOperationException("Invalid tournament action.");

            await coordinator.SubmitReportAsync(parts[1], component.User.Id, report);
            await component.RespondAsync($"Your response **{report}** was recorded for match **{parts[1]}**.", ephemeral: true);
        }
        catch (Exception ex)
        {
            await component.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    public Task MatchQueuedAsync(MatchRecord match) => SendAdminAsync(
        $"Match **{match.Id}** queued: {Mention(match.PlayerOneDiscordId)} vs {Mention(match.PlayerTwoDiscordId)} on **{Escape(match.MapTitle)}**.");

    public async Task ServerReadyAsync(MatchRecord match, string joinUri)
    {
        string MessageFor(ulong opponentId) =>
            $"**YMCA tournament match {match.Id}**\n" +
            $"Opponent: {Mention(opponentId)}\n" +
            $"Map: **{Escape(match.MapTitle)}**\n" +
            $"Server: `{config.Server.PublicHost}:{match.Port}`\n" +
            $"Password: `{match.Password}`\n\n" +
            $"Join link: `{joinUri}`\n\n" +
            "Manual join: YMCA → Multiplayer → Direct Connect, then enter the server and password shown above.";

        MessageComponent? components = null;
        if (config.JoinPage.Enabled)
            components = new ComponentBuilder()
                .WithButton("Join YMCA server", style: ButtonStyle.Link, url: joinPage.GetPublicJoinUrl(match.Id))
                .Build();

        await SendDmAsync(match.PlayerOneDiscordId, MessageFor(match.PlayerTwoDiscordId), components);
        await SendDmAsync(match.PlayerTwoDiscordId, MessageFor(match.PlayerOneDiscordId), components);
        await SendAdminAsync($"Server for **{match.Id}** is ready on `{config.Server.PublicHost}:{match.Port}`.");
    }

    public async Task ResultReadyAsync(MatchRecord match, ReplayResult result)
    {
        var automaticResult = match.AutomaticWinnerDiscordId is ulong winner
            ? $"OpenRA reports {Mention(winner)} as winner."
            : "OpenRA could not determine an unambiguous winner. Both reports will be checked manually if necessary.";

        var components = new ComponentBuilder()
            .WithButton("I won", $"match:{match.Id}:Won", ButtonStyle.Success)
            .WithButton("I lost", $"match:{match.Id}:Lost", ButtonStyle.Secondary)
            .WithButton("Request rematch", $"match:{match.Id}:Rematch", ButtonStyle.Primary)
            .WithButton("Dispute", $"match:{match.Id}:Dispute", ButtonStyle.Danger)
            .Build();

        var text = $"**Result for match {match.Id}**\n{automaticResult}\nPlease report your result.";
        await SendDmAsync(match.PlayerOneDiscordId, text, components);
        await SendDmAsync(match.PlayerTwoDiscordId, text, components);
        await SendAdminAsync($"Match **{match.Id}** is awaiting player confirmation. Replay: `{result.ReplayPath}`");
    }

    public async Task MatchCompletedAsync(MatchRecord match)
    {
        var text = match.Status == MatchStatus.RematchRequested
            ? $"Both players requested a rematch for **{match.Id}**. A new match was queued."
            : $"Match **{match.Id}** completed. Winner: {Mention(match.FinalWinnerDiscordId ?? 0)}.";
        await SendAdminAsync(text);
    }

    public Task MatchDisputedAsync(MatchRecord match, string reason) =>
        SendAdminAsync($"⚠️ Match **{match.Id}** requires manual review: {reason}");

    public Task MatchFailedAsync(MatchRecord match, string reason) =>
        SendAdminAsync($"❌ Server for match **{match.Id}** failed: {Escape(reason)}");

    public Task TournamentUpdatedAsync(TournamentRecord tournament, IReadOnlyList<MatchRecord> newMatches)
    {
        var pairings = string.Join('\n', newMatches.Select(match =>
            $"• `{match.Id}`: {Mention(match.PlayerOneDiscordId)} vs {Mention(match.PlayerTwoDiscordId)}"));
        return SendAnnouncementAsync(
            $"⚔️ **{Escape(tournament.Name)}** — Round {tournament.RoundNumber}\n" +
            $"Map for every match this round: **{Escape(tournament.MapTitle)}**\n{pairings}\n\n" +
            "Players will receive their server details by DM.");
    }

    public Task TournamentCompletedAsync(TournamentRecord tournament) =>
        SendAnnouncementAsync(
            $"🏆 Tournament **{Escape(tournament.Name)}** (`{tournament.Id}`) completed!\n" +
            $"Champion: {Mention(tournament.ChampionDiscordId ?? 0)}");

    async Task SendDmAsync(ulong userId, string text, MessageComponent? components = null)
    {
        IUser? user = client.GetUser(userId);
        user ??= await client.Rest.GetUserAsync(userId);
        await user.SendMessageAsync(text, components: components);
    }

    async Task SendAdminAsync(string text)
    {
        if (config.AdminChannelId == 0)
        {
            Console.WriteLine($"[Tournament] {text}");
            return;
        }

        if (client.GetChannel(config.AdminChannelId) is not IMessageChannel channel)
            throw new InvalidOperationException($"Admin channel {config.AdminChannelId} is unavailable.");
        await channel.SendMessageAsync(text);
    }

    async Task SendAnnouncementAsync(string text, MessageComponent? components = null)
    {
        if (config.AnnouncementChannelId == 0)
        {
            await SendAdminAsync(text);
            return;
        }

        if (client.GetChannel(config.AnnouncementChannelId) is not IMessageChannel channel)
            throw new InvalidOperationException($"Announcement channel {config.AnnouncementChannelId} is unavailable.");
        await channel.SendMessageAsync(text, components: components);
    }

    void EnsureAdmin(SocketUser user)
    {
        if (user is not SocketGuildUser guildUser
            || !guildUser.GuildPermissions.Administrator
            && (config.AdminRoleId == 0 || guildUser.Roles.All(role => role.Id != config.AdminRoleId)))
            throw new InvalidOperationException("Tournament administrator permission required.");
    }

    static SocketUser GetUser(SocketSlashCommand command, string name) =>
        (SocketUser)(command.Data.Options.First(option => option.Name == name).Value
            ?? throw new InvalidOperationException($"Missing option {name}."));

    static string GetString(SocketSlashCommand command, string name) =>
        (string)(command.Data.Options.First(option => option.Name == name).Value
            ?? throw new InvalidOperationException($"Missing option {name}."));

    static string? GetOptionalString(SocketSlashCommand command, string name) =>
        command.Data.Options.FirstOrDefault(option => option.Name == name)?.Value as string;

    static string Mention(ulong userId) => userId == 0 ? "unknown" : $"<@{userId}>";
    static string Escape(string text) => Format.Sanitize(text);
    static string FormatTournamentFormat(TournamentFormat format) => format == TournamentFormat.DoubleElimination
        ? "Double Elimination"
        : "Single Elimination";

    public async ValueTask DisposeAsync()
    {
        await client.DisposeAsync();
    }
}
