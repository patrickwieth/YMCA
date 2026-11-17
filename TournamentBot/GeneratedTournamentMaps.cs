using System.Security.Cryptography;

namespace Ymca.TournamentBot;

// Persisted before any match is queued. Never roll a new seed when retrying.
public sealed class GeneratedMapSpec
{
    public string Id { get; set; } = "";
    public string Preset { get; set; } = "";
    public int Seed { get; set; }
    public int Players { get; set; }
    public int Protocol { get; set; } = 1;
}

public static class GeneratedTournamentMaps
{
    public static IReadOnlyList<TournamentMap> Archetypes => new[]
    {
        Choice("MountainValleys", "Mountain Valleys"), Choice("TwoSides", "Two Sides"),
        Choice("HillFortress", "Hill Fortress"), Choice("BattleNexus", "Battle Nexus"),
        Choice("Arabia", "Plains"), Choice("WaterBasins", "Water Basins"),
        Choice("Continents", "Continents"), Choice("Archipelago", "Archipelago"), Choice("Migration", "Migration")
    };

    static TournamentMap Choice(string preset, string title) => new()
    {
        Uid = "archetype:" + preset, Title = title, PlayerCount = 4
    };

    public static TournamentMap Freeze(TournamentMap choice, TournamentMode mode)
    {
        var known = Archetypes.SingleOrDefault(map => map.Uid == choice.Uid)
            ?? throw new InvalidOperationException("Unknown tournament map archetype.");
        var spec = new GeneratedMapSpec
        {
            Id = Guid.NewGuid().ToString("N"), Preset = known.Uid.Substring("archetype:".Length),
            Seed = RandomNumberGenerator.GetInt32(1, int.MaxValue), Players = mode == TournamentMode.TwoVsTwo ? 4 : 2
        };
        return new TournamentMap { Uid = "generated:" + spec.Id, Title = known.Title, PlayerCount = spec.Players, Generation = spec };
    }

    public static void Validate(GeneratedMapSpec spec)
    {
        if (!Guid.TryParseExact(spec.Id, "N", out _) || spec.Protocol != 1 || spec.Seed < 1 ||
            spec.Players is not (2 or 4) || !Archetypes.Any(map => map.Uid == "archetype:" + spec.Preset))
            throw new InvalidDataException("Invalid or unsupported tournament map generation request.");
    }
}
