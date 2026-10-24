using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class HillDirtTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--hill-dirt-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT", "Check centered Hill Fortress dirt and collision-preserving legacy grass/dirt normalization.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			foreach (var name in new[] { "RUBBERDUCK-TEMPERATE", "RUBBERDUCK-TEMPERATE-SHADED" })
			{
				var terrain = (DefaultTerrain)utility.ModData.DefaultTerrainInfo[name];
				foreach (var tile in Enumerable.Range(1100, 20).Concat(Enumerable.Range(1200, 20)).Concat(Enumerable.Range(1300, 4)).Concat(Enumerable.Range(1320, 4)))
					for (var frame = 0; frame < terrain.Templates[(ushort)tile].TilesCount; frame++)
						if (!RubberduckShoreTopology.IsLegacyGrassDirt((ushort)tile) || terrain.GetTerrainIndex(new TerrainTile((ushort)tile, (byte)frame)) != terrain.GetTerrainIndex(new TerrainTile(1000, 0)))
							throw new InvalidDataException("Legacy dirt normalization would change collision.");
			}
			foreach (var players in new[] { 3, 4, 8 })
				foreach (var seed in new[] { 42, 117327894 })
				{
					var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "preset=hill-fortress", "mode=strategic", $"players={players}", $"seed={seed}", "size=auto" });
					var plan = MapPlanGenerator.Generate(options);
					var path = new RubberduckMapExporter(utility.ModData).Export(plan, options, Path.Combine(args[1], $"{players}-{seed}"));
					using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var map = new Map(utility.ModData, zip);
					var dirt = map.AllCells.Where(map.Contains).Where(c => map.Tiles[c].Type == 1030).Select(c => c.ToMPos(map)).ToArray();
					var cx = (plan.Width - 1) / 2.0 + 1; var cy = (plan.Height - 1) / 2.0 + map.Grid.MaximumTerrainHeight + 1;
					if (dirt.Length == 0 || Math.Abs(dirt.Average(p => p.U) - cx) > 1 || Math.Abs(dirt.Average(p => p.V) - cy) > 1)
						throw new InvalidDataException("Hill dirt drifted away from the contested center.");
					if (map.AllCells.Any(c => RubberduckShoreTopology.IsLegacyGrassDirt(map.Tiles[c].Type))) throw new InvalidDataException("Export baked old rectangular dirt transitions.");
					Console.WriteLine($"HILL DIRT PASS players={players} seed={seed} dirt={dirt.Length}; centered patch, native joins.");
				}
		}
	}
}
