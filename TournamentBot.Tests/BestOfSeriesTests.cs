using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

[TestFixture]
public sealed class BestOfSeriesTests
{
    [Test]
    public async Task BestOfThreeUsesBothPicksThenTheRoundRandomMap()
    {
        var (directory, coordinator, pool) = await CreateCoordinatorAsync(3);
        await using (pool)
        {
            try
            {
                var tournament = await coordinator.CreateTournamentAsync(
                    "BO3", TournamentFormat.SingleElimination, winsRequired: 2);
                await RegisterAndJoinAsync(coordinator, tournament.Id, 1, 2);
                tournament = await coordinator.StartTournamentAsync(tournament.Id);

                Assert.That(await coordinator.GetRecentMatchesAsync(), Is.Empty);
                var series = (await coordinator.GetSeriesAsync(tournament.SeriesIds.Single()))!;
                var randomMap = tournament.RandomMapByRound[1];
                var choices = tournament.MapPool.Where(map => map.Uid != randomMap.Uid).Take(2).ToList();

                await coordinator.SubmitSeriesMapPickAsync(series.Id, 1, choices[0].Uid);
                await coordinator.SubmitSeriesMapPickAsync(series.Id, 2, choices[1].Uid);

                var first = (await coordinator.GetRecentMatchesAsync()).Single();
                Assert.That(first.MapUid, Is.EqualTo(choices[0].Uid));
                await coordinator.ResolveAsync(first.Id, 1);

                var second = (await coordinator.GetRecentMatchesAsync()).First(match => match.Id != first.Id);
                Assert.That(second.MapUid, Is.EqualTo(choices[1].Uid));
                await coordinator.ResolveAsync(second.Id, 2);

                var third = (await coordinator.GetRecentMatchesAsync()).First(match =>
                    match.Id != first.Id && match.Id != second.Id);
                Assert.That(third.MapUid, Is.EqualTo(randomMap.Uid));
                await coordinator.ResolveAsync(third.Id, 1);

                var completed = await coordinator.GetTournamentAsync(tournament.Id);
                Assert.That(completed!.ChampionDiscordId, Is.EqualTo(1));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Test]
    public async Task BestOfFiveCollectsTwoAlternatingPicksPerSide()
    {
        var (directory, coordinator, pool) = await CreateCoordinatorAsync(6);
        await using (pool)
        {
            try
            {
                var tournament = await coordinator.CreateTournamentAsync(
                    "BO5", TournamentFormat.SingleElimination, winsRequired: 3);
                await RegisterAndJoinAsync(coordinator, tournament.Id, 1, 2);
                tournament = await coordinator.StartTournamentAsync(tournament.Id);
                var series = (await coordinator.GetSeriesAsync(tournament.SeriesIds.Single()))!;
                var picks = tournament.MapPool
                    .Where(map => map.Uid != tournament.RandomMapByRound[1].Uid)
                    .Take(4)
                    .ToList();

                await coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerOneDiscordId, picks[0].Uid);
                await coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerTwoDiscordId, picks[1].Uid);
                await coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerOneDiscordId, picks[2].Uid);
                await coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerTwoDiscordId, picks[3].Uid);

                series = (await coordinator.GetSeriesAsync(series.Id))!;
                Assert.That(series.Maps.Select(map => map.Uid), Is.EqualTo(new[]
                {
                    picks[0].Uid,
                    picks[1].Uid,
                    picks[2].Uid,
                    picks[3].Uid,
                    tournament.RandomMapByRound[1].Uid
                }));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Test]
    public async Task DecidingMapIsSharedByEverySeriesInTheRound()
    {
        var (directory, coordinator, pool) = await CreateCoordinatorAsync(7);
        await using (pool)
        {
            try
            {
                var tournament = await coordinator.CreateTournamentAsync(
                    "Shared decider", TournamentFormat.SingleElimination, winsRequired: 2);
                await RegisterAndJoinAsync(coordinator, tournament.Id, 1, 2, 3, 4);
                tournament = await coordinator.StartTournamentAsync(tournament.Id);

                foreach (var seriesId in tournament.SeriesIds)
                {
                    var series = (await coordinator.GetSeriesAsync(seriesId))!;
                    await coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerOneDiscordId, "__random__");
                    await coordinator.SubmitSeriesMapPickAsync(series.Id, series.PlayerTwoDiscordId, "__random__");
                }

                var seriesRecords = new List<TournamentSeries>();
                foreach (var seriesId in tournament.SeriesIds)
                    seriesRecords.Add((await coordinator.GetSeriesAsync(seriesId))!);

                Assert.That(seriesRecords.Select(series => series.Maps.Last().Uid).Distinct(),
                    Is.EqualTo(new[] { tournament.RandomMapByRound[1].Uid }));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }

    static async Task<(string Directory, TournamentCoordinator Coordinator, OpenRaServerPool Pool)> CreateCoordinatorAsync(int mapCount)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ymca-tournament-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var config = new BotConfiguration
        {
            StateFile = Path.Combine(directory, "state.json"),
            Server = new OpenRaServerConfiguration
            {
                MatchDirectory = Path.Combine(directory, "matches"),
                MaxConcurrentServers = 1
            }
        };
        var pool = new OpenRaServerPool(config.Server, new ReplayMetadataReader(config.Server));
        var coordinator = new TournamentCoordinator(config, new StateStore(config.StateFile), pool);
        for (var i = 1; i <= mapCount; i++)
            await coordinator.AddMapAsync($"map-{i}", $"Map {i}");
        return (directory, coordinator, pool);
    }

    static async Task RegisterAndJoinAsync(TournamentCoordinator coordinator, string tournamentId, params ulong[] players)
    {
        foreach (var player in players)
        {
            await coordinator.RegisterAsync(player, $"Player {player}", $"Player{player}");
            await coordinator.JoinTournamentAsync(tournamentId, player);
        }
    }
}
