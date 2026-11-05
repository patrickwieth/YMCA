using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--prepare-building-test-water PATH", "Put water under Naval Yards in the disposable building-test map only.")]
	public sealed class PrepareBuildingTestWaterCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--prepare-building-test-water";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var path = Path.GetFullPath(args[1]);
			if (!string.Equals(Path.GetFileName(path), "testmap1-debug.oramap", StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException("Only the disposable testmap1-debug.oramap may be modified.");

			Game.ModData = utility.ModData;
			using var package = new Folder(Platform.EngineDir).OpenPackage(path, utility.ModData.ModFiles);
			using var map = new Map(utility.ModData, package);
			if (!map.Title.StartsWith("Building Grid Test", StringComparison.Ordinal))
				throw new InvalidDataException("Not the building grid test map.");

			// Reuse a water tile already present in this map rather than inventing artwork.
			var candidates = map.AllCells.Where(c => map.GetTerrainInfo(c).Type == "Water").ToArray();
			TerrainTile water;
			if (candidates.Length > 0)
				water = map.Tiles[candidates[0]];
			else
			{
				var terrain = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
				var template = terrain.Templates.Values.OrderBy(t => t.Id).First(t =>
					t.Size == new int2(1, 1) && t.Contains(0) && t[0].TerrainType == terrain.GetTerrainIndex("Water"));
				water = new TerrainTile(template.Id, 0);
			}
			var changed = 0;
			var yards = 0;
			foreach (var node in map.ActorDefinitions.Where(n => n.Value.Value.Equals("syrd", StringComparison.OrdinalIgnoreCase)))
			{
				var actor = new ActorReference(node.Value.Value, node.Value.ToDictionary());
				var location = actor.Get<LocationInit>().Value;
				var size = map.Rules.Actors["syrd"].TraitInfo<BuildingInfo>().Dimensions;
				// Include a one-cell water border for the ship exits.
				for (var y = -1; y <= size.Y; y++)
					for (var x = -1; x <= size.X; x++)
					{
						var cell = location + new CVec(x, y);
						if (!map.Contains(cell))
							throw new InvalidDataException("Naval Yard water pool extends outside the map.");
						map.Tiles[cell] = water;
						changed++;
					}

				yards++;
			}

			if (yards == 0)
				throw new InvalidDataException("No Allied Naval Yard found in the test map.");
			map.Save((IReadWritePackage)package);
			Console.WriteLine($"Building test water prepared: {yards} Naval Yard(s), {changed} cells; original map untouched.");
		}
	}
}
