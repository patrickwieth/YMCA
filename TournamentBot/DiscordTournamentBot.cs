using Discord;
using Discord.WebSocket;

namespace Ymca.TournamentBot;

public sealed class DiscordTournamentBot : ITournamentNotifier, IAsyncDisposable
{
    readonly BotConfiguration config;
    readonly TournamentCoordinator coordinator;
    readonly JoinPageServer joinPage;
    readonly OfficialMapCatalog mapCatalog;
    readonly DiscordSocketClient client;
    readonly TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public DiscordTournamentBot(
        BotConfiguration config,
        TournamentCoordinator coordinator,
        JoinPageServer joinPage,
        OfficialMapCatalog mapCatalog)
    {
        this.config = config;
        this.coordinator = coordinator;
        this.joinPage = joinPage;
        this.mapCatalog = mapCatalog;
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
        client.ModalSubmitted += OnModalAsync;
        client.AutocompleteExecuted += OnAutocompleteAsync;
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
                .WithDescription("Add an official YMCA map to the shared tournament map pool.")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("map")
                    .WithDescription("Start typing an official map name")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true)
                    .WithAutocomplete(true))
                .Build(),
            new SlashCommandBuilder()
                .WithName("map-remove")
                .WithDescription("Remove a map from the shared tournament map pool.")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("map")
                    .WithDescription("Start typing a map-pool name")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true)
                    .WithAutocomplete(true))
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
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("mode")
                    .WithDescription("Player mode")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true)
                    .AddChoice("1v1", "1v1")
                    .AddChoice("2v2", "2v2"))
                .AddOption("spectators", ApplicationCommandOptionType.Boolean,
                    "Allow public spectator joins and announce live matches", true)
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-team-join")
                .WithDescription("Invite a teammate and enter an open 2v2 tournament.")
                .AddOption(TournamentOption("Choose an open 2v2 tournament", true))
                .AddOption("team-name", ApplicationCommandOptionType.String, "Your team name", true)
                .AddOption("teammate", ApplicationCommandOptionType.User, "Your teammate", true)
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-join")
                .WithDescription("Join an open tournament.")
                .AddOption(TournamentOption("Choose an open tournament", true))
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-leave")
                .WithDescription("Leave a tournament before it starts.")
                .AddOption(TournamentOption("Choose a tournament", true))
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-delete")
                .WithDescription("Delete a tournament that is not running.")
                .AddOption(TournamentOption("Choose a tournament to delete", true))
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-start")
                .WithDescription("Close registration and start a tournament.")
                .AddOption(TournamentOption("Choose an open tournament", true))
                .Build(),
            new SlashCommandBuilder()
                .WithName("tournament-status")
                .WithDescription("Show tournament standings and current state.")
                .AddOption(TournamentOption("Choose a tournament", false))
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
                case "tournament-team-join":
                    await JoinTournamentTeamAsync(command);
                    break;
                case "tournament-leave":
                    await LeaveTournamentAsync(command);
                    break;
                case "tournament-delete":
                    await DeleteTournamentAsync(command);
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
        var selected = mapCatalog.Get(GetString(command, "map"))
            ?? throw new InvalidOperationException("Select an official YMCA map from the autocomplete list.");
        var map = await coordinator.AddMapAsync(selected.Uid, selected.Title, selected.PlayerCount);
        await command.RespondAsync(
            $"Added **{Escape(map.Title)}** (`{map.Uid}`) to the tournament map pool.",
            ephemeral: true);
    }

    async Task RemoveMapAsync(SocketSlashCommand command)
    {
        EnsureAdmin(command.User);
        var map = await coordinator.RemoveMapAsync(GetString(command, "map"));
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
        var mode = GetString(command, "mode").Equals("2v2", StringComparison.OrdinalIgnoreCase)
            ? TournamentMode.TwoVsTwo
            : TournamentMode.OneVsOne;
        var allowSpectators = GetBool(command, "spectators");
        var tournament = await coordinator.CreateTournamentAsync(name, format, mode, allowSpectators);

        await command.RespondAsync(
            $"Tournament **{Escape(tournament.Name)}** (`{tournament.Id}`) created in the announcements channel.",
            ephemeral: true);
        var joinButton = new ComponentBuilder()
            .WithButton("Join tournament", $"tournament:{tournament.Id}:join", ButtonStyle.Success)
            .Build();
        await SendAnnouncementAsync(
            $"🏆 **{Escape(tournament.Name)}** is open for registration!\n" +
            $"Format: **{FormatTournamentFormat(tournament.Format)}** — **{FormatTournamentMode(tournament.Mode)}**\n" +
            $"Public spectators: **{(tournament.AllowSpectators ? "Yes" : "No")}**\n" +
            "Each round uses one randomly drawn map from the shared map pool.\n\n" +
            (tournament.Mode == TournamentMode.TwoVsTwo
                ? "Create a team using `/tournament-team-join`; your teammate must accept the invitation.\n"
                : "Press **Join tournament** below. If needed, the bot will ask for your exact YMCA/OpenRA player name.\n") +
            $"Tournament ID: `{tournament.Id}`",
            joinButton);
    }

    async Task JoinTournamentAsync(SocketSlashCommand command)
    {
        var tournamentId = GetString(command, "tournament-id");
        if (await coordinator.GetPlayerAsync(command.User.Id) == null)
        {
            var registrationButton = new ComponentBuilder()
                .WithButton("Register player name and join", $"register-join:{tournamentId}", ButtonStyle.Success)
                .Build();
            await command.RespondAsync(
                "Register your exact YMCA/OpenRA player name before joining.",
                components: registrationButton,
                ephemeral: true);
            return;
        }

        var tournament = await coordinator.JoinTournamentAsync(tournamentId, command.User.Id);
        await command.RespondAsync(
            $"You joined **{Escape(tournament.Name)}** (`{tournament.Id}`). Entrants: **{tournament.Entrants.Count}**.",
            ephemeral: true);
    }

    async Task JoinTournamentTeamAsync(SocketSlashCommand command)
    {
        var tournamentId = GetString(command, "tournament-id");
        var teammate = GetUser(command, "teammate");
        var team = await coordinator.InviteTeamAsync(
            tournamentId,
            command.User.Id,
            teammate.Id,
            GetString(command, "team-name"));
        var tournament = await coordinator.GetTournamentAsync(tournamentId)
            ?? throw new InvalidOperationException("Tournament not found.");
        var components = new ComponentBuilder()
            .WithButton("Accept team", $"team:{tournament.Id}:{command.User.Id}:accept", ButtonStyle.Success)
            .WithButton("Decline", $"team:{tournament.Id}:{command.User.Id}:decline", ButtonStyle.Danger)
            .Build();
        try
        {
            await SendDmAsync(teammate.Id,
                $"{Mention(command.User.Id)} invited you to join team **{Escape(team.Name)}** for " +
                $"**{Escape(tournament.Name)}** (`{tournament.Id}`).", components);
        }
        catch
        {
            await coordinator.RespondToTeamInviteAsync(tournament.Id, command.User.Id, teammate.Id, false);
            throw new InvalidOperationException("The teammate could not receive the invitation DM. Ask them to enable direct messages.");
        }

        await command.RespondAsync(
            $"Invitation sent to {teammate.Mention}. The team enters after they accept.", ephemeral: true);
    }

    async Task LeaveTournamentAsync(SocketSlashCommand command)
    {
        var tournament = await coordinator.LeaveTournamentAsync(GetString(command, "tournament-id"), command.User.Id);
        await command.RespondAsync(
            $"You left **{Escape(tournament.Name)}** (`{tournament.Id}`).",
            ephemeral: true);
    }

    async Task DeleteTournamentAsync(SocketSlashCommand command)
    {
        EnsureAdmin(command.User);
        var tournament = await coordinator.DeleteTournamentAsync(GetString(command, "tournament-id"));
        await command.RespondAsync(
            $"Tournament **{Escape(tournament.Name)}** (`{tournament.Id}`) was deleted.",
            ephemeral: true);
        await SendAnnouncementAsync(
            $"🗑️ Tournament **{Escape(tournament.Name)}** (`{tournament.Id}`) was removed by an administrator.");
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

        string DisplayEntrant(ulong id) => tournament.Mode == TournamentMode.TwoVsTwo
            && tournament.Teams.TryGetValue(id, out var team)
                ? $"**{Escape(team.Name)}** ({Mention(team.CaptainDiscordId)} + {Mention(team.TeammateDiscordId)})"
                : Mention(id);
        var eliminationLosses = tournament.Format == TournamentFormat.DoubleElimination ? 2 : 1;
        var entrants = tournament.Entrants.Count == 0
            ? "No entrants yet."
            : string.Join('\n', tournament.Entrants.Select(player =>
            {
                var losses = tournament.Losses.GetValueOrDefault(player);
                var state = tournament.Status == TournamentStatus.Registration
                    ? "registered"
                    : losses >= eliminationLosses ? "eliminated" : $"{losses} loss(es)";
                return $"• {DisplayEntrant(player)} — {state}";
            }));
        var podium = tournament.ChampionDiscordId is ulong championId
            ? $"\n🥇 {DisplayEntrant(championId)}"
            : "";
        if (tournament.RunnerUpDiscordId is ulong runnerUpId)
            podium += $"\n🥈 {DisplayEntrant(runnerUpId)}";
        if (tournament.ThirdPlaceDiscordId is ulong thirdPlaceId)
            podium += $"\n🥉 {DisplayEntrant(thirdPlaceId)}";
        var mapStatus = tournament.RoundNumber > 0
            ? $"Round {tournament.RoundNumber} map: **{Escape(tournament.MapTitle)}**"
            : $"Map pool: **{tournament.MapPool.Count}** map(s) will be snapshotted when the tournament starts";
        var text = $"**{Escape(tournament.Name)}** (`{tournament.Id}`)\n" +
            $"Format: **{FormatTournamentFormat(tournament.Format)}** — **{FormatTournamentMode(tournament.Mode)}**\n" +
            $"Public spectators: **{(tournament.AllowSpectators ? "Yes" : "No")}**\n" +
            $"Status: **{tournament.Status}**\n{mapStatus}\n" +
            $"Entrants: **{tournament.Entrants.Count}**\n{entrants}{podium}";
        await command.RespondAsync(text.Length <= 2000 ? text : text[..1997] + "...", ephemeral: true);
    }

    async Task OnAutocompleteAsync(SocketAutocompleteInteraction interaction)
    {
        try
        {
            var query = interaction.Data.Current.Value?.ToString()?.Trim() ?? "";
            if (interaction.Data.CommandName.StartsWith("tournament-", StringComparison.Ordinal))
            {
                IEnumerable<TournamentRecord> tournaments = await coordinator.GetTournamentsAsync();
                tournaments = interaction.Data.CommandName switch
                {
                    "tournament-join" or "tournament-team-join" or "tournament-start" => tournaments.Where(value => value.Status == TournamentStatus.Registration),
                    "tournament-leave" => tournaments.Where(value => value.Status == TournamentStatus.Registration),
                    _ => tournaments
                };

                var tournamentResults = tournaments
                    .Where(value => query.Length == 0
                        || value.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || value.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(value => value.Status == TournamentStatus.Registration)
                    .ThenByDescending(value => value.CreatedAtUtc)
                    .Take(25)
                    .Select(value => new AutocompleteResult(
                        Truncate($"{value.Id} — {value.Name} ({value.Status})", 100),
                        value.Id));
                await interaction.RespondAsync(tournamentResults);
                return;
            }

            IEnumerable<OfficialMap> candidates;
            if (interaction.Data.CommandName == "map-add")
            {
                var pool = await coordinator.GetMapPoolAsync();
                candidates = mapCatalog.Maps.Where(map =>
                    pool.All(existing => !existing.Uid.Equals(map.Uid, StringComparison.OrdinalIgnoreCase)));
            }
            else if (interaction.Data.CommandName == "map-remove")
            {
                var pool = await coordinator.GetMapPoolAsync();
                candidates = pool.Select(map => mapCatalog.Get(map.Uid)
                    ?? new OfficialMap(map.Uid, 0, map.Title));
            }
            else
            {
                await interaction.RespondAsync(Array.Empty<AutocompleteResult>());
                return;
            }

            var results = candidates
                .Where(map => query.Length == 0 || map.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(map => map.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenBy(map => map.Title, StringComparer.OrdinalIgnoreCase)
                .Take(25)
                .Select(map => new AutocompleteResult(
                    Truncate($"{map.Title} ({map.PlayerCount} players)", 100),
                    map.Uid));
            await interaction.RespondAsync(results);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Map autocomplete failed: {ex}");
            await interaction.RespondAsync(Array.Empty<AutocompleteResult>());
        }
    }

    async Task OnButtonAsync(SocketMessageComponent component)
    {
        try
        {
            var parts = component.Data.CustomId.Split(':');
            if (parts.Length == 2 && parts[0] == "register-join")
            {
                await ShowRegistrationModalAsync(component, parts[1]);
                return;
            }

            if (parts.Length == 4 && parts[0] == "team")
            {
                if (!ulong.TryParse(parts[2], out var captainId))
                    throw new InvalidOperationException("Invalid team invitation.");
                var accept = parts[3] == "accept";
                var response = await coordinator.RespondToTeamInviteAsync(parts[1], captainId, component.User.Id, accept);
                await component.RespondAsync(accept
                    ? $"Team **{Escape(response.Team.Name)}** joined **{Escape(response.Tournament.Name)}**."
                    : $"You declined the invitation to **{Escape(response.Team.Name)}**.", ephemeral: true);
                return;
            }

            if (parts.Length == 3 && parts[0] == "tournament" && parts[2] == "join")
            {
                var selectedTournament = await coordinator.GetTournamentAsync(parts[1])
                    ?? throw new InvalidOperationException("Tournament not found.");
                if (selectedTournament.Mode == TournamentMode.TwoVsTwo)
                {
                    await component.RespondAsync(
                        "Use `/tournament-team-join` to choose a team name and invite your teammate.", ephemeral: true);
                    return;
                }

                if (await coordinator.GetPlayerAsync(component.User.Id) == null)
                {
                    await ShowRegistrationModalAsync(component, parts[1]);
                    return;
                }

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

    static Task ShowRegistrationModalAsync(SocketMessageComponent component, string tournamentId)
    {
        var modal = new ModalBuilder()
            .WithTitle("Register YMCA player name")
            .WithCustomId($"registration:{tournamentId}")
            .AddTextInput(
                "Exact in-game player name",
                "openra-name",
                TextInputStyle.Short,
                "Enter the name shown in YMCA",
                minLength: 1,
                maxLength: 32,
                required: true)
            .Build();
        return component.RespondWithModalAsync(modal);
    }

    async Task OnModalAsync(SocketModal modal)
    {
        try
        {
            var parts = modal.Data.CustomId.Split(':');
            if (parts.Length != 2 || parts[0] != "registration")
                throw new InvalidOperationException("Invalid registration form.");

            var openRaName = modal.Data.Components
                .First(component => component.CustomId == "openra-name")
                .Value;
            await coordinator.RegisterAsync(
                modal.User.Id,
                modal.User.GlobalName ?? modal.User.Username,
                openRaName);
            var tournament = await coordinator.JoinTournamentAsync(parts[1], modal.User.Id);
            await modal.RespondAsync(
                $"Registered as **{Escape(openRaName)}** and joined **{Escape(tournament.Name)}** (`{tournament.Id}`). " +
                $"Entrants: **{tournament.Entrants.Count}**.",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            await modal.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    public Task MatchQueuedAsync(MatchRecord match) => SendAdminAsync(
        $"Match **{match.Id}** queued: {MatchSide(match, true)} vs {MatchSide(match, false)} on **{Escape(match.MapTitle)}**.");

    public async Task ServerReadyAsync(MatchRecord match, string joinUri)
    {
        string MessageFor(string opponent, string playerName) =>
            $"**YMCA tournament {(match.IsThirdPlaceMatch ? "third-place " : "")}match {match.Id}**\n" +
            $"Opponent: {opponent}\n" +
            $"Map: **{Escape(match.MapTitle)}**\n" +
            $"Join using this exact player name: `{playerName}`\n" +
            $"Server: `{config.Server.PublicHost}:{match.Port}`\n" +
            $"Password: `{match.Password}`\n\n" +
            $"Join link: `{joinUri}&name={Uri.EscapeDataString(playerName)}`\n\n" +
            "Manual join: YMCA → Multiplayer → Direct Connect, then enter the player name, server, and password shown above.";

        MessageComponent? ComponentsFor(ulong playerId) => !config.JoinPage.Enabled
            ? null
            : new ComponentBuilder()
                .WithButton("Join YMCA server", style: ButtonStyle.Link, url: joinPage.GetPublicJoinUrl(match.Id, playerId))
                .Build();

        await SendDmAsync(match.PlayerOneDiscordId, MessageFor(MatchSide(match, false), match.PlayerOneOpenRaName),
            ComponentsFor(match.PlayerOneDiscordId));
        await SendDmAsync(match.PlayerTwoDiscordId, MessageFor(MatchSide(match, true), match.PlayerTwoOpenRaName),
            ComponentsFor(match.PlayerTwoDiscordId));
        if (match.PlayerOneTeammateDiscordId is ulong firstTeammate)
            await SendDmAsync(firstTeammate, MessageFor(MatchSide(match, false), match.PlayerOneTeammateOpenRaName),
                ComponentsFor(firstTeammate));
        if (match.PlayerTwoTeammateDiscordId is ulong secondTeammate)
            await SendDmAsync(secondTeammate, MessageFor(MatchSide(match, true), match.PlayerTwoTeammateOpenRaName),
                ComponentsFor(secondTeammate));
        await SendAdminAsync($"Server for **{match.Id}** is ready on `{config.Server.PublicHost}:{match.Port}`.");

        if (match.AllowSpectators && config.JoinPage.Enabled)
        {
            var spectatorComponents = new ComponentBuilder()
                .WithButton("Join as spectator", style: ButtonStyle.Link,
                    url: joinPage.GetPublicSpectatorUrl(match.Id))
                .Build();
            await SendAnnouncementAsync(
                $"📺 **{match.Id}** is ready: {MatchSide(match, true)} vs {MatchSide(match, false)}\n" +
                $"Map: **{Escape(match.MapTitle)}**\nJoin as spectator before the match starts.",
                spectatorComponents);
        }
    }

    public async Task ResultReadyAsync(MatchRecord match, ReplayResult result)
    {
        var automaticResult = match.AutomaticWinnerDiscordId is ulong winner
            ? $"OpenRA reports {(winner == match.PlayerOneDiscordId ? MatchSide(match, true) : MatchSide(match, false))} as winner."
            : "OpenRA could not determine an unambiguous winner. Both reports will be checked manually if necessary.";

        var components = new ComponentBuilder()
            .WithButton("I won", $"match:{match.Id}:Won", ButtonStyle.Success)
            .WithButton("I lost", $"match:{match.Id}:Lost", ButtonStyle.Secondary)
            .WithButton("Request rematch", $"match:{match.Id}:Rematch", ButtonStyle.Primary)
            .WithButton("Dispute", $"match:{match.Id}:Dispute", ButtonStyle.Danger)
            .Build();

        var text = $"**Result for match {match.Id}**\n{automaticResult}\nPlease report your result.";
        foreach (var participant in MatchParticipantIds(match))
            await SendDmAsync(participant, text, components);
        await SendAdminAsync($"Match **{match.Id}** is awaiting player confirmation. Replay: `{result.ReplayPath}`");
    }

    public async Task MatchCompletedAsync(MatchRecord match)
    {
        var isRematch = match.Status == MatchStatus.RematchRequested;
        var winner = match.FinalWinnerDiscordId == match.PlayerOneDiscordId
            ? MatchSide(match, true)
            : MatchSide(match, false);
        var replayUrl = config.JoinPage.Enabled && !string.IsNullOrEmpty(match.ReplayPath)
            ? joinPage.GetPublicReplayUrl(match.Id)
            : null;
        var text = isRematch
            ? $"Both players requested a rematch for **{match.Id}**. A new match was queued."
            : $"Match **{match.Id}** completed. Winner: {winner}." +
                (replayUrl == null ? "" : $"\nReplay: {replayUrl}");
        await SendAdminAsync(text);

        if (!isRematch)
            await SendAnnouncementAsync($"🏁 **{match.Id}** — Winner: {winner}" +
                (replayUrl == null ? "" : $"\n▶️ [Watch replay in YMCA]({replayUrl})"));
    }

    public Task MatchDisputedAsync(MatchRecord match, string reason) =>
        SendAdminAsync($"⚠️ Match **{match.Id}** requires manual review: {reason}");

    public Task MatchFailedAsync(MatchRecord match, string reason) =>
        SendAdminAsync($"❌ Server for match **{match.Id}** failed: {Escape(reason)}");

    public Task TournamentUpdatedAsync(TournamentRecord tournament, IReadOnlyList<MatchRecord> newMatches)
    {
        var pairings = string.Join('\n', newMatches.Select(match =>
            $"• `{match.Id}`: {MatchSide(match, true)} vs {MatchSide(match, false)}" +
            (match.IsThirdPlaceMatch ? " — **Third-place playoff**" : "")));
        return SendAnnouncementAsync(
            $"⚔️ **{Escape(tournament.Name)}** — Round {tournament.RoundNumber}\n" +
            $"Map for every match this round: **{Escape(tournament.MapTitle)}**\n{pairings}\n\n" +
            "Players will receive their server details by DM.");
    }

    public Task TournamentCompletedAsync(TournamentRecord tournament)
    {
        string Entrant(ulong id) => tournament.Mode == TournamentMode.TwoVsTwo
            && tournament.Teams.TryGetValue(id, out var team)
                ? $"**{Escape(team.Name)}** ({Mention(team.CaptainDiscordId)} + {Mention(team.TeammateDiscordId)})"
                : Mention(id);
        var podium = $"🥇 {Entrant(tournament.ChampionDiscordId ?? 0)}";
        if (tournament.RunnerUpDiscordId is ulong runnerUp)
            podium += $"\n🥈 {Entrant(runnerUp)}";
        if (tournament.ThirdPlaceDiscordId is ulong thirdPlace)
            podium += $"\n🥉 {Entrant(thirdPlace)}";

        return SendAnnouncementAsync(
            $"🏆 Tournament **{Escape(tournament.Name)}** (`{tournament.Id}`) completed!\n\n{podium}");
    }

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

    static bool GetBool(SocketSlashCommand command, string name) =>
        (bool)(command.Data.Options.First(option => option.Name == name).Value
            ?? throw new InvalidOperationException($"Missing option {name}."));

    static string Mention(ulong userId) => userId == 0 ? "unknown" : $"<@{userId}>";
    static string Escape(string text) => Format.Sanitize(text);
    static SlashCommandOptionBuilder TournamentOption(string description, bool required) =>
        new SlashCommandOptionBuilder()
            .WithName("tournament-id")
            .WithDescription(description)
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(required)
            .WithAutocomplete(true);

    static IEnumerable<ulong> MatchParticipantIds(MatchRecord match)
    {
        yield return match.PlayerOneDiscordId;
        if (match.PlayerOneTeammateDiscordId is ulong firstTeammate)
            yield return firstTeammate;
        yield return match.PlayerTwoDiscordId;
        if (match.PlayerTwoTeammateDiscordId is ulong secondTeammate)
            yield return secondTeammate;
    }

    static string MatchSide(MatchRecord match, bool first)
    {
        var captain = first ? match.PlayerOneDiscordId : match.PlayerTwoDiscordId;
        var teammate = first ? match.PlayerOneTeammateDiscordId : match.PlayerTwoTeammateDiscordId;
        var name = first ? match.PlayerOneTeamName : match.PlayerTwoTeamName;
        return teammate == null ? Mention(captain) : $"**{Escape(name)}** ({Mention(captain)} + {Mention(teammate.Value)})";
    }

    static string FormatTournamentFormat(TournamentFormat format) => format == TournamentFormat.DoubleElimination
        ? "Double Elimination"
        : "Single Elimination";
    static string FormatTournamentMode(TournamentMode mode) => mode == TournamentMode.TwoVsTwo ? "2v2" : "1v1";
    static string Truncate(string text, int length) => text.Length <= length ? text : text[..(length - 3)] + "...";

    public async ValueTask DisposeAsync()
    {
        await client.DisposeAsync();
    }
}
