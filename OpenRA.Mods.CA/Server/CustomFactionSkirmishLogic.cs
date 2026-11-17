using System.Linq;
using OpenRA.Mods.CA.Modular;
using OpenRA.Server;
using OpenRA.Traits;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.CA.Server
{
	// Preserve CA's generated-map wizard/diagnostics except for frozen designer sessions.
	public sealed class CustomFactionSkirmishLogic : ServerTrait, IClientJoined, INotifySyncLobbyInfo
	{
		readonly SkirmishLogicCA stock = new SkirmishLogicCA();
		readonly CustomFactionLobbySession session = new CustomFactionLobbySession();

		bool UseSavedSettings(S server) => session.UseSavedSettings(server.Type == ServerType.Skirmish,
			server.Type == ServerType.Skirmish && server.Map.Package.Contents.Contains("custom-faction.json"));

		void IClientJoined.ClientJoined(S server, Connection conn)
		{
			if (UseSavedSettings(server))
			{
				((IClientJoined)stock).ClientJoined(server, conn);
				return;
			}

			Log.Write("server", $"Custom faction test: keeping map {server.Map.Uid}; not restoring saved skirmish settings.");
			// Same default bot as a fresh stock skirmish, without reading skirmish.ca.yaml.
			var slot = server.LobbyInfo.FirstEmptyBotSlot();
			var bot = server.Map.PlayerActorInfo.TraitInfos<IBotInfo>().Select(t => t.Type).FirstOrDefault();
			var controller = server.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
			if (slot != null && bot != null && controller != null)
				server.InterpretCommand($"slot_bot {slot} {controller.Index} {bot}", conn);
		}

		void INotifySyncLobbyInfo.LobbyInfoSynced(S server)
		{
			if (UseSavedSettings(server))
				((INotifySyncLobbyInfo)stock).LobbyInfoSynced(server);
		}
	}
}
