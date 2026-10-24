using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// Opt-in diagnostic map only; never attached to normal worlds.
	public class TiberiumLightProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly CPos Cell = new CPos(82, -17);
		public override object Create(ActorInitializer init) => new TiberiumLightProbe(this);
	}

	public class TiberiumLightProbe : ITick
	{
		readonly TiberiumLightProbeInfo info;
		int ticks;
		float3 baseline, fullGreen;
		public TiberiumLightProbe(TiberiumLightProbeInfo info) { this.info = info; }
		static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException("Tiberium light: " + message); }
		void ITick.Tick(Actor self)
		{
			if (++ticks % 20 != 0 || ticks > 160) return;
			var world = self.World;
			var layer = self.Trait<IResourceLayer>();
			var lights = self.Trait<TiberiumTerrainLight>();
			var terrain = self.Trait<ITerrainLighting>();
			var cell = info.Cell;
			var other = cell + new CVec(8, 0);
			var position = world.Map.CenterOfCell(cell);
			var green = layer.GetMaxDensity("Tiberium");
			var blue = layer.GetMaxDensity("BlueTiberium");
			var shroud = world.RenderPlayer.Shroud;
			switch (ticks)
			{
				case 20:
					shroud.Disabled = true;
					baseline = terrain.TintAt(position);
					// Empty cells start with density one in ResourceLayer.CreateResourceCell.
					layer.AddResource("Tiberium", cell, green - 1);
					Check(layer.GetResource(cell).Density == green, "initial green addition");
					break;
				case 40:
					Check(lights.KnownResource(cell) == ("Tiberium", green), "green light creation");
					fullGreen = terrain.TintAt(position);
					Check(fullGreen.Y > fullGreen.X && fullGreen.Y > baseline.Y, "actual green terrain tint");
					layer.RemoveResource("Tiberium", cell, green - green / 2);
					break;
				case 60:
					Check(lights.KnownResource(cell) == ("Tiberium", green / 2) && terrain.TintAt(position).Y < fullGreen.Y, "partial harvest dims light");
					layer.ClearResources(cell);
					layer.AddResource("BlueTiberium", cell, blue / 2 - 1);
					break;
				case 80:
					Check(lights.KnownResource(cell) == ("BlueTiberium", blue / 2), "type change to blue");
					var tint = terrain.TintAt(position);
					Check(tint.Z > tint.Y, "actual blue terrain tint");
					shroud.Disabled = false;
					break;
				case 100:
					Check(!layer.IsVisible(cell) && !layer.IsVisible(other), "fixture targets must be fogged");
					layer.ClearResources(cell);
					layer.AddResource("Tiberium", other, green - 1);
					break;
				case 120:
					Check(lights.KnownResource(cell) == ("BlueTiberium", blue / 2) && lights.KnownResource(other).Density == 0, "hidden harvest/growth leaked through fog");
					shroud.Disabled = true;
					break;
				case 140:
					Check(lights.KnownResource(cell).Density == 0 && lights.KnownResource(other) == ("Tiberium", green), "reveal failed to refresh");
					Check(terrain.TintAt(position) == baseline, "depleted light was not removed");
					layer.AddResource("BlueTiberium", cell, blue - 1);
					break;
				case 160:
					Check(lights.KnownResource(cell) == ("BlueTiberium", blue), "regrowth failed");
					PlateauMovementProbe.WriteResult(info.ResultPath, "TIBERIUM LIGHT PASS green/blue tint, partial harvest, depletion, replacement, regrowth and hidden harvest/growth/reveal.");
					break;
			}
		}
	}
}
