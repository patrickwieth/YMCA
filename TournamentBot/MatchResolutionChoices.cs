namespace Ymca.TournamentBot;

public static class MatchResolutionChoices
{
    public static string SideName(MatchRecord match, bool one)
    {
        var team = one ? match.PlayerOneTeamName : match.PlayerTwoTeamName;
        var first = one ? match.PlayerOneOpenRaName : match.PlayerTwoOpenRaName;
        var second = one ? match.PlayerOneTeammateOpenRaName : match.PlayerTwoTeammateOpenRaName;
        var players = first + (string.IsNullOrEmpty(second) ? "" : " + " + second);
        return string.IsNullOrEmpty(team) ? players : team + " (" + players + ")";
    }

    public static IReadOnlyList<MapPoolChoice> Winners(MatchRecord? match, string query)
    {
        if (match == null || match.Status is MatchStatus.Completed or MatchStatus.Cancelled) return Array.Empty<MapPoolChoice>();
        return new[] { true, false }.Select(one => new MapPoolChoice(SideName(match, one), match.Id + (one ? ":one" : ":two")))
            .Where(choice => choice.Name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(choice => choice with { Name = choice.Name.Length <= 100 ? choice.Name : choice.Name[..97] + "..." }).ToList();
    }

    public static ulong Winner(MatchRecord match, string choice) => choice switch
    {
        var value when value == match.Id + ":one" => match.PlayerOneDiscordId,
        var value when value == match.Id + ":two" => match.PlayerTwoDiscordId,
        _ => throw new InvalidOperationException("Select a winning side from this match's autocomplete choices.")
    };
}
