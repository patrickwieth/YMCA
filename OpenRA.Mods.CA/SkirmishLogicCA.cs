using System;
using System.IO;
using System.Collections.Generic;
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
		SkirmishDebugPreference debugPreference;
		string observedMap;
		readonly HashSet<int> previousOpponents = new();

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
			if (server.Type == ServerType.Skirmish)
			{
				var mapChanged = observedMap != null && observedMap != server.LobbyInfo.GlobalSettings.Map;
				observedMap = server.LobbyInfo.GlobalSettings.Map;
				var humans = server.LobbyInfo.Clients.Where(c => c.Bot == null && !c.IsObserver).ToArray();
				var moved = false;
				if (humans.Length == 1)
				{
					var human = humans[0];
					if (mapChanged && server.Map.Players.Players[human.Slot].LockTeam)
						foreach (var bot in server.LobbyInfo.Clients.Where(c => c.Bot != null && !c.IsObserver && previousOpponents.Contains(c.Index)))
						{
							if (bot.Team != human.Team) continue;
							// Map changes compact occupied slots before applying map locks.
							// Keep an existing enemy an enemy, not a new bank-mate in Multi1.
							var slot = server.LobbyInfo.Slots.Values.FirstOrDefault(s => !s.Closed && s.AllowBots &&
								server.LobbyInfo.ClientInSlot(s.PlayerReference) == null &&
								server.Map.Players.Players[s.PlayerReference].Team != human.Team);
							if (slot == null) continue;
							bot.Slot = slot.PlayerReference;
							S.SyncClientToPlayerReference(bot, server.Map.Players.Players[bot.Slot]);
							moved = true;
						}
					previousOpponents.Clear();
					foreach (var bot in server.LobbyInfo.Clients.Where(c => c.Bot != null && !c.IsObserver &&
						(human.Team == 0 || c.Team == 0 || c.Team != human.Team))) previousOpponents.Add(bot.Index);
				}
				else previousOpponents.Clear();
				if (moved) { server.SyncLobbyInfo(); return; }
			}
			if (server.Type == ServerType.Skirmish && debugPreference != null)
			{
				server.LobbyInfo.GlobalSettings.LobbyOptions.TryGetValue("cheats", out var cheats);
				// SyncInfo was already dispatched. A restored map preference needs
				// one additional sync; Observe records the map before re-entry.
				if (debugPreference.Observe(server.LobbyInfo.GlobalSettings.Map, cheats)) server.SyncLobbyInfo();
			}
			((INotifySyncLobbyInfo)defaults).LobbyInfoSynced(server);
		}

		void IClientJoined.ClientJoined(S server, Connection connection)
		{
			// Launch.Map uses a Local server, not the ordinary skirmish lobby. Only an
			// explicitly instrumented local fixture may request automatic opponents.
			if (server.Type == ServerType.Local && server.Map.WorldActorInfo.TraitInfoOrDefault<StrategicFrontProbeInfo>() != null)
			{
				var controller = server.LobbyInfo.Clients.First(c => c.IsAdmin);
				string empty;
				while ((empty = server.LobbyInfo.FirstEmptyBotSlot()) != null)
					if (!server.InterpretCommand($"slot_bot {empty} {controller.Index} normal", connection))
						throw new InvalidOperationException("Cannot populate Strategic diagnostic slots.");
				server.SyncLobbyInfo();
				return;
			}
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
			var saved = File.Exists(path) ? new MiniYaml("", MiniYaml.FromFile(path)) : null;
			var previous = saved?.NodeWithKeyOrDefault("Map")?.Value.Value;
			var preference = new SkirmishDebugPreference(Path.Combine(Platform.SupportDir, $"skirmish-debug.{server.ModData.Manifest.Id}.txt"),
				saved?.NodeWithKeyOrDefault("Options")?.Value.NodeWithKeyOrDefault("cheats")?.Value.Value);
			void RestoreDebugPreference()
			{
				debugPreference = preference;
				server.LobbyInfo.GlobalSettings.LobbyOptions.TryGetValue("cheats", out var cheats);
				debugPreference.Restore(server.LobbyInfo.GlobalSettings.Map, cheats);
			}
			var hasLockedTeams = server.Map.Players.Players.Values.Any(p => p.Playable && p.LockTeam && p.Team != 0);
			if (previous == server.LobbyInfo.GlobalSettings.Map || previous == null && !hasLockedTeams)
			{
				((IClientJoined)defaults).ClientJoined(server, connection);
				RestoreDebugPreference();
				// Restoring a saved human slot must not override map-locked settings.
				foreach (var client in server.LobbyInfo.Clients.Where(c => c.Slot != null))
					S.SyncClientToPlayerReference(client, server.Map.Players.Players[client.Slot]);
				server.SyncLobbyInfo();
				return;
			}

			RestoreDebugPreference();
			// Fresh map: create the usual default opponent without loading old slots.
			var admin = server.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
			var slot = server.LobbyInfo.FirstEmptyBotSlot();
			if (hasLockedTeams && admin?.Slot != null)
			{
				var team = server.Map.Players.Players[admin.Slot].Team;
				slot = server.LobbyInfo.Slots.Values.Where(s => !s.Closed && s.AllowBots && server.LobbyInfo.ClientInSlot(s.PlayerReference) == null)
					.Select(s => s.PlayerReference).FirstOrDefault(s => server.Map.Players.Players[s].Team != team);
			}
			var bot = server.Map.PlayerActorInfo.TraitInfos<IBotInfo>().Select(t => t.Type).FirstOrDefault();
			if (slot != null && bot != null && admin != null)
				server.InterpretCommand($"slot_bot {slot} {admin.Index} {bot}", connection);
			server.SyncLobbyInfo();
		}
	}
}
