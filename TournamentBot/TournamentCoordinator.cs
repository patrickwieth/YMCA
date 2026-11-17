namespace Ymca.TournamentBot;

public interface ITournamentNotifier
{
    Task MatchQueuedAsync(MatchRecord match);
    Task ServerReadyAsync(MatchRecord match, string joinUri);
    Task MatchStartedAsync(MatchRecord match);
    Task ResultReadyAsync(MatchRecord match, ReplayResult result);
    Task MatchCompletedAsync(MatchRecord match);
    Task MatchDisputedAsync(MatchRecord match, string reason);
    Task MatchFailedAsync(MatchRecord match, string reason);
    Task SeriesMapPicksRequestedAsync(TournamentRecord tournament, TournamentSeries series);
    Task TournamentUpdatedAsync(TournamentRecord tournament, IReadOnlyList<MatchRecord> newMatches);
    Task TournamentCompletedAsync(TournamentRecord tournament);
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
        serverPool.MatchStarted += OnMatchStartedAsync;
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
                match.SpectatorPassword = "";
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
        string? parentMatchId = null,
        string? tournamentId = null,
        int tournamentRound = 0,
        bool isThirdPlaceMatch = false,
        MatchRecord? teamSource = null)
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

            var created = CreateMatchRecord(state, playerOne, playerTwo, mapUid, mapTitle, parentMatchId, tournamentId,
                tournamentRound, isThirdPlaceMatch);
            if (teamSource != null)
            {
                created.PlayerOneTeammateDiscordId = teamSource.PlayerOneTeammateDiscordId;
                created.PlayerTwoTeammateDiscordId = teamSource.PlayerTwoTeammateDiscordId;
                created.PlayerOneTeammateOpenRaName = teamSource.PlayerOneTeammateOpenRaName;
                created.PlayerTwoTeammateOpenRaName = teamSource.PlayerTwoTeammateOpenRaName;
                created.PlayerOneTeamName = teamSource.PlayerOneTeamName;
                created.PlayerTwoTeamName = teamSource.PlayerTwoTeamName;
                created.SeriesId = teamSource.SeriesId;
                created.SeriesGameNumber = teamSource.SeriesGameNumber;
                created.WinsRequired = teamSource.WinsRequired;
                if (!string.IsNullOrEmpty(created.SeriesId)
                    && state.Series.TryGetValue(created.SeriesId, out var series))
                    series.MatchIds.Add(created.Id);
            }

            return created;
        });

        await serverPool.EnqueueAsync(match);
        await NotifyAsync(value => value.MatchQueuedAsync(match));
        return match;
    }

    public Task<TournamentMap> AddMapAsync(string uid, string title, int playerCount = 0) => store.UpdateAsync(state =>
    {
        if (string.IsNullOrWhiteSpace(uid))
            throw new InvalidOperationException("A map UID is required.");
        if (state.MapPool.Any(map => map.Uid.Equals(uid.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("This map is already in the tournament pool.");

        var map = new TournamentMap
        {
            Uid = uid.Trim(),
            Title = string.IsNullOrWhiteSpace(title) ? uid.Trim() : title.Trim(),
            PlayerCount = playerCount
        };
        state.MapPool.Add(map);
        return map;
    });

    public Task<TournamentMap> RemoveMapAsync(string uid) => store.UpdateAsync(state =>
    {
        var map = state.MapPool.FirstOrDefault(value => value.Uid.Equals(uid.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Map not found in the tournament pool.");
        state.MapPool.Remove(map);
        return map;
    });

    public Task<IReadOnlyList<TournamentMap>> GetMapPoolAsync() =>
        store.ReadAsync<IReadOnlyList<TournamentMap>>(state => state.MapPool.ToList());

    public Task<TournamentRecord> CreateTournamentAsync(
        string name,
        TournamentFormat format,
        TournamentMode mode = TournamentMode.OneVsOne,
        bool allowSpectators = false,
        int winsRequired = 1) => store.UpdateAsync(state =>
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("A tournament name is required.");
        if (state.MapPool.Count == 0)
            throw new InvalidOperationException("Add at least one map to the tournament map pool first.");
        if (winsRequired is < 1 or > 3)
            throw new InvalidOperationException("Required wins must be between one and three.");

        var id = $"T{state.NextTournamentNumber++:000}";
        var tournament = new TournamentRecord
        {
            Id = id,
            Name = name.Trim(),
            Format = format,
            Mode = mode,
            AllowSpectators = allowSpectators,
            WinsRequired = winsRequired,
            Status = TournamentStatus.Registration,
            MapPool = state.MapPool.Select(map => new TournamentMap
            {
                Uid = map.Uid,
                Title = map.Title,
                PlayerCount = map.PlayerCount
            }).ToList(),
            CreatedAtUtc = DateTime.UtcNow
        };
        state.Tournaments[id] = tournament;
        return tournament;
    });

    public Task<TournamentRecord> JoinTournamentAsync(string tournamentId, ulong playerId) =>
        store.UpdateAsync(state =>
        {
            var tournament = GetTournament(state, tournamentId);
            if (tournament.Status != TournamentStatus.Registration)
                throw new InvalidOperationException("Tournament registration is closed.");
            if (tournament.Mode != TournamentMode.OneVsOne)
                throw new InvalidOperationException("Use /tournament-team-join to enter a 2v2 tournament.");
            if (!state.Players.ContainsKey(playerId))
                throw new InvalidOperationException("Register your OpenRA name first using /register.");
            if (!tournament.Entrants.Contains(playerId))
                tournament.Entrants.Add(playerId);
            return tournament;
        });

    public Task<TournamentTeam> InviteTeamAsync(
        string tournamentId,
        ulong captainId,
        ulong teammateId,
        string teamName) => store.UpdateAsync(state =>
    {
        var tournament = GetTournament(state, tournamentId);
        if (tournament.Status != TournamentStatus.Registration || tournament.Mode != TournamentMode.TwoVsTwo)
            throw new InvalidOperationException("This tournament is not open for 2v2 team registration.");
        if (captainId == teammateId)
            throw new InvalidOperationException("Choose a different teammate.");
        if (!state.Players.ContainsKey(captainId) || !state.Players.ContainsKey(teammateId))
            throw new InvalidOperationException("Both players must register their YMCA names first using /register.");
        if (string.IsNullOrWhiteSpace(teamName) || teamName.Trim().Length > 40)
            throw new InvalidOperationException("The team name must contain between 1 and 40 characters.");
        if (tournament.Teams.Values.Any(team =>
            team.CaptainDiscordId == captainId || team.TeammateDiscordId == captainId
            || team.CaptainDiscordId == teammateId || team.TeammateDiscordId == teammateId))
            throw new InvalidOperationException("One of these players already has a team or pending invitation.");
        if (tournament.Teams.Values.Any(team => team.Name.Equals(teamName.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("This team name is already in use.");

        var team = new TournamentTeam
        {
            Name = teamName.Trim(),
            CaptainDiscordId = captainId,
            TeammateDiscordId = teammateId
        };
        tournament.Teams[captainId] = team;
        return team;
    });

    public Task<(TournamentRecord Tournament, TournamentTeam Team)> RespondToTeamInviteAsync(
        string tournamentId,
        ulong captainId,
        ulong teammateId,
        bool accept) => store.UpdateAsync(state =>
    {
        var tournament = GetTournament(state, tournamentId);
        if (tournament.Status != TournamentStatus.Registration)
            throw new InvalidOperationException("Tournament registration is closed.");
        if (!tournament.Teams.TryGetValue(captainId, out var team) || team.TeammateDiscordId != teammateId || team.Accepted)
            throw new InvalidOperationException("This team invitation is no longer active.");

        if (!accept)
        {
            tournament.Teams.Remove(captainId);
            return (tournament, team);
        }

        if (tournament.Teams.Values.Any(other => other.Accepted && other.CaptainDiscordId != captainId
            && (other.CaptainDiscordId == teammateId || other.TeammateDiscordId == teammateId
                || other.CaptainDiscordId == captainId || other.TeammateDiscordId == captainId)))
            throw new InvalidOperationException("One of these players has joined another team.");

        team.Accepted = true;
        if (!tournament.Entrants.Contains(captainId))
            tournament.Entrants.Add(captainId);
        return (tournament, team);
    });

    public Task<TournamentRecord> LeaveTournamentAsync(string tournamentId, ulong playerId) =>
        store.UpdateAsync(state =>
        {
            var tournament = GetTournament(state, tournamentId);
            if (tournament.Status != TournamentStatus.Registration)
                throw new InvalidOperationException("You cannot leave after the tournament has started.");
            if (tournament.Mode == TournamentMode.TwoVsTwo)
            {
                var team = tournament.Teams.Values.FirstOrDefault(value =>
                    value.CaptainDiscordId == playerId || value.TeammateDiscordId == playerId)
                    ?? throw new InvalidOperationException("You are not entered in this tournament.");
                tournament.Entrants.Remove(team.CaptainDiscordId);
                tournament.Teams.Remove(team.CaptainDiscordId);
            }
            else if (!tournament.Entrants.Remove(playerId))
                throw new InvalidOperationException("You are not entered in this tournament.");
            return tournament;
        });

    public Task<TournamentRecord> DeleteTournamentAsync(string tournamentId) => store.UpdateAsync(state =>
    {
        var tournament = GetTournament(state, tournamentId);
        if (tournament.Status == TournamentStatus.Running)
            throw new InvalidOperationException("A running tournament cannot be deleted while its matches are active.");

        state.Tournaments.Remove(tournament.Id);
        return tournament;
    });

    public async Task<TournamentRecord> StartTournamentAsync(string tournamentId)
    {
        var transition = await store.UpdateAsync(state =>
        {
            var tournament = GetTournament(state, tournamentId);
            if (tournament.Status != TournamentStatus.Registration)
                throw new InvalidOperationException("Tournament is not open for registration.");
            if (tournament.Entrants.Count < 2)
                throw new InvalidOperationException("At least two players are required.");

            if (state.MapPool.Count == 0)
                throw new InvalidOperationException("The tournament map pool is empty.");

            Shuffle(tournament.Entrants);
            tournament.MapPool = state.MapPool
                .Where(map => tournament.Mode == TournamentMode.OneVsOne || map.PlayerCount == 0 || map.PlayerCount >= 4)
                .Select(map => new TournamentMap { Uid = map.Uid, Title = map.Title, PlayerCount = map.PlayerCount })
                .ToList();
            if (tournament.MapPool.Count == 0)
                throw new InvalidOperationException("The map pool has no maps that support four players.");
            tournament.Losses = tournament.Entrants.ToDictionary(player => player, _ => 0);
            tournament.Status = TournamentStatus.Running;
            tournament.StartedAtUtc = DateTime.UtcNow;
            return ScheduleNextRound(state, tournament);
        });

        await StartScheduledMatchesAsync(transition);
        return transition.Tournament;
    }

    public Task<TournamentRecord?> GetTournamentAsync(string tournamentId) =>
        store.ReadAsync(state => state.Tournaments.GetValueOrDefault(tournamentId.Trim().ToUpperInvariant()));

    public Task<IReadOnlyList<TournamentRecord>> GetTournamentsAsync() =>
        store.ReadAsync<IReadOnlyList<TournamentRecord>>(state => state.Tournaments.Values
            .OrderByDescending(tournament => tournament.CreatedAtUtc)
            .ToList());

    public Task<RegisteredPlayer?> GetPlayerAsync(ulong discordId) =>
        store.ReadAsync(state => state.Players.GetValueOrDefault(discordId));

    public Task<MatchRecord?> GetMatchAsync(string id) =>
        store.ReadAsync(state => state.Matches.GetValueOrDefault(id.Trim().ToUpperInvariant()));

    public Task<TournamentSeries?> GetSeriesAsync(string id) =>
        store.ReadAsync(state => state.Series.GetValueOrDefault(id.Trim().ToUpperInvariant()));

    public async Task<SeriesMapPickResult> SubmitSeriesMapPickAsync(string seriesId, ulong playerId, string mapUid)
    {
        var result = await store.UpdateAsync(state =>
        {
            if (!state.Series.TryGetValue(seriesId.Trim().ToUpperInvariant(), out var series))
                throw new InvalidOperationException("Tournament series not found.");
            var tournament = GetTournament(state, series.TournamentId);
            if (series.Status != TournamentSeriesStatus.AwaitingMapPicks)
                throw new InvalidOperationException("Map selection for this series is closed.");
            if (playerId != series.PlayerOneDiscordId && playerId != series.PlayerTwoDiscordId)
                throw new InvalidOperationException("Only the team representatives can select maps.");

            var required = tournament.WinsRequired - 1;
            var picks = series.MapPicks[playerId];
            if (picks.Count >= required)
                throw new InvalidOperationException("You have already selected all your maps.");

            var excluded = series.MapPicks.Values.SelectMany(value => value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var randomMap = tournament.RandomMapByRound[series.TournamentRound];
            var available = tournament.MapPool.Where(map => !excluded.Contains(map.Uid)
                && (tournament.MapPool.Count <= 1 || !map.Uid.Equals(randomMap.Uid, StringComparison.OrdinalIgnoreCase))).ToList();
            TournamentMap selected;
            if (mapUid == "__random__")
            {
                if (available.Count == 0)
                    available = tournament.MapPool.Where(map => !excluded.Contains(map.Uid)).ToList();
                if (available.Count == 0)
                    available = tournament.MapPool.ToList();
                selected = available[Random.Shared.Next(available.Count)];
            }
            else
            {
                selected = tournament.MapPool.FirstOrDefault(map => map.Uid.Equals(mapUid, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("The selected map is not in this tournament's pool.");
                if (excluded.Contains(selected.Uid) && tournament.MapPool.Count > excluded.Count)
                    throw new InvalidOperationException("That map has already been selected for this series.");
            }

            picks.Add(selected.Uid);
            var ready = series.MapPicks.Values.All(value => value.Count >= required);
            MatchRecord? match = null;
            if (ready)
            {
                series.Maps = BuildSeriesMaps(tournament, series);
                series.Status = TournamentSeriesStatus.Playing;
                match = CreateSeriesMatch(state, tournament, series, 1);
            }

            return new SeriesMapPickResult(tournament, series, selected, match);
        });

        if (result.Match != null)
        {
            await serverPool.EnqueueAsync(result.Match);
            await NotifyAsync(value => value.MatchQueuedAsync(result.Match));
            await NotifyAsync(value => value.TournamentUpdatedAsync(result.Tournament, new[] { result.Match }));
        }

        return result;
    }

    public Task<IReadOnlyList<MatchRecord>> GetRecentMatchesAsync(int count = 10) =>
        store.ReadAsync<IReadOnlyList<MatchRecord>>(state => state.Matches.Values
            .OrderByDescending(match => match.CreatedAtUtc)
            .Take(count)
            .ToList());

    public Task RecordTournamentAnnouncementAsync(string tournamentId, ulong messageId) => store.UpdateAsync(state =>
        GetTournament(state, tournamentId).RegistrationAnnouncementMessageId = messageId);

    public Task RecordTeamInvitationAsync(string tournamentId, ulong captainId, ulong messageId) => store.UpdateAsync(state =>
        GetTournament(state, tournamentId).Teams[captainId].InvitationMessageId = messageId);

    public Task RecordMatchMessagesAsync(
        string matchId,
        IReadOnlyDictionary<ulong, ulong>? joinMessages = null,
        IReadOnlyDictionary<ulong, ulong>? resultMessages = null,
        ulong? spectatorMessageId = null) => store.UpdateAsync(state =>
    {
        var match = state.Matches[matchId];
        if (joinMessages != null)
            match.JoinDmMessageIds = new Dictionary<ulong, ulong>(joinMessages);
        if (resultMessages != null)
            match.ResultDmMessageIds = new Dictionary<ulong, ulong>(resultMessages);
        if (spectatorMessageId != null)
            match.SpectatorAnnouncementMessageId = spectatorMessageId;
    });

    public async Task SubmitReportAsync(string matchId, ulong playerId, PlayerReport report)
    {
        var resolution = await store.UpdateAsync(state =>
        {
            if (!state.Matches.TryGetValue(matchId.Trim().ToUpperInvariant(), out var match))
                throw new InvalidOperationException("Match not found.");
            if (!ParticipantIds(match).Contains(playerId))
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
                resolution.Match.Id,
                resolution.Match.TournamentId,
                resolution.Match.TournamentRound,
                resolution.Match.IsThirdPlaceMatch,
                resolution.Match);
        }
        else if (resolution.Match.Status == MatchStatus.Completed)
        {
            await NotifyAsync(value => value.MatchCompletedAsync(resolution.Match));
            await AdvanceTournamentAsync(resolution.Match);
        }
        else if (resolution.Match.Status == MatchStatus.Disputed)
            await NotifyAsync(value => value.MatchDisputedAsync(resolution.Match, "Player reports conflict or a player disputed the result."));
    }

    public async Task ResolveAsync(string matchId, ulong winnerId)
    {
        var match = await store.UpdateAsync(state =>
        {
            if (!state.Matches.TryGetValue(matchId.Trim().ToUpperInvariant(), out var existing))
                throw new InvalidOperationException("Match not found.");
            var representative = TeamOneIds(existing).Contains(winnerId)
                ? existing.PlayerOneDiscordId
                : TeamTwoIds(existing).Contains(winnerId) ? existing.PlayerTwoDiscordId : (ulong?)null;
            if (representative == null)
                throw new InvalidOperationException("The selected winner did not participate in this match.");
            if (existing.Status == MatchStatus.Completed)
                throw new InvalidOperationException("This match has already been resolved.");

            existing.FinalWinnerDiscordId = representative;
            existing.Status = MatchStatus.Completed;
            existing.FinishedAtUtc = DateTime.UtcNow;
            return existing;
        });

        await NotifyAsync(value => value.MatchCompletedAsync(match));
        await AdvanceTournamentAsync(match);
    }

    async Task AdvanceTournamentAsync(MatchRecord match)
    {
        if (match.TournamentId == null || match.FinalWinnerDiscordId == null)
            return;

        var transition = await store.UpdateAsync(state =>
        {
            var tournament = GetTournament(state, match.TournamentId);
            if (tournament.Status != TournamentStatus.Running
                || !tournament.ProcessedMatchIds.Add(match.Id))
                return new TournamentTransition(tournament, Array.Empty<MatchRecord>(), false);

            if (!string.IsNullOrEmpty(match.SeriesId)
                && state.Series.TryGetValue(match.SeriesId, out var series))
            {
                series.Wins[match.FinalWinnerDiscordId.Value] =
                    series.Wins.GetValueOrDefault(match.FinalWinnerDiscordId.Value) + 1;
                if (series.Wins[match.FinalWinnerDiscordId.Value] < series.WinsRequired)
                {
                    var next = CreateSeriesMatch(state, tournament, series, match.SeriesGameNumber + 1);
                    return new TournamentTransition(tournament, new[] { next }, false);
                }

                series.Status = TournamentSeriesStatus.Completed;
                tournament.ProcessedSeriesIds.Add(series.Id);
            }

            var loser = match.FinalWinnerDiscordId == match.PlayerOneDiscordId
                ? match.PlayerTwoDiscordId
                : match.PlayerOneDiscordId;
            if (match.IsThirdPlaceMatch)
            {
                tournament.ThirdPlaceDiscordId = match.FinalWinnerDiscordId;
                tournament.FourthPlaceDiscordId = loser;
            }
            else
            {
                tournament.Losses[loser] = tournament.Losses.GetValueOrDefault(loser) + 1;
                var eliminationLosses = tournament.Format == TournamentFormat.DoubleElimination ? 2 : 1;
                if (tournament.Losses[loser] >= eliminationLosses)
                {
                    tournament.EliminatedInRound[loser] = match.TournamentRound;
                    var survivors = tournament.Entrants.Count(player =>
                        tournament.Losses.GetValueOrDefault(player) < eliminationLosses);
                    if (survivors == 1)
                        tournament.RunnerUpDiscordId = loser;
                    else if (survivors == 2 && tournament.Format == TournamentFormat.DoubleElimination)
                        tournament.ThirdPlaceDiscordId = loser;
                }
            }

            return ScheduleNextRound(state, tournament);
        });

        await StartScheduledMatchesAsync(transition);
    }

    async Task StartScheduledMatchesAsync(TournamentTransition transition)
    {
        foreach (var match in transition.NewMatches)
        {
            await serverPool.EnqueueAsync(match);
            await NotifyAsync(value => value.MatchQueuedAsync(match));
        }

        foreach (var series in transition.NewSeries.Where(series => series.Status == TournamentSeriesStatus.AwaitingMapPicks))
            await NotifyAsync(value => value.SeriesMapPicksRequestedAsync(transition.Tournament, series));

        if (transition.Completed)
            await NotifyAsync(value => value.TournamentCompletedAsync(transition.Tournament));
        else if (transition.NewMatches.Count > 0 || transition.NewSeries.Count > 0)
            await NotifyAsync(value => value.TournamentUpdatedAsync(transition.Tournament, transition.NewMatches));
    }

    static TournamentTransition ScheduleNextRound(TournamentState state, TournamentRecord tournament)
    {
        var eliminationLosses = tournament.Format == TournamentFormat.DoubleElimination ? 2 : 1;
        var active = tournament.Entrants
            .Where(player => tournament.Losses.GetValueOrDefault(player) < eliminationLosses)
            .ToList();

        var unresolvedMatchExists = tournament.MatchIds
            .Where(state.Matches.ContainsKey)
            .Select(id => state.Matches[id])
            .Any(match => !tournament.ProcessedMatchIds.Contains(match.Id)
                && match.Status is not MatchStatus.RematchRequested and not MatchStatus.Cancelled);
        var unresolvedSeriesExists = tournament.SeriesIds.Any(id =>
            state.Series.TryGetValue(id, out var series) && series.Status != TournamentSeriesStatus.Completed);
        if (unresolvedMatchExists || unresolvedSeriesExists)
            return new TournamentTransition(tournament, Array.Empty<MatchRecord>(), false);

        if (active.Count == 1)
        {
            tournament.ChampionDiscordId = active[0];
            tournament.Status = TournamentStatus.Completed;
            tournament.FinishedAtUtc = DateTime.UtcNow;
            return new TournamentTransition(tournament, Array.Empty<MatchRecord>(), true);
        }

        var pairings = new List<(ulong First, ulong Second)>();
        if (tournament.Format == TournamentFormat.SingleElimination)
        {
            var pairingCount = active.Count / 2;
            if (tournament.RoundNumber == 0 && (active.Count & (active.Count - 1)) != 0)
            {
                var bracketSize = 1;
                while (bracketSize * 2 < active.Count)
                    bracketSize *= 2;
                pairingCount = active.Count - bracketSize;
            }

            for (var i = 0; i < pairingCount * 2; i += 2)
                pairings.Add((active[i], active[i + 1]));
        }
        else if (active.Count == 2 && tournament.Losses[active[0]] != tournament.Losses[active[1]])
            pairings.Add((active[0], active[1]));
        else
        {
            foreach (var group in active.GroupBy(player => tournament.Losses[player]).OrderBy(group => group.Key))
            {
                var players = group.ToList();
                for (var i = 0; i + 1 < players.Count; i += 2)
                    pairings.Add((players[i], players[i + 1]));
            }
        }

        if (pairings.Count == 0)
            throw new InvalidOperationException($"Tournament {tournament.Id} cannot schedule its next round.");

        var roundMap = SelectRoundMap(tournament);
        tournament.RoundNumber++;
        tournament.MapUid = roundMap.Uid;
        tournament.MapTitle = roundMap.Title;
        tournament.RandomMapByRound[tournament.RoundNumber] = roundMap;

        if (tournament.Format == TournamentFormat.SingleElimination && active.Count == 2
            && tournament.ThirdPlaceDiscordId == null
            && !tournament.MatchIds.Where(state.Matches.ContainsKey).Select(id => state.Matches[id])
                .Any(match => match.IsThirdPlaceMatch))
        {
            var latestEliminationRound = tournament.EliminatedInRound.Values.DefaultIfEmpty().Max();
            var bronzeCandidates = tournament.EliminatedInRound
                .Where(entry => entry.Value == latestEliminationRound)
                .Select(entry => entry.Key)
                .ToList();
            if (bronzeCandidates.Count == 2)
                pairings.Add((bronzeCandidates[0], bronzeCandidates[1]));
            else if (bronzeCandidates.Count == 1)
                tournament.ThirdPlaceDiscordId = bronzeCandidates[0];
        }

        var matches = new List<MatchRecord>();
        var seriesList = new List<TournamentSeries>();
        foreach (var pairing in pairings)
        {
            var isThirdPlaceMatch = tournament.Format == TournamentFormat.SingleElimination
                && active.Count == 2
                && tournament.Losses.GetValueOrDefault(pairing.First) > 0
                && tournament.Losses.GetValueOrDefault(pairing.Second) > 0;
            var series = new TournamentSeries
            {
                Id = $"S{state.NextSeriesNumber++:0000}",
                TournamentId = tournament.Id,
                PlayerOneDiscordId = pairing.First,
                PlayerTwoDiscordId = pairing.Second,
                TournamentRound = tournament.RoundNumber,
                IsThirdPlaceMatch = isThirdPlaceMatch,
                WinsRequired = tournament.WinsRequired,
                Status = tournament.WinsRequired == 1
                    ? TournamentSeriesStatus.Playing
                    : TournamentSeriesStatus.AwaitingMapPicks,
                MapPicks = new Dictionary<ulong, List<string>>
                {
                    [pairing.First] = new(),
                    [pairing.Second] = new()
                },
                Wins = new Dictionary<ulong, int>
                {
                    [pairing.First] = 0,
                    [pairing.Second] = 0
                }
            };
            if (tournament.WinsRequired == 1)
            {
                series.Maps.Add(roundMap);
                matches.Add(CreateSeriesMatch(state, tournament, series, 1));
            }

            state.Series[series.Id] = series;
            tournament.SeriesIds.Add(series.Id);
            seriesList.Add(series);
        }

        return new TournamentTransition(tournament, matches, false, seriesList);
    }

    static TournamentMap SelectRoundMap(TournamentRecord tournament)
    {
        if (tournament.MapPool.Count == 0)
            throw new InvalidOperationException($"Tournament {tournament.Id} has no maps configured.");

        var available = tournament.MapPool
            .Where(map => !tournament.MapHistory.Contains(map.Uid, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (available.Count == 0)
        {
            tournament.MapHistory.Clear();
            available = tournament.MapPool
                .Where(map => tournament.MapPool.Count == 1
                    || !map.Uid.Equals(tournament.MapUid, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var selected = available[Random.Shared.Next(available.Count)];
        tournament.MapHistory.Add(selected.Uid);
        return selected;
    }

    static List<TournamentMap> BuildSeriesMaps(TournamentRecord tournament, TournamentSeries series)
    {
        var maps = new List<TournamentMap>();
        var picksPerSide = tournament.WinsRequired - 1;
        for (var i = 0; i < picksPerSide; i++)
        {
            maps.Add(tournament.MapPool.First(map => map.Uid.Equals(
                series.MapPicks[series.PlayerOneDiscordId][i], StringComparison.OrdinalIgnoreCase)));
            maps.Add(tournament.MapPool.First(map => map.Uid.Equals(
                series.MapPicks[series.PlayerTwoDiscordId][i], StringComparison.OrdinalIgnoreCase)));
        }

        maps.Add(tournament.RandomMapByRound[series.TournamentRound]);
        return maps;
    }

    static MatchRecord CreateSeriesMatch(
        TournamentState state,
        TournamentRecord tournament,
        TournamentSeries series,
        int gameNumber)
    {
        var map = series.Maps[gameNumber - 1];
        var match = CreateMatchRecord(
            state,
            state.Players[series.PlayerOneDiscordId],
            state.Players[series.PlayerTwoDiscordId],
            map.Uid,
            map.Title,
            null,
            tournament.Id,
            series.TournamentRound,
            series.IsThirdPlaceMatch);
        match.SeriesId = series.Id;
        match.SeriesGameNumber = gameNumber;
        match.WinsRequired = series.WinsRequired;
        series.MatchIds.Add(match.Id);

        if (tournament.Mode == TournamentMode.TwoVsTwo)
        {
            var firstTeam = tournament.Teams[series.PlayerOneDiscordId];
            var secondTeam = tournament.Teams[series.PlayerTwoDiscordId];
            var firstTeammate = state.Players[firstTeam.TeammateDiscordId];
            var secondTeammate = state.Players[secondTeam.TeammateDiscordId];
            match.PlayerOneTeamName = firstTeam.Name;
            match.PlayerTwoTeamName = secondTeam.Name;
            match.PlayerOneTeammateDiscordId = firstTeammate.DiscordUserId;
            match.PlayerTwoTeammateDiscordId = secondTeammate.DiscordUserId;
            match.PlayerOneTeammateOpenRaName = firstTeammate.OpenRaName;
            match.PlayerTwoTeammateOpenRaName = secondTeammate.OpenRaName;
        }

        return match;
    }

    static MatchRecord CreateMatchRecord(
        TournamentState state,
        RegisteredPlayer playerOne,
        RegisteredPlayer playerTwo,
        string mapUid,
        string mapTitle,
        string? parentMatchId,
        string? tournamentId,
        int tournamentRound = 0,
        bool isThirdPlaceMatch = false)
    {
        var id = $"M{state.NextMatchNumber++:0000}";
        var match = new MatchRecord
        {
            Id = id,
            PlayerOneDiscordId = playerOne.DiscordUserId,
            PlayerTwoDiscordId = playerTwo.DiscordUserId,
            PlayerOneOpenRaName = playerOne.OpenRaName,
            PlayerTwoOpenRaName = playerTwo.OpenRaName,
            MapUid = mapUid.Trim(),
            MapTitle = string.IsNullOrWhiteSpace(mapTitle) ? mapUid.Trim() : mapTitle.Trim(),
            Status = MatchStatus.Queued,
            ParentMatchId = parentMatchId,
            TournamentId = tournamentId,
            TournamentRound = tournamentRound,
            IsThirdPlaceMatch = isThirdPlaceMatch,
            AllowSpectators = tournamentId != null
                && state.Tournaments.TryGetValue(tournamentId, out var spectatorTournament)
                && spectatorTournament.AllowSpectators,
            CreatedAtUtc = DateTime.UtcNow
        };
        state.Matches[id] = match;
        if (tournamentId != null && state.Tournaments.TryGetValue(tournamentId, out var tournament))
            tournament.MatchIds.Add(id);
        return match;
    }

    static TournamentRecord GetTournament(TournamentState state, string tournamentId)
    {
        if (!state.Tournaments.TryGetValue(tournamentId.Trim().ToUpperInvariant(), out var tournament))
            throw new InvalidOperationException("Tournament not found.");
        return tournament;
    }

    static void Shuffle<T>(IList<T> values)
    {
        for (var i = values.Count - 1; i > 0; i--)
        {
            var other = Random.Shared.Next(i + 1);
            (values[i], values[other]) = (values[other], values[i]);
        }
    }

    Task OnServerStartingAsync(MatchRecord match) => store.UpdateAsync(state =>
    {
        var stored = state.Matches[match.Id];
        stored.Port = match.Port;
        stored.Password = match.Password;
        stored.SpectatorPassword = match.SpectatorPassword;
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
            stored.SpectatorPassword = match.SpectatorPassword;
            stored.SupportDirectory = match.SupportDirectory;
            stored.Status = MatchStatus.WaitingForPlayers;
            stored.StartedAtUtc = DateTime.UtcNow;
        });

        var joinUri = $"ymca://{config.Server.PublicHost}:{match.Port}?password={Uri.EscapeDataString(match.Password)}";
        await NotifyAsync(value => value.ServerReadyAsync(match, joinUri));
    }

    async Task OnMatchStartedAsync(MatchRecord match)
    {
        var updated = await store.UpdateAsync(state =>
        {
            var stored = state.Matches[match.Id];
            stored.Status = MatchStatus.Playing;
            return stored;
        });

        await NotifyAsync(value => value.MatchStartedAsync(updated));
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
        bool HasOutcome(IEnumerable<string> names, string outcome) => names.All(name =>
            result.Players.Any(player => player.IsHuman
                && player.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                && player.Outcome.Equals(outcome, StringComparison.OrdinalIgnoreCase)));

        var teamOneNames = TeamOneNames(match);
        var teamTwoNames = TeamTwoNames(match);
        if (HasOutcome(teamOneNames, "Won") && HasOutcome(teamTwoNames, "Lost"))
            return match.PlayerOneDiscordId;
        if (HasOutcome(teamTwoNames, "Won") && HasOutcome(teamOneNames, "Lost"))
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

        PlayerReport? TeamReport(IEnumerable<ulong> members)
        {
            var reports = members.Where(match.PlayerReports.ContainsKey).Select(id => match.PlayerReports[id]).Distinct().ToList();
            if (reports.Count > 1)
                return PlayerReport.Dispute;
            return reports.Count == 1 ? reports[0] : null;
        }

        var first = TeamReport(TeamOneIds(match));
        var second = TeamReport(TeamTwoIds(match));
        if (first == PlayerReport.Dispute || second == PlayerReport.Dispute)
        {
            match.Status = MatchStatus.Disputed;
            return new ReportResolution(match, false);
        }
        if (first == null || second == null)
            return new ReportResolution(null, false);

        if (first == PlayerReport.Rematch && second == PlayerReport.Rematch)
        {
            match.Status = MatchStatus.RematchRequested;
            match.FinishedAtUtc = DateTime.UtcNow;
            return new ReportResolution(match, true);
        }

        ulong? reportedWinner = (first.Value, second.Value) switch
        {
            (PlayerReport.Won, PlayerReport.Lost) => match.PlayerOneDiscordId,
            (PlayerReport.Lost, PlayerReport.Won) => match.PlayerTwoDiscordId,
            _ => null
        };

        if (reportedWinner == null)
        {
            match.Status = MatchStatus.Disputed;
            return new ReportResolution(match, false);
        }

        // A consistent report from both sides takes precedence over the replay parser.
        // Explicit disputes and conflicting player reports are handled above.
        match.FinalWinnerDiscordId = reportedWinner;
        match.Status = MatchStatus.Completed;
        match.FinishedAtUtc = DateTime.UtcNow;
        return new ReportResolution(match, false);
    }

    static IEnumerable<ulong> TeamOneIds(MatchRecord match)
    {
        yield return match.PlayerOneDiscordId;
        if (match.PlayerOneTeammateDiscordId is ulong teammate)
            yield return teammate;
    }

    static IEnumerable<ulong> TeamTwoIds(MatchRecord match)
    {
        yield return match.PlayerTwoDiscordId;
        if (match.PlayerTwoTeammateDiscordId is ulong teammate)
            yield return teammate;
    }

    static IEnumerable<ulong> ParticipantIds(MatchRecord match) => TeamOneIds(match).Concat(TeamTwoIds(match));

    static IEnumerable<string> TeamOneNames(MatchRecord match)
    {
        yield return match.PlayerOneOpenRaName;
        if (!string.IsNullOrEmpty(match.PlayerOneTeammateOpenRaName))
            yield return match.PlayerOneTeammateOpenRaName;
    }

    static IEnumerable<string> TeamTwoNames(MatchRecord match)
    {
        yield return match.PlayerTwoOpenRaName;
        if (!string.IsNullOrEmpty(match.PlayerTwoTeammateOpenRaName))
            yield return match.PlayerTwoTeammateOpenRaName;
    }

    public sealed record SeriesMapPickResult(
        TournamentRecord Tournament,
        TournamentSeries Series,
        TournamentMap SelectedMap,
        MatchRecord? Match);

    sealed record ReportResolution(MatchRecord? Match, bool CreateRematch);
    sealed record TournamentTransition(
        TournamentRecord Tournament,
        IReadOnlyList<MatchRecord> NewMatches,
        bool Completed,
        IReadOnlyList<TournamentSeries>? ScheduledSeries = null)
    {
        public IReadOnlyList<TournamentSeries> NewSeries => ScheduledSeries ?? Array.Empty<TournamentSeries>();
    }
}
