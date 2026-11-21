namespace Ymca.TournamentBot;

public sealed partial class TournamentCoordinator
{
    public static bool HasAllMapChoices(TournamentSeries series, ulong playerId) =>
        series.MapPicks.TryGetValue(playerId, out var picks) && picks.Count >= series.WinsRequired - 1;

    sealed record MapChoiceTimerUpdate(TournamentRecord Tournament, TournamentSeries Series,
        string Action, string Reason, TournamentTransition? Transition = null);

    async Task CheckMapChoiceTimersAsync(DateTime now)
    {
        var ids = await store.ReadAsync(state => state.Series.Values
            .Where(series => series.MapChoiceTimer?.Outcome == "Pending").Select(series => series.Id).ToArray());
        foreach (var id in ids)
        {
            var update = await store.UpdateAsync<MapChoiceTimerUpdate?>(state =>
            {
                var series = state.Series[id];
                var timer = series.MapChoiceTimer;
                if (timer?.Outcome != "Pending") return null;
                if (!state.Tournaments.TryGetValue(series.TournamentId, out var tournament) ||
                    tournament.Status != TournamentStatus.Running || series.Status != TournamentSeriesStatus.AwaitingMapPicks)
                {
                    timer.Outcome = "Skipped";
                    return null;
                }
                var firstReady = HasAllMapChoices(series, series.PlayerOneDiscordId);
                var secondReady = HasAllMapChoices(series, series.PlayerTwoDiscordId);
                if (firstReady && secondReady)
                {
                    timer.Outcome = "Completed";
                    return null;
                }
                if (now < timer.DeadlineUtc)
                {
                    if (timer.WarningSent || now < timer.DeadlineUtc.AddMinutes(-2)) return null;
                    timer.WarningSent = true;
                    return new MapChoiceTimerUpdate(tournament, series, "Warning", "");
                }
                if (!timer.EligibleForForfeit || firstReady == secondReady)
                {
                    timer.Outcome = "Review";
                    return new MapChoiceTimerUpdate(tournament, series, "Review", !timer.EligibleForForfeit
                        ? "The bot restarted during the map-choice timer; no automatic loss. An admin can restart the timer."
                        : "Neither side completed its map choices. No automatic winner; an admin can restart the timer.");
                }

                var winner = firstReady ? series.PlayerOneDiscordId : series.PlayerTwoDiscordId;
                var loser = firstReady ? series.PlayerTwoDiscordId : series.PlayerOneDiscordId;
                if (!tournament.ProcessedSeriesIds.Add(series.Id))
                {
                    timer.Outcome = "Skipped";
                    return null;
                }
                timer.Outcome = "Forfeit";
                series.Status = TournamentSeriesStatus.Completed;
                series.ForfeitWinnerDiscordId = winner;
                series.ForfeitReason = "Map choices not completed within the admin-triggered 15-minute deadline; entire series forfeited.";
                series.ForfeitedAtUtc = now;
                series.Wins[winner] = series.WinsRequired;
                // No fictitious games/replays are created. The series and bracket result
                // are saved atomically and the loser receives ONE tournament loss.
                ApplyTournamentResult(tournament, winner, loser, series.TournamentRound, series.IsThirdPlaceMatch);
                return new MapChoiceTimerUpdate(tournament, series, "Forfeit", series.ForfeitReason,
                    ScheduleNextRound(state, tournament));
            });
            if (update == null) continue;
            if (update.Action == "Warning")
                await NotifyAsync(n => n.MapChoiceTimerWarningAsync(update.Tournament, update.Series, update.Series.MapChoiceTimer!.DeadlineUtc));
            else
                await NotifyAsync(n => n.MapChoiceTimerResolvedAsync(update.Tournament, update.Series, update.Reason));
            if (update.Transition != null)
                await StartScheduledMatchesAsync(update.Transition);
        }
    }
}
