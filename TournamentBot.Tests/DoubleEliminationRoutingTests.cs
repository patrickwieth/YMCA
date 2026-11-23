using System.Text.Json;
using NUnit.Framework;
using Ymca.TournamentBot;

namespace Ymca.TournamentBot.Tests;

public sealed class DoubleEliminationRoutingTests
{
    [Test]
    public void HistoricalStateDefaultsToLegacyRouting()
    {
        var old = JsonSerializer.Deserialize<TournamentRecord>("{\"Id\":\"T013\"}")!;
        Assert.That(old.BracketRoutingVersion, Is.Zero);
    }

    [Test]
    public void EightPlayerDropsCrossToOppositeHalf()
    {
        var plan = DoubleEliminationRouting.Plan(Enumerable.Range(1, 8).Select(i => (ulong)i).ToArray());
        Assert.That(plan.Single(s => s.Id == "L2-0").Second, Is.EqualTo(new DoubleEliminationRouting.Input("W2-1", true)));
        Assert.That(plan.Single(s => s.Id == "L2-1").Second, Is.EqualTo(new DoubleEliminationRouting.Input("W2-0", true)));
    }

    [Test]
    public void EveryFieldSizeCompletesWithTwoLossesAndRestartStableSlots()
    {
        for (var count = 2; count <= 64; count++)
        for (var trial = 0; trial < 8; trial++)
        {
            var random = new Random(count * 100 + trial);
            var t = new TournamentRecord { BracketRoutingVersion = 1,
                Entrants = Enumerable.Range(1, count).Select(i => (ulong)i).ToList() };
            t.Losses = t.Entrants.ToDictionary(p => p, _ => 0);
            var history = new List<TournamentSeries>();
            while (t.Losses.Count(p => p.Value < 2) > 1)
            {
                var ready = DoubleEliminationRouting.Ready(t, history);
                Assert.That(ready, Is.Not.Empty, $"Deadlock: {count}/{trial}");
                Assert.That(ready.SelectMany(p => new[] { p.First, p.Second }).Distinct().Count(), Is.EqualTo(ready.Count * 2));
                Assert.That(DoubleEliminationRouting.Ready(JsonSerializer.Deserialize<TournamentRecord>(JsonSerializer.Serialize(t))!,
                    JsonSerializer.Deserialize<List<TournamentSeries>>(JsonSerializer.Serialize(history))!), Is.EqualTo(ready));
                foreach (var p in ready)
                {
                    Assert.That(t.Losses[p.First], Is.LessThan(2));
                    Assert.That(t.Losses[p.Second], Is.LessThan(2));
                    if (p.SlotId.StartsWith("W")) Assert.That(t.Losses[p.First] + t.Losses[p.Second], Is.Zero);
                    if (p.SlotId.StartsWith("L")) Assert.That((t.Losses[p.First], t.Losses[p.Second]), Is.EqualTo((1, 1)));
                    var winner = random.Next(2) == 0 ? p.First : p.Second;
                    t.Losses[winner == p.First ? p.Second : p.First]++;
                    history.Add(new TournamentSeries { BracketSlotId = p.SlotId,
                        PlayerOneDiscordId = p.First, PlayerTwoDiscordId = p.Second,
                        Status = TournamentSeriesStatus.Completed, WinsRequired = 2,
                        // Exercise both played BO3 and series-level walkovers.
                        ForfeitWinnerDiscordId = trial % 2 == 0 ? winner : null,
                        Wins = trial % 2 == 0 ? new() : new() { [winner] = 2 } });
                }
                Assert.That(history.Count, Is.LessThanOrEqualTo(2 * count - 1));
            }
            Assert.That(history.Count, Is.InRange(2 * count - 2, 2 * count - 1));
            Assert.That(DoubleEliminationRouting.Ready(t, history), Is.Empty);
        }
    }
}
