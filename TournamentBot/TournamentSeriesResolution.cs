namespace Ymca.TournamentBot;

public sealed partial class TournamentCoordinator
{
    // UI projections only: these are never inserted into Matches or assigned game IDs.
    public Task<IReadOnlyList<MatchRecord>> GetResolutionChoicesAsync() => store.ReadAsync<IReadOnlyList<MatchRecord>>(state =>
    {
        var choices = state.Series.Values.Where(s => s.Status == TournamentSeriesStatus.AwaitingMapPicks
            && s.MatchIds.Count == 0 && state.Tournaments.TryGetValue(s.TournamentId, out var t)
            && t.Status == TournamentStatus.Running).Select(s =>
        {
            var t = state.Tournaments[s.TournamentId];
            var choice = new MatchRecord { Id = s.Id, PlayerOneDiscordId = s.PlayerOneDiscordId,
                PlayerTwoDiscordId = s.PlayerTwoDiscordId,
                PlayerOneOpenRaName = state.Players[s.PlayerOneDiscordId].OpenRaName,
                PlayerTwoOpenRaName = state.Players[s.PlayerTwoDiscordId].OpenRaName };
            if (t.Mode == TournamentMode.TwoVsTwo)
            {
                var one = t.Teams[s.PlayerOneDiscordId]; var two = t.Teams[s.PlayerTwoDiscordId];
                choice.PlayerOneTeamName = one.Name; choice.PlayerTwoTeamName = two.Name;
                choice.PlayerOneTeammateOpenRaName = state.Players[one.TeammateDiscordId].OpenRaName;
                choice.PlayerTwoTeammateOpenRaName = state.Players[two.TeammateDiscordId].OpenRaName;
            }
            return choice;
        });
        return choices.Concat(state.Matches.Values.Where(m => m.Status is not (MatchStatus.Completed or MatchStatus.Cancelled)))
            .ToArray();
    });

    async Task ResolvePendingSeriesAsync(string id, ulong winner)
    {
        var update = await store.UpdateAsync(state =>
        {
            if (!state.Series.TryGetValue(id, out var series))
                throw new InvalidOperationException("Series not found.");
            var tournament = state.Tournaments[series.TournamentId];
            if (tournament.Status != TournamentStatus.Running || series.Status != TournamentSeriesStatus.AwaitingMapPicks
                || series.MatchIds.Count != 0 || tournament.ProcessedSeriesIds.Contains(id))
                throw new InvalidOperationException("Only unresolved series awaiting map choices can be resolved directly. Resolve an individual game if play has already been scheduled.");
            if (winner != series.PlayerOneDiscordId && winner != series.PlayerTwoDiscordId)
                throw new InvalidOperationException("The selected winner did not participate in this series.");
            var loser = winner == series.PlayerOneDiscordId ? series.PlayerTwoDiscordId : series.PlayerOneDiscordId;
            tournament.ProcessedSeriesIds.Add(id);
            series.Status = TournamentSeriesStatus.Completed;
            series.ForfeitWinnerDiscordId = winner;
            series.ForfeitReason = "Admin awarded the entire series before map selection was completed.";
            series.ForfeitedAtUtc = DateTime.UtcNow;
            series.Wins[winner] = series.WinsRequired;
            if (series.MapChoiceTimer != null) series.MapChoiceTimer.Outcome = "Resolved";
            ApplyTournamentResult(tournament, winner, loser, series.TournamentRound, series.IsThirdPlaceMatch);
            return new MapChoiceTimerUpdate(tournament, series, "Resolved", series.ForfeitReason,
                ScheduleNextRound(state, tournament));
        });
        await NotifyAsync(n => n.MapChoiceTimerResolvedAsync(update.Tournament, update.Series, update.Reason));
        await StartScheduledMatchesAsync(update.Transition!);
    }
}
