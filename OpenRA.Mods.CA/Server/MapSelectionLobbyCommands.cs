using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Server;
using OpenRA.Network;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.CA.Server
{
	// Keep the engine lobby implementation, but never treat the waiting map as a match.
	public sealed class MapSelectionLobbyCommands : LobbyCommands, INotifyServerStart, IInterpretCommand
	{
		public const string PlaceholderCategory = "LobbyPlaceholder";
		public static bool IsPlaceholder(MapPreview map) => map?.Categories?.Contains(PlaceholderCategory) == true;

		public static bool BlocksCommand(MapPreview map, string command)
		{
			if (!IsPlaceholder(map)) return false;
			var parts = command.Split(new[] { ' ' }, 2);
			return parts[0] == "startgame" || parts[0] == "state" && parts.Length == 2 &&
				Enum<Session.ClientState>.TryParse(parts[1], false, out var state) && state == Session.ClientState.Ready;
		}

		void INotifyServerStart.ServerStarted(S server)
		{
			// Tournament hosts name their participants and supply an assigned map.
			if (server.Type != ServerType.Dedicated || server.Settings.AllowedPlayerNames.Length != 0)
			{
				base.ServerStarted(server);
				return;
			}

			var placeholder = server.ModData.MapCache.SingleOrDefault(m => m.Status == MapStatus.Available &&
				m.Class == MapClassification.System && IsPlaceholder(m));
			if (placeholder == null) throw new InvalidDataException("The bundled Please select a map lobby is missing.");

			var configuredPool = server.Settings.MapPool;
			var initialPool = configuredPool.Length != 0 ? configuredPool :
				server.Settings.QueryMapRepository ? Array.Empty<string>() : server.ModData.MapCache
					.Where(m => m.Status == MapStatus.Available && m.Visibility.HasFlag(MapVisibility.Lobby)).Select(m => m.Uid).ToArray();
			server.Settings.Map = placeholder.Uid;
			// InitializeMapPool runs inside the base method. Admit the hidden placeholder
			// only during startup, then restore the operator's original map policy.
			server.Settings.MapPool = initialPool.Concat(new[] { placeholder.Uid }).ToArray();
			try { base.ServerStarted(server); }
			finally
			{
				server.Settings.MapPool = configuredPool;
				if (configuredPool.Length == 0 && server.Settings.QueryMapRepository) server.MapPool = null;
				else server.MapPool?.Remove(placeholder.Uid);
			}
		}

		bool IInterpretCommand.InterpretCommand(S server, Connection conn, Session.Client client, string cmd)
		{
			if (server == null || conn == null || client == null) return base.InterpretCommand(server, conn, client, cmd);
			if (BlocksCommand(server.Map, cmd))
			{
				server.SendOrderTo(conn, "Message", "Please select a map before readying or starting the game.");
				return true;
			}

			return base.InterpretCommand(server, conn, client, cmd);
		}
	}
}
