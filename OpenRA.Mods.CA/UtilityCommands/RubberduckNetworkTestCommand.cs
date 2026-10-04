using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckNetworkTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-network-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		[Desc("MAP OUTPUT", "Export a two-client loopback network-order/sync fixture on unchanged production terrain.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[2]); Directory.CreateDirectory(output);
			using var stream = File.OpenRead(args[1]); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[1]);
			using var map = new Map(utility.ModData, zip);
			if (!map.Tileset.StartsWith("RUBBERDUCK-", StringComparison.Ordinal) || map.RuleDefinitions != null && !map.RuleDefinitions.Nodes.IsDefaultOrEmpty)
				throw new InvalidDataException("Network fixture needs an ordinary Rubberduck production map.");
			var spawns = map.ActorDefinitions.Where(n => n.Value.Value == "mpspawn").Select(n => new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value).ToArray();
			if (spawns.Length < 2) throw new InvalidDataException("Two starts required.");
			var actors = map.ActorDefinitions.ToList();
			for (var i = 0; i < 2; i++)
			{
				var cell = spawns[i] + new CVec(-1, -1);
				foreach (var c in new[] { cell, cell + new CVec(0, 3) })
					if (!map.Contains(c) || map.Tiles[c].Type != 1000 || map.Ramp[c] != 0)
						throw new InvalidDataException("Network tank endpoint is not ordinary flat home grass.");
				actors.Add(new MiniYamlNode("NetworkTank" + i, new ActorReference("calibration.networktank") { new LocationInit(cell), new OwnerInit("Multi" + i) }.Save()));
			}
			map.ActorDefinitions = actors;
			var players = new MapPlayers(map.PlayerDefinitions);
			for (var i = 0; i < 2; i++)
			{
				var player = players.Players["Multi" + i]; player.Spawn = i + 1; player.LockSpawn = true;
				player.Faction = "blackh"; player.LockFaction = true;
			}
			map.PlayerDefinitions = players.ToMiniYaml();
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString("World:\n\tTerrainNetworkProbe:\n\t\tTicks: 3000\nPlayer:\n\t-ConquestVictoryConditions:\ncalibration.networktank:\n\tInherits: MTNK\n\tRenderSprites:\n\t\tImage: mtnk\n", "loopback-network-test"));
			var path = Path.Combine(output, "rubberduck-network-test.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			File.WriteAllText(Path.Combine(output, "map-uid.txt"), map.Uid);
			Console.WriteLine(path);
		}
	}
}
