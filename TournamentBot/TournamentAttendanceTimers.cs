using OpenRA.Mods.CA.MapGeneration;

namespace Ymca.TournamentBot;

public sealed partial class TournamentCoordinator
{
    public async Task<IReadOnlyList<MatchRecord>> StartAttendanceTimerAsync(string tournamentId, ulong adminId)
    {
        var now = DateTime.UtcNow;
        var started = await store.UpdateAsync(state =>
        {
            var tournament = GetTournament(state, tournamentId);
            if (tournament.Status != TournamentStatus.Running)
                throw new InvalidOperationException("The tournament must be running.");
            var matches = state.Matches.Values.Where(m => m.TournamentId == tournament.Id &&
                m.Status is MatchStatus.Queued or MatchStatus.StartingServer or MatchStatus.WaitingForPlayers).ToList();
            if (matches.Count == 0) throw new InvalidOperationException("There are no open games waiting to start.");
            if (matches.Any(m => m.AttendanceTimer?.Outcome == "Pending"))
                throw new InvalidOperationException("An attendance timer is already active. It will not be silently reset.");
            foreach (var match in matches)
                match.AttendanceTimer = new MatchAttendanceTimer { Id = Guid.NewGuid().ToString("N"), StartedByDiscordId = adminId,
                    StartedAtUtc = now, DeadlineUtc = now.AddMinutes(15), EligibleForForfeit = match.Status == MatchStatus.WaitingForPlayers };
            return (Tournament: tournament, Matches: matches);
        });
        // Persist before arming the local hosts or notifying players.
        foreach (var match in started.Matches) TryArmAttendance(match);
        await NotifyAsync(n => n.AttendanceTimerStartedAsync(started.Tournament, started.Matches, now.AddMinutes(15)));
        return started.Matches;
    }

    static void TryArmAttendance(MatchRecord match)
    {
        try { TournamentAttendanceFiles.Arm(match); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine($"Could not arm attendance timer for {match.Id}: {e.Message}");
        }
    }

    public async Task WatchAttendanceTimersAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try { await CheckAttendanceTimersAsync(); }
            catch (Exception e) { Console.Error.WriteLine("Attendance timer check failed: " + e.Message); }
            await Task.Delay(TimeSpan.FromSeconds(3), token);
        }
    }

    public async Task CheckAttendanceTimersAsync(DateTime? observedAtUtc = null)
    {
        var now = observedAtUtc ?? DateTime.UtcNow;
        var matches = await store.ReadAsync(s => s.Matches.Values.Where(m => m.AttendanceTimer?.Outcome == "Pending").ToList());
        foreach (var match in matches)
        {
            var timer = match.AttendanceTimer!;
            if (match.Status is not (MatchStatus.Queued or MatchStatus.StartingServer or MatchStatus.WaitingForPlayers))
            {
                await FinishAttendanceReviewAsync(match.Id, timer.Id, "Not applicable", false);
                continue;
            }
            var decision = TournamentAttendanceFiles.Read(match);
            if (decision != null)
            {
                if (decision.Outcome is "OneWins" or "TwoWins")
                {
                    var accepted = await OnAttendanceDecisionAsync(match, decision);
                    if (!accepted && now >= timer.DeadlineUtc.AddSeconds(30))
                        await FinishAttendanceReviewAsync(match.Id, timer.Id, "Server decision was not applicable; admin review required.", true);
                }
                else await FinishAttendanceReviewAsync(match.Id, timer.Id,
                    decision.Outcome == "Started" ? "Game already started" : "Neither side is exclusively fully present; admin review required.", decision.Outcome != "Started");
                continue;
            }
            if (now >= timer.DeadlineUtc.AddSeconds(timer.EligibleForForfeit ? 30 : 0))
            {
                await FinishAttendanceReviewAsync(match.Id, timer.Id, timer.EligibleForForfeit
                    ? "No timely authoritative server decision; no automatic loss."
                    : "The lobby was unavailable at timer start or restarted; no automatic loss.", true);
                continue;
            }
            if (now < timer.DeadlineUtc)
            {
                TryArmAttendance(match);
                if (now >= timer.DeadlineUtc.AddMinutes(-2))
                {
                    var warn = await store.UpdateAsync(state =>
                    {
                        var current = state.Matches[match.Id].AttendanceTimer;
                        if (current?.Id != timer.Id || current.Outcome != "Pending" || current.WarningSent) return false;
                        current.WarningSent = true; return true;
                    });
                    if (warn) await NotifyAsync(n => n.AttendanceTimerWarningAsync(match, timer.DeadlineUtc));
                }
            }
        }
    }

    async Task FinishAttendanceReviewAsync(string matchId, string timerId, string reason, bool notify)
    {
        var updated = await store.UpdateAsync(state =>
        {
            var match = state.Matches[matchId]; var timer = match.AttendanceTimer;
            if (timer?.Id != timerId || timer.Outcome != "Pending") return null;
            timer.Outcome = notify ? "Review" : "Skipped";
            return match;
        });
        if (updated != null && notify) await NotifyAsync(n => n.AttendanceReviewAsync(updated, reason));
    }

    async Task<bool> OnAttendanceDecisionAsync(MatchRecord source, TournamentAttendanceDecision decision)
    {
        var resolved = await store.UpdateAsync(state =>
        {
            var match = state.Matches[source.Id]; var timer = match.AttendanceTimer;
            if (timer == null || timer.Id != decision.Id || decision.MatchId != match.Id || decision.MapUid != match.MapUid) return (Match: match, Changed: false, Accepted: false);
            if (timer.Outcome is "OneWins" or "TwoWins") return (Match: match, Changed: false, Accepted: true);
            if (timer.Outcome != "Pending" || !timer.EligibleForForfeit || match.Status != MatchStatus.WaitingForPlayers ||
                match.TournamentId == null || GetTournament(state, match.TournamentId).Status != TournamentStatus.Running ||
                DateTime.UtcNow < timer.DeadlineUtc || decision.DecidedAtUtc.Kind != DateTimeKind.Utc ||
                decision.DecidedAtUtc < timer.DeadlineUtc || decision.DecidedAtUtc > timer.DeadlineUtc.AddSeconds(10) ||
                decision.Outcome is not ("OneWins" or "TwoWins")) return (Match: match, Changed: false, Accepted: false);
            timer.Outcome = decision.Outcome;
            match.FinalWinnerDiscordId = decision.Outcome == "OneWins" ? match.PlayerOneDiscordId : match.PlayerTwoDiscordId;
            match.ForfeitReason = "No-show after the admin-triggered 15-minute attendance timer.";
            match.Status = MatchStatus.Completed; match.FinishedAtUtc = DateTime.UtcNow;
            return (Match: match, Changed: true, Accepted: true);
        });
        if (resolved.Changed)
        {
            await NotifyAsync(n => n.MatchCompletedAsync(resolved.Match));
            await AdvanceTournamentAsync(resolved.Match);
        }
        return resolved.Accepted;
    }
}
