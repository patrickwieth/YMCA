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

    public static ulong Winner(MatchRecord match, string choice)
    {
        choice = choice.Trim();
        if (choice.Equals(match.Id + ":one", StringComparison.OrdinalIgnoreCase)) return match.PlayerOneDiscordId;
        if (choice.Equals(match.Id + ":two", StringComparison.OrdinalIgnoreCase)) return match.PlayerTwoDiscordId;
        // Never reinterpret stale structured selections from a different pairing as a name.
        if (!System.Text.RegularExpressions.Regex.IsMatch(choice, @"^[MS]\d+:(one|two)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            bool Matches(bool one) => new[] { SideName(match, one),
                one ? match.PlayerOneOpenRaName : match.PlayerTwoOpenRaName,
                one ? match.PlayerOneTeammateOpenRaName : match.PlayerTwoTeammateOpenRaName,
                one ? match.PlayerOneTeamName : match.PlayerTwoTeamName }
                .Any(name => !string.IsNullOrWhiteSpace(name) && name.Equals(choice, StringComparison.OrdinalIgnoreCase));
            var first = Matches(true); var second = Matches(false);
            if (first != second) return first ? match.PlayerOneDiscordId : match.PlayerTwoDiscordId;
        }
        throw new InvalidOperationException($"Winner is unknown or ambiguous for {match.Id}. Select the winner again from this pairing's suggestions, or enter the exact player/team name.");
    }
}
