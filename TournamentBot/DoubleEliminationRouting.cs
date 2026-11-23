namespace Ymca.TournamentBot;

// Version 1: fixed cross-feeds. Seed order is frozen in Entrants at tournament start.
// Missing seeds advance as byes, never as fabricated series or match results.
public static class DoubleEliminationRouting
{
    public sealed record Input(string Slot = "", bool Loser = false, ulong? Seed = null);
    public sealed record Slot(string Id, Input First, Input Second);
    public sealed record Pairing(string SlotId, ulong First, ulong Second);

    public static IReadOnlyList<Slot> Plan(IReadOnlyList<ulong> entrants)
    {
        if (entrants.Count < 2 || entrants.Distinct().Count() != entrants.Count)
            throw new ArgumentException("At least two distinct entrants are required.");
        var size = 2;
        while (size < entrants.Count) size *= 2;
        var slots = new List<Slot>();
        // Spread first-round byes across pairs instead of grouping empty matches.
        var seeds = new ulong?[size];
        for (var i = 0; i < size / 2; i++) seeds[2 * i] = entrants[i];
        for (var i = size / 2; i < entrants.Count; i++) seeds[2 * (i - size / 2) + 1] = entrants[i];
        var upper = new List<string>();
        for (var i = 0; i < size / 2; i++)
        {
            var id = $"W1-{i}";
            slots.Add(new(id, new(Seed: seeds[2 * i]), new(Seed: seeds[2 * i + 1])));
            upper.Add(id);
        }
        var lower = new List<string>();
        for (var i = 0; i < upper.Count / 2; i++)
        {
            var id = $"L1-{i}";
            slots.Add(new(id, new(upper[2 * i], true), new(upper[2 * i + 1], true)));
            lower.Add(id);
        }
        var round = 2;
        while (upper.Count > 1)
        {
            var next = new List<string>();
            for (var i = 0; i < upper.Count / 2; i++)
            {
                var id = $"W{round}-{i}";
                slots.Add(new(id, new(upper[2 * i]), new(upper[2 * i + 1])));
                next.Add(id);
            }
            var drop = new List<string>();
            for (var i = 0; i < next.Count; i++)
            {
                var id = $"L{2 * round - 2}-{i}";
                var crossed = next.Count > 1 ? i ^ 1 : i;
                slots.Add(new(id, new(lower[i]), new(next[crossed], true)));
                drop.Add(id);
            }
            lower = new();
            if (drop.Count == 1) lower.Add(drop[0]);
            else for (var i = 0; i < drop.Count / 2; i++)
            {
                var id = $"L{2 * round - 1}-{i}";
                slots.Add(new(id, new(drop[2 * i]), new(drop[2 * i + 1])));
                lower.Add(id);
            }
            upper = next;
            round++;
        }
        slots.Add(new("F1", new(upper[0]), lower.Count == 0 ? new(upper[0], true) : new(lower[0])));
        return slots;
    }

    public static IReadOnlyList<Pairing> Ready(TournamentRecord tournament, IEnumerable<TournamentSeries> series)
    {
        var played = series.ToDictionary(s => s.BracketSlotId);
        var outcomes = new Dictionary<string, (bool Ready, ulong? Winner, ulong? Loser)>();
        var ready = new List<Pairing>();
        (bool Ready, ulong? Player) Resolve(Input input)
        {
            if (input.Slot.Length == 0) return (true, input.Seed);
            var result = outcomes[input.Slot];
            return (result.Ready, input.Loser ? result.Loser : result.Winner);
        }
        foreach (var slot in Plan(tournament.Entrants))
        {
            var a = Resolve(slot.First); var b = Resolve(slot.Second);
            if (!a.Ready || !b.Ready) { outcomes[slot.Id] = (false, null, null); continue; }
            if (!a.Player.HasValue || !b.Player.HasValue)
            { outcomes[slot.Id] = (true, a.Player ?? b.Player, null); continue; }
            if (!played.TryGetValue(slot.Id, out var match))
            {
                ready.Add(new(slot.Id, a.Player.Value, b.Player.Value));
                outcomes[slot.Id] = (false, null, null);
                continue;
            }
            if (match.PlayerOneDiscordId != a.Player || match.PlayerTwoDiscordId != b.Player)
                throw new InvalidOperationException($"Bracket participants changed at {slot.Id}.");
            if (match.Status != TournamentSeriesStatus.Completed)
            { outcomes[slot.Id] = (false, null, null); continue; }
            var winner = match.ForfeitWinnerDiscordId ?? match.Wins.Where(w => w.Value >= match.WinsRequired).Select(w => (ulong?)w.Key).SingleOrDefault();
            if (winner != a.Player && winner != b.Player)
                throw new InvalidOperationException($"Missing bracket winner at {slot.Id}.");
            outcomes[slot.Id] = (true, winner, winner == a.Player ? b.Player : a.Player);
        }
        var final = outcomes["F1"];
        if (final.Ready && final.Winner.HasValue && final.Loser.HasValue &&
            tournament.Losses[final.Loser.Value] == 1 && !played.ContainsKey("F2"))
            ready.Add(new("F2", final.Winner.Value, final.Loser.Value));
        return ready;
    }
}
