namespace Ymca.TournamentBot;

public sealed record MapPoolChoice(string Name, string Value);

public static class MapPoolAutocomplete
{
    public static IReadOnlyList<MapPoolChoice> Search(bool generated, bool adding,
        IReadOnlyList<TournamentMap> pool, IReadOnlyList<OfficialMap> officialMaps, string query)
    {
        IEnumerable<TournamentMap> catalog = generated ? GeneratedTournamentMaps.Archetypes
            : officialMaps.Select(map => new TournamentMap { Uid = map.Uid, Title = map.Title, PlayerCount = map.PlayerCount });
        var candidates = adding
            ? catalog.Where(map => pool.All(existing => !existing.Uid.Equals(map.Uid, StringComparison.OrdinalIgnoreCase)))
            : pool;
        query = query.Trim();
        return candidates.Where(map => query.Length == 0 || map.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(map => map.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(map => map.Title, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .Select(map => new MapPoolChoice(Label(map, generated), map.Uid)).ToList();
    }

    static string Label(TournamentMap map, bool generated)
    {
        var label = generated ? $"{map.Title} (archetype)" : $"{map.Title} ({map.PlayerCount} players)";
        return label.Length <= 100 ? label : label[..97] + "...";
    }
}
