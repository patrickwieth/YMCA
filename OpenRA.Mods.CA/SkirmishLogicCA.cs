using System;
using System.IO;
using OpenRA.Mods.CA.Traits;
using System.Linq;
using OpenRA.Mods.Common.Server;
using OpenRA.Server;
using OpenRA.Traits;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.CA
{
	// Keep standard option/slot persistence, but never replace the wizard's map.
	public class SkirmishLogicCA : ServerTrait, IClientJoined, INotifySyncLobbyInfo
	{
		readonly SkirmishLogic defaults = new();
		bool diagnosticNetworkStarted;

		void INotifySyncLobbyInfo.LobbyInfoSynced(S server)
		{
			// No change to ordinary multiplayer. Explicit test maps may auto-start
			// only on a non-advertised dedicated server with two loopback clients.
			if (!diagnosticNetworkStarted && server.Type == ServerType.Dedicated && !server.Settings.AdvertiseOnline &&
				server.Map.WorldActorInfo.TraitInfoOrDefault<TerrainNetworkProbeInfo>() != null && server.Conns.Count == 2 &&
				server.Conns.All(c => c.Validated && c.EndPoint is System.Net.IPEndPoint endpoint && System.Net.IPAddress.IsLoopback(endpoint.Address)) &&
				server.LobbyInfo.Clients.Count(c => c.Bot == null && !c.IsObserver && !c.IsInvalid) == 2)
			{
				diagnosticNetworkStarted = true;
				server.LobbyInfo.GlobalSettings.RandomSeed = 43;
				foreach (var client in server.LobbyInfo.Clients) client.State = OpenRA.Network.Session.ClientState.Ready;
				server.SyncLobbyInfo();
				// The enclosing lobby state command performs CheckAutoStart after this
				// notification. Calling StartGame here would start the match twice.
				return;
			}
			((INotifySyncLobbyInfo)defaults).LobbyInfoSynced(server);
		}

		void IClientJoined.ClientJoined(S server, Connection connection)
		{
			// Launch.Map uses a Local server, not the ordinary skirmish lobby. Only an
			// explicitly instrumented local fixture may request automatic opponents.
			var soak = server.Type == ServerType.Local ? server.Map.WorldActorInfo.TraitInfoOrDefault<TerrainSoakProbeInfo>() : null;
			if (server.Type == ServerType.Local && soak != null && soak.BotCount > 0)
			{
				var controller = server.LobbyInfo.Clients.First(c => c.IsAdmin);
				if (soak.BotCount > 15 || !server.Map.PlayerActorInfo.TraitInfos<IBotInfo>().Any(b => b.Type == "normal"))
					throw new InvalidOperationException("Invalid diagnostic bot configuration.");
				server.LobbyInfo.GlobalSettings.RandomSeed = 43;
				server.InterpretCommand("spectate", connection);
				for (var i = server.LobbyInfo.Clients.Count(c => c.Bot != null); i < soak.BotCount; i++)
				{
					var empty = server.LobbyInfo.FirstEmptyBotSlot();
					if (empty == null) throw new InvalidOperationException("Not enough diagnostic bot slots.");
					server.InterpretCommand($"slot_bot {empty} {controller.Index} normal", connection);
				}
				server.SyncLobbyInfo();
				return;
			}
			if (server.Type != ServerType.Skirmish) return;
			var path = Path.Combine(Platform.SupportDir, $"skirmish.{server.ModData.Manifest.Id}.yaml");
			var previous = File.Exists(path) ? new MiniYaml("", MiniYaml.FromFile(path)).NodeWithKeyOrDefault("Map")?.Value.Value : null;
			if (previous == null || previous == server.LobbyInfo.GlobalSettings.Map)
			{
				((IClientJoined)defaults).ClientJoined(server, connection);
				return;
			}

			// Fresh map: create the usual default opponent without loading old slots.
			var slot = server.LobbyInfo.FirstEmptyBotSlot();
			var bot = server.Map.PlayerActorInfo.TraitInfos<IBotInfo>().Select(t => t.Type).FirstOrDefault();
			var admin = server.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
			if (slot != null && bot != null && admin != null)
				server.InterpretCommand($"slot_bot {slot} {admin.Index} {bot}", connection);
		}
	}
}
