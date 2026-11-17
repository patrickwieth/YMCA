using System;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Diagnostic shoreline sight/source-removal assertions; never installed on ordinary maps.")]
	public class ShoreVisibilityProbeInfo : TraitInfo
	{
		public readonly CPos[] Cells = Array.Empty<CPos>();
		public readonly CPos HiddenCell = CPos.Zero;
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new ShoreVisibilityProbe(this);
	}

	public class ShoreVisibilityProbe : ITick
	{
		readonly ShoreVisibilityProbeInfo info;
		Player owner;
		public ShoreVisibilityProbe(ShoreVisibilityProbeInfo info) { this.info = info; }
		CPos[] Samples() => info.Cells.SelectMany((cell, i) => new[] { cell, cell + PlateauTopology.Directions[i % 4] }).ToArray();
		void ITick.Tick(Actor self)
		{
			var world = self.World;
			if (world.WorldTick == 20)
			{
				owner = world.LocalPlayer;
				if (owner == null || !owner.Shroud.FogEnabled || info.Cells.Length != 20)
					throw new InvalidOperationException("Shore sight fixture requires fog and twenty shoreline cells.");
				world.AddFrameEndTask(w =>
				{
					foreach (var cell in info.Cells)
						w.CreateActor("calibration.shore-vision", new TypeDictionary { new OwnerInit(owner), new LocationInit(cell) });
				});
			}
			if (world.WorldTick == 100 || world.WorldTick == 500)
			{
				if (!world.Map.Contains(info.HiddenCell) || owner.Shroud.IsVisible(info.HiddenCell) || owner.Shroud.IsExplored(info.HiddenCell))
					throw new InvalidOperationException("Shore sight fixture exposed its distant negative-control cell.");
				PlateauMovementProbe.WriteResult(info.ResultPath, "SHORE HIDDEN PASS tick=" + world.WorldTick);
			}
			if (world.WorldTick == 100)
				foreach (var cell in Samples())
				{
					if (!owner.Shroud.IsVisible(cell) || !owner.Shroud.IsExplored(cell))
						throw new InvalidOperationException("Shore source did not reveal/explore " + cell);
					PlateauMovementProbe.WriteResult(info.ResultPath, "SHORE SIGHT PASS " + cell);
				}
			if (world.WorldTick == 450)
			{
				// The movement probes and starting MCV also reveal. Remove all local
				// units only after their movement assertions have completed.
				var sources = world.Actors.Where(a => a.Owner == owner && a != owner.PlayerActor && a != world.WorldActor).ToArray();
				world.AddFrameEndTask(_ => { foreach (var actor in sources) actor.Dispose(); });
			}
			if (world.WorldTick == 500)
				foreach (var cell in Samples())
				{
					if (owner.Shroud.IsVisible(cell) || !owner.Shroud.IsExplored(cell))
						throw new InvalidOperationException("Shore source removal did not retain explored-but-fogged state at " + cell);
					PlateauMovementProbe.WriteResult(info.ResultPath, "SHORE FOG PASS " + cell);
				}
		}
	}
}
