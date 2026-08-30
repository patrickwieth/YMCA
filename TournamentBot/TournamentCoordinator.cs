namespace Ymca.TournamentBot;

public interface ITournamentNotifier
{
    Task MatchQueuedAsync(MatchRecord match);
    Task ServerReadyAsync(MatchRecord match, string joinUri);
    Task ResultReadyAsync(MatchRecord match, ReplayResult result);
    Task MatchCompletedAsync(MatchRecord match);
    Task MatchDisputedAsync(MatchRecord match, string reason);
    Task MatchFailedAsync(MatchRecord match, string reason);
}

public sealed class TournamentCoordinator
{
    readonly BotConfiguration config;
    readonly StateStore store;
    readonly OpenRaServerPool serverPool;
    ITournamentNotifier? notifier;

    public TournamentCoordinator(BotConfiguration config, StateStore store, OpenRaServerPool serverPool)
    {
        this.config = config;
        this.store = store;
        this.serverPool = serverPool;
        serverPool.ServerStarting += OnServerStartingAsync;
        serverPool.ServerReady += OnServerReadyAsync;
        serverPool.ResultAvailable += OnResultAvailableAsync;
        serverPool.ServerFailed += OnServerFailedAsync;
    }

    public void SetNotifier(ITournamentNotifier value) => notifier = value;

    public async Task StartAsync()
    {
        await store.LoadAsync();
        serverPool.Start();

        var unfinished = await store.UpdateAsync(state =>
        {
            var matches = state.Matches.Values
                .Where(match => match.Status is MatchStatus.Queued or MatchStatus.StartingServer
                    or MatchStatus.WaitingForPlayers or MatchStatus.Playing)
                .ToList();
            foreach (var match in matches)
            {
                match.Status = MatchStatus.Queued;
                match.Port = null;
                match.Password = "";
                match.FailureReason = null;
            }

            return matches;
        });

        foreach (var match in unfinished)
            await serverPool.EnqueueAsync(match);
    }

    public Task<RegisteredPlayer> RegisterAsync(ulong discordId, string displayName, string openRaName) =>
        store.UpdateAsync(state =>
        {
            if (state.Players.Values.Any(player => player.DiscordUserId != discordId
                && player.OpenRaName.Equals(openRaName.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("This OpenRA name is already registered by another Discord user.");

            var player = new RegisteredPlayer
            {
                DiscordUserId = discordId,
                DiscordDisplayName = displayName,
                OpenRaName = openRaName.Trim(),
                RegisteredAtUtc = DateTime.UtcNow
            };
            state.Players[discordId] = player;
            return player;
        });

    public async Task<MatchRecord> CreateMatchAsync(
        ulong playerOneId,
        ulong playerTwoId,
        string mapUid,
        string mapTitle,
        string? parentMatchId = null)
    {
        if (playerOneId == playerTwoId)
            throw new InvalidOperationException("A player cannot play against themselves.");
        if (string.IsNullOrWhiteSpace(mapUid))
            throw new InvalidOperationException("A map UID is required.");

        var match = await store.UpdateAsync(state =>
        {
            if (!state.Players.TryGetValue(playerOneId, out var playerOne))
                throw new InvalidOperationException("Player one is not registered.");
            if (!state.Players.TryGetValue(playerTwoId, out var playerTwo))
                throw new InvalidOperationException("Player two is not registered.");

            var id = $"M{state.NextMatchNumber++:0000}";
            var created = new MatchRecord
            {
                Id = id,
                PlayerOneDiscordId = playerOneId,
                PlayerTwoDiscordId = playerTwoId,
                PlayerOneOpenRaName = playerOne.OpenRaName,
                PlayerTwoOpenRaName = playerTwo.OpenRaName,
                MapUid = mapUid.Trim(),
                MapTitle = string.IsNullOrWhiteSpace(mapTitle) ? mapUid.Trim() : mapTitle.Trim(),
                Status = MatchStatus.Queued,
                ParentMatchId = parentMatchId,
                CreatedAtUtc = DateTime.UtcNow
            };
            state.Matches[id] = created;
            return created;
        });

        await serverPool.EnqueueAsync(match);
        await NotifyAsync(value => value.MatchQueuedAsync(match));
        return match;
    }

    public Task<RegisteredPlayer?> GetPlayerAsync(ulong discordId) =>
        store.ReadAsync(state => state.Players.GetValueOrDefault(discordId));

    public Task<MatchRecord?> GetMatchAsync(string id) =>
        store.ReadAsync(state => state.Matches.GetValueOrDefault(id.Trim().ToUpperInvariant()));

    public Task<IReadOnlyList<MatchRecord>> GetRecentMatchesAsync(int count = 10) =>
        store.ReadAsync<IReadOnlyList<MatchRecord>>(state => state.Matches.Values
            .OrderByDescending(match => match.CreatedAtUtc)
            .Take(count)
            .ToList());

    public async Task SubmitReportAsync(string matchId, ulong playerId, PlayerReport report)
    {
        var resolution = await store.UpdateAsync(state =>
        {
            if (!state.Matches.TryGetValue(matchId.Trim().ToUpperInvariant(), out var match))
                throw new InvalidOperationException("Match not found.");
            if (playerId != match.PlayerOneDiscordId && playerId != match.PlayerTwoDiscordId)
                throw new InvalidOperationException("Only match participants can report a result.");
            if (match.Status is MatchStatus.Completed or MatchStatus.Cancelled)
                throw new InvalidOperationException("This match has already been closed.");

            match.PlayerReports[playerId] = report;
            return EvaluateReports(match);
        });

        if (resolution.Match == null)
            return;

        if (resolution.CreateRematch)
        {
            await NotifyAsync(value => value.MatchCompletedAsync(resolution.Match));
            await CreateMatchAsync(
                resolution.Match.PlayerOneDiscordId,
                resolution.Match.PlayerTwoDiscordId,
                resolution.Match.MapUid,
                resolution.Match.MapTitle,
                resolution.Match.Id);
        }
        else if (resolution.Match.Status == MatchStatus.Completed)
            await NotifyAsync(value => value.MatchCompletedAsync(resolution.Match));
        else if (resolution.Match.Status == MatchStatus.Disputed)
            await NotifyAsync(value => value.MatchDisputedAsync(resolution.Match, "Player reports conflict or a player disputed the result."));
    }

    public async Task ResolveAsync(string matchId, ulong winnerId)
    {
        var match = await store.UpdateAsync(state =>
        {
            if (!state.Matches.TryGetValue(matchId.Trim().ToUpperInvariant(), out var existing))
                throw new InvalidOperationException("Match not found.");
            if (winnerId != existing.PlayerOneDiscordId && winnerId != existing.PlayerTwoDiscordId)
                throw new InvalidOperationException("The selected winner did not participate in this match.");

            existing.FinalWinnerDiscordId = winnerId;
            existing.Status = MatchStatus.Completed;
            existing.FinishedAtUtc = DateTime.UtcNow;
            return existing;
        });

        await NotifyAsync(value => value.MatchCompletedAsync(match));
    }

    Task OnServerStartingAsync(MatchRecord match) => store.UpdateAsync(state =>
    {
        var stored = state.Matches[match.Id];
        stored.Port = match.Port;
        stored.Password = match.Password;
        stored.SupportDirectory = match.SupportDirectory;
        stored.Status = MatchStatus.StartingServer;
    });

    async Task OnServerReadyAsync(MatchRecord match)
    {
        await store.UpdateAsync(state =>
        {
            var stored = state.Matches[match.Id];
            stored.Port = match.Port;
            stored.Password = match.Password;
            stored.SupportDirectory = match.SupportDirectory;
            stored.Status = MatchStatus.WaitingForPlayers;
            stored.StartedAtUtc = DateTime.UtcNow;
        });

        var joinUri = $"ymca://{config.Server.PublicHost}:{match.Port}?password={Uri.EscapeDataString(match.Password)}";
        await NotifyAsync(value => value.ServerReadyAsync(match, joinUri));
    }

    async Task OnResultAvailableAsync(MatchRecord match, ReplayResult result)
    {
        var updated = await store.UpdateAsync(state =>
        {
            var stored = state.Matches[match.Id];
            stored.ReplayPath = result.ReplayPath;
            stored.AutomaticWinnerDiscordId = DetermineWinner(stored, result);
            stored.Status = MatchStatus.AwaitingConfirmation;
            return stored;
        });

        await NotifyAsync(value => value.ResultReadyAsync(updated, result));
    }

    async Task OnServerFailedAsync(MatchRecord match, string reason)
    {
        var updated = await store.UpdateAsync(state =>
        {
            var stored = state.Matches[match.Id];
            stored.Status = MatchStatus.Failed;
            stored.FailureReason = reason;
            stored.FinishedAtUtc = DateTime.UtcNow;
            return stored;
        });

        await NotifyAsync(value => value.MatchFailedAsync(updated, reason));
    }

    async Task NotifyAsync(Func<ITournamentNotifier, Task> notification)
    {
        if (notifier == null)
            return;

        try
        {
            await notification(notifier);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Tournament notification failed: {ex}");
        }
    }

    static ulong? DetermineWinner(MatchRecord match, ReplayResult result)
    {
        var playerOne = result.Players.FirstOrDefault(player => player.IsHuman
            && player.Name.Equals(match.PlayerOneOpenRaName, StringComparison.OrdinalIgnoreCase));
        var playerTwo = result.Players.FirstOrDefault(player => player.IsHuman
            && player.Name.Equals(match.PlayerTwoOpenRaName, StringComparison.OrdinalIgnoreCase));

        if (playerOne?.Outcome.Equals("Won", StringComparison.OrdinalIgnoreCase) == true
            && playerTwo?.Outcome.Equals("Lost", StringComparison.OrdinalIgnoreCase) == true)
            return match.PlayerOneDiscordId;
        if (playerTwo?.Outcome.Equals("Won", StringComparison.OrdinalIgnoreCase) == true
            && playerOne?.Outcome.Equals("Lost", StringComparison.OrdinalIgnoreCase) == true)
            return match.PlayerTwoDiscordId;

        return null;
    }

    static ReportResolution EvaluateReports(MatchRecord match)
    {
        if (match.PlayerReports.Values.Any(report => report == PlayerReport.Dispute))
        {
            match.Status = MatchStatus.Disputed;
            return new ReportResolution(match, false);
        }

        if (!match.PlayerReports.TryGetValue(match.PlayerOneDiscordId, out var first)
            || !match.PlayerReports.TryGetValue(match.PlayerTwoDiscordId, out var second))
            return new ReportResolution(null, false);

        if (first == PlayerReport.Rematch && second == PlayerReport.Rematch)
        {
            match.Status = MatchStatus.RematchRequested;
            match.FinishedAtUtc = DateTime.UtcNow;
            return new ReportResolution(match, true);
        }

        ulong? reportedWinner = (first, second) switch
        {
            (PlayerReport.Won, PlayerReport.Lost) => match.PlayerOneDiscordId,
            (PlayerReport.Lost, PlayerReport.Won) => match.PlayerTwoDiscordId,
            _ => null
        };

        if (reportedWinner == null
            || match.AutomaticWinnerDiscordId != null && match.AutomaticWinnerDiscordId != reportedWinner)
        {
            match.Status = MatchStatus.Disputed;
            return new ReportResolution(match, false);
        }

        match.FinalWinnerDiscordId = reportedWinner;
        match.Status = MatchStatus.Completed;
        match.FinishedAtUtc = DateTime.UtcNow;
        return new ReportResolution(match, false);
    }

    sealed record ReportResolution(MatchRecord? Match, bool CreateRematch);
}
