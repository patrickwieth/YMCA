using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.Server;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckMapSelectionLobbyCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-map-selection-lobby";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Resolve the waiting lobby and verify hidden-map and start/ready command policy.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			mod.MapCache.LoadMaps();
			var waiting = mod.MapCache.Single(m => m.Status == MapStatus.Available &&
				m.Class == MapClassification.System && MapSelectionLobbyCommands.IsPlaceholder(m));
			if (waiting.Title != "Please select a map" || waiting.Visibility != (MapVisibility)0 || waiting.PlayerCount != 8)
				throw new InvalidDataException("Unexpected waiting lobby metadata.");
			if (!mod.Manifest.ServerTraits.Contains("MapSelectionLobbyCommands") ||
				mod.Manifest.ServerTraits.Contains("LobbyCommands"))
				throw new InvalidDataException("Start guard is not the active lobby command handler.");
			foreach (var command in new[] { "startgame", "startgame force", "state Ready", "state  Ready", "state Ready, Ready" })
				if (!MapSelectionLobbyCommands.BlocksCommand(waiting, command))
					throw new InvalidDataException("Waiting lobby allowed: " + command);
			foreach (var command in new[] { "state NotReady", "map example", "generate_map example recipe" })
				if (MapSelectionLobbyCommands.BlocksCommand(waiting, command))
					throw new InvalidDataException("Waiting lobby blocked selection: " + command);
			var playable = mod.MapCache.First(m => m.Status == MapStatus.Available && m.Visibility.HasFlag(MapVisibility.Lobby));
			if (MapSelectionLobbyCommands.BlocksCommand(playable, "startgame") || MapSelectionLobbyCommands.BlocksCommand(playable, "state Ready"))
				throw new InvalidDataException("Real map is incorrectly blocked.");
			Console.WriteLine("Waiting lobby passed: " + waiting.Uid + "; hidden placeholder, normal-map controls and start/ready guards.");
		}
	}
}
