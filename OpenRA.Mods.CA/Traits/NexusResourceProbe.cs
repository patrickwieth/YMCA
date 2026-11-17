using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class NexusResourceProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly bool CheckGreenFields = false;
		public override object Create(ActorInitializer init) => new NexusResourceProbe(this);
	}

	// Diagnostic fixtures only: check the live layer, not merely exported resource bytes.
	public class NexusResourceProbe : ITick
	{
		readonly NexusResourceProbeInfo info;
		int ticks;
		public NexusResourceProbe(NexusResourceProbeInfo info) { this.info = info; }
		void ITick.Tick(Actor self)
		{
			if (++ticks != 10) return;
			var world = self.World;
			var map = world.Map;
			var layer = world.WorldActor.Trait<IResourceLayer>();
			var buildings = world.WorldActor.Trait<BuildingInfluence>();
			if (info.CheckGreenFields)
			{
				var green = map.AllCells.Where(map.Contains).Where(c => map.Resources[c].Type == 3 && !buildings.AnyBuildingAt(c)).ToArray();
				if (green.Length < 1000 || green.Any(c => layer.GetResource(c).Type != "Tiberium" || layer.GetResource(c).Density <= 0))
					throw new InvalidOperationException("Generated green fields were missing or discarded during world loading.");
				var sample = green.First(c => layer.GetResource(c).Density > 1);
				if (layer.RemoveResource("Tiberium", sample, 1) != 1) throw new InvalidOperationException("Green resource extraction failed.");
				layer.AddResource("Tiberium", sample, 1);
				PlateauMovementProbe.WriteResult(info.ResultPath, $"RIVER RESOURCE PASS live green cells={green.Length}; extraction passed.");
				return;
			}
			var cells = map.AllCells.Where(map.Contains).Where(c => map.Resources[c].Type == 4 &&
				map.Tiles[c].Type == 1030 && !buildings.AnyBuildingAt(c)).ToArray();
			if (cells.Length == 0 || cells.Any(c => layer.GetResource(c).Type != "BlueTiberium" || layer.GetResource(c).Density <= 0))
				throw new InvalidOperationException("Dirt gems were discarded by the live resource layer.");
			var cell = cells[0];
			var density = layer.GetResource(cell).Density;
			if (layer.RemoveResource("BlueTiberium", cell, density) != density || layer.GetResource(cell).Density != 0 || map.GetTerrainInfo(cell).Type != "Dirt")
				throw new InvalidOperationException("Gem extraction did not restore non-buildable Dirt.");
			if (!layer.CanAddResource("Tiberium", cell) || layer.AddResource("Tiberium", cell, 1) != 1)
				throw new InvalidOperationException("Green Tiberium cannot grow on Dirt.");
			var greenDensity = layer.GetResource(cell).Density;
			if (greenDensity <= 0 || layer.RemoveResource("Tiberium", cell, greenDensity) != greenDensity || map.GetTerrainInfo(cell).Type != "Dirt")
				throw new InvalidOperationException("Green Tiberium cannot be extracted on Dirt.");
			layer.AddResource("BlueTiberium", cell, density);
			if (map.AllCells.Any(c => map.Resources[c].Type == 1 || map.Resources[c].Type == 2))
				throw new InvalidOperationException("Legacy resource indices were not normalized.");
			var tree = world.Actors.First(a => a.Info.Name == "splitblue" || a.Info.Name == "gmine");
			var neighbors = Enumerable.Range(-1, 3).SelectMany(x => Enumerable.Range(-1, 3).Select(y => tree.Location + new CVec(x, y)))
				.Where(c => map.Contains(c) && map.Tiles[c].Type == 1030 && !buildings.AnyBuildingAt(c)).ToArray();
			foreach (var neighbor in neighbors) layer.ClearResources(neighbor);
			for (var i = 0; i < 50; i++) tree.Trait<ISeedableResource>().Seed(tree);
			if (!neighbors.Any(c => layer.GetResource(c).Type == "BlueTiberium")) throw new InvalidOperationException("Blue tree produces no Tiberium on Dirt.");
			if (map.Rules.Actors.Values.Where(a => a.HasTraitInfo<BuildableInfo>() && a.HasTraitInfo<BuildingInfo>())
				.Any(a => a.TraitInfo<BuildingInfo>().TerrainTypes.Contains("Dirt")))
				throw new InvalidOperationException("A buildable structure allows Dirt.");
			PlateauMovementProbe.WriteResult(info.ResultPath, $"NEXUS RESOURCE PASS live blue-tiberium={cells.Length}; extracted={density}; depleted ground=Dirt; both Tiberium types extractable; blue tree replenishes Dirt; building rules reject Dirt.");
		}
	}
}
