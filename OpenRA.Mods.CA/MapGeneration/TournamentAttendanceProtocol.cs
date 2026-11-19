using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
    public sealed class TournamentAttendanceRequest
    {
        public string Id { get; set; } = "";
        public string MatchId { get; set; } = "";
        public string MapUid { get; set; } = "";
        public DateTime DeadlineUtc { get; set; }
    }

    public sealed class TournamentAttendanceDecision
    {
        public string Id { get; set; } = "";
        public string MatchId { get; set; } = "";
        public string MapUid { get; set; } = "";
        public DateTime DecidedAtUtc { get; set; }
        public string Outcome { get; set; } = "Review";
    }

    public static class TournamentAttendanceProtocol
    {
        public const string RequestFile = "tournament-attendance-request.json";
        public const string DecisionFile = "tournament-attendance-decision.json";
        public static string Decide(string[] one, string[] two, IEnumerable<string> connected)
        {
            if (one.Length == 0 || two.Length == 0) return "Review";
            var names = new HashSet<string>(connected, StringComparer.OrdinalIgnoreCase);
            var first = one.Count(names.Contains);
            var second = two.Count(names.Contains);
            if (first == one.Length && second == 0) return "OneWins";
            if (second == two.Length && first == 0) return "TwoWins";
            return "Review";
        }
    }
}
