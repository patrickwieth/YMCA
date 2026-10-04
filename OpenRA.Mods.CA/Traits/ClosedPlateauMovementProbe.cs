using System;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class ClosedPlateauMovementProbeInfo : TraitInfo
	{
		public readonly CPos Target = CPos.Zero;
		public readonly byte Height = 0;
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new ClosedPlateauMovementProbe(this);
	}
	public class ClosedPlateauMovementProbe : ITick
	{
		readonly ClosedPlateauMovementProbeInfo info;
		CPos start;
		int ticks, stage, next = 10;
		bool visited;
		public ClosedPlateauMovementProbe(ClosedPlateauMovementProbeInfo info) { this.info = info; }
		void ITick.Tick(Actor self)
		{
			if (stage == 2) return;
			var map = self.World.Map; var mobile = self.Trait<Mobile>();
			if (++ticks == 1)
			{
				start = self.Location; var feet = 0;
				foreach (var c in map.AllCells.Where(c => map.Contains(c) && map.Tiles[c].Type == 3992))
				{
					if (map.Height[c] != 0 || mobile.CanEnterCell(c)) throw new InvalidOperationException("Closed cliff foot admits a vehicle.");
					foreach (var d in PlateauTopology.Directions)
						if (mobile.Locomotor.MovementCostToEnterCell(self, c + d, c, BlockedByActor.None, null) != PathGraph.MovementCostForUnreachableCell)
							throw new InvalidOperationException("Closed foot admits cardinal entry.");
					feet++;
				}
				if (feet == 0) throw new InvalidOperationException("Missing closed cliff feet.");
				PlateauMovementProbe.WriteResult(info.ResultPath, $"CLOSED FOOT PASS {self.Info.Name}: {feet} blocked feet, all four entry directions.");
			}
			var physical = map.CellContaining(self.CenterPosition);
			if (map.Height[physical] != info.Height || map.Ramp[physical] != 0 || map.Tiles[physical].Type == 3992 || Math.Abs(map.DistanceAboveTerrain(self.CenterPosition).Length) > 2)
				throw new InvalidOperationException("Closed plateau movement left its physical surface.");
			var target = stage == 0 ? info.Target : start;
			if (ticks == next)
			{
				var path = mobile.PathFinder.FindPathToTargetCell(self, new[] { self.Location }, target, BlockedByActor.Immovable);
				if (path.Count == 0 || path.Any(c => map.Height[c] != info.Height || map.Tiles[c].Type == 3992)) throw new InvalidOperationException("Closed plateau route is invalid.");
				self.QueueActivity(false, mobile.MoveTo(target));
			}
			visited |= ticks >= next && physical == target && self.Location == target;
			if (visited && self.IsIdle)
			{
				PlateauMovementProbe.WriteResult(info.ResultPath, $"CLOSED MOVEMENT PASS {self.Info.Name}: height {info.Height}, leg {stage}, tick {ticks}.");
				stage++; visited = false; next = ticks + 25;
			}
			if (ticks > 2400) throw new InvalidOperationException("Closed plateau movement timed out.");
		}
	}
}
