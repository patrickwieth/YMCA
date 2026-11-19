using System.Text.Json;
using OpenRA.Mods.CA.MapGeneration;

namespace Ymca.TournamentBot;

public static class TournamentAttendanceFiles
{
    public static void Clear(MatchRecord match)
    {
        foreach (var name in new[] { TournamentAttendanceProtocol.RequestFile, TournamentAttendanceProtocol.DecisionFile })
        {
            var path = Path.Combine(match.SupportDirectory, name);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public static void Arm(MatchRecord match)
    {
        var timer = match.AttendanceTimer;
        if (timer == null || !timer.EligibleForForfeit || timer.Outcome != "Pending" || match.Status != MatchStatus.WaitingForPlayers) return;
        var path = Path.Combine(match.SupportDirectory, TournamentAttendanceProtocol.RequestFile);
        if (File.Exists(path))
        {
            var old = JsonSerializer.Deserialize<TournamentAttendanceRequest>(File.ReadAllText(path));
            if (old?.Id == timer.Id) return;
        }
        var request = new TournamentAttendanceRequest { Id = timer.Id, MatchId = match.Id, MapUid = match.MapUid, DeadlineUtc = timer.DeadlineUtc };
        var temporary = path + ".bot-tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(request));
        File.Move(temporary, path, true);
    }

    public static TournamentAttendanceDecision? Read(MatchRecord match)
    {
        if (match.AttendanceTimer == null || string.IsNullOrEmpty(match.SupportDirectory)) return null;
        try
        {
            var path = Path.Combine(match.SupportDirectory, TournamentAttendanceProtocol.DecisionFile);
            if (!File.Exists(path) || new FileInfo(path).Length > 4096) return null;
            var decision = JsonSerializer.Deserialize<TournamentAttendanceDecision>(File.ReadAllText(path));
            return decision?.Id == match.AttendanceTimer.Id && decision.MatchId == match.Id && decision.MapUid == match.MapUid ? decision : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return null; }
    }
}
