using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.CA.Server
{
    // Only an explicit, local bot request arms this. The decision and shutdown run
    // on the server thread, so a match cannot start between checking and forfeiting.
    public sealed class TournamentAttendanceServer : ServerTrait, ITick
    {
        DateTime nextCheck;
        string handled = "";
        void ITick.Tick(S server)
        {
            var now = DateTime.UtcNow;
            if (now < nextCheck || server.Type != ServerType.Dedicated || server.Settings.AllowedPlayerNames.Length == 0) return;
            nextCheck = now.AddSeconds(1);
            var path = Path.Combine(Platform.SupportDir, TournamentAttendanceProtocol.RequestFile);
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length > 4096) return;
                var request = JsonConvert.DeserializeObject<TournamentAttendanceRequest>(File.ReadAllText(path));
                if (request == null || request.Id == handled || !Guid.TryParseExact(request.Id, "N", out _) ||
                    request.MapUid != server.LobbyInfo.GlobalSettings.Map || request.DeadlineUtc.Kind != DateTimeKind.Utc) return;
                now = DateTime.UtcNow; // Slow filesystem reads must not extend the forfeit decision window.
                if (server.State == ServerState.WaitingPlayers && now < request.DeadlineUtc) return;
                var outcome = "Review";
                if (server.State == ServerState.GameStarted) outcome = "Started";
                else if (server.State == ServerState.WaitingPlayers && now <= request.DeadlineUtc.AddSeconds(10))
                {
                    var validated = server.Conns.Where(c => c.Validated).Select(c => c.PlayerIndex).ToArray();
                    var present = server.LobbyInfo.Clients.Where(c => validated.Contains(c.Index) && !c.IsObserver && c.Bot == null).Select(c => c.Name);
                    outcome = TournamentAttendanceProtocol.Decide(server.Settings.TeamOnePlayerNames, server.Settings.TeamTwoPlayerNames, present);
                }
                var decision = new TournamentAttendanceDecision { Id = request.Id, MatchId = request.MatchId, MapUid = request.MapUid,
                    DecidedAtUtc = now, Outcome = outcome };
                var output = Path.Combine(Platform.SupportDir, TournamentAttendanceProtocol.DecisionFile);
                var temporary = output + ".tmp";
                File.WriteAllText(temporary, JsonConvert.SerializeObject(decision));
                if (File.Exists(output)) File.Delete(output);
                File.Move(temporary, output);
                handled = request.Id;
                if (outcome == "OneWins" || outcome == "TwoWins")
                {
                    Log.Write("server", "Admin-triggered attendance deadline: " + outcome);
                    server.Shutdown();
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
            {
                Log.Write("server", "Attendance timer could not be evaluated: " + e.Message);
            }
        }
    }
}
