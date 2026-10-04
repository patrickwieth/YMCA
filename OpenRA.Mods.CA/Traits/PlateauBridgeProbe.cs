using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class PlateauBridgeProbeInfo : TraitInfo
	{
		public readonly CPos Waypoint;
		public readonly CPos Target;
		public readonly string ResultPath = "";
		public readonly int StartDelay = 0;
		public override object Create(ActorInitializer init) => new PlateauBridgeProbe(this);
	}
	public class PlateauBridgeProbe : ITick
	{
		readonly PlateauBridgeProbeInfo info;
		CPos start;
		int ticks;
		int stage;
		int moveAt = 10;
		public PlateauBridgeProbe(PlateauBridgeProbeInfo info) { this.info = info; moveAt = info.StartDelay + 10; }
		void ITick.Tick(Actor self)
		{
			if (stage == 4) return;
			var map = self.World.Map;
			if (++ticks == 1) start = self.Location;
			if (ticks <= info.StartDelay) return;
			var cell = map.CellContaining(self.CenterPosition);
			if (map.Height[cell] != 4 || map.Ramp[cell] != 0 || self.CenterPosition.Z != 4 * 724)
				throw new InvalidOperationException("Bridge probe left the raised roof: " + self.Info.Name);
			var target = stage == 0 || stage == 2 ? info.Waypoint : stage == 1 ? info.Target : start;
			if (ticks == moveAt)
			{
				var mobile = self.Trait<Mobile>();
				var path = mobile.PathFinder.FindPathToTargetCell(self, new[] { self.Location }, target, BlockedByActor.Immovable);
				if (path.Count == 0 || path.Any(c => map.Height[c] != 4 || map.Ramp[c] != 0))
					throw new InvalidOperationException("Bridge route descends instead of crossing the joined roof.");
				self.QueueActivity(false, mobile.MoveTo(target));
			}
			if (self.Location == target && self.IsIdle)
			{
				PlateauMovementProbe.WriteResult(info.ResultPath, $"BRIDGE PASS {self.Info.Name}: segment {stage}, tick {ticks}.");
				stage++; moveAt = ticks + 20;
			}
			if (ticks > 1500) throw new InvalidOperationException("Bridge traversal timed out: " + self.Info.Name);
		}
	}
}
