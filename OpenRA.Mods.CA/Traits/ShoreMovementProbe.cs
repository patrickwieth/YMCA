using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Runtime movement assertions for isolated shore fixtures only.")]
	public class ShoreMovementProbeInfo : TraitInfo
	{
		public readonly CPos Target = CPos.Zero;
		public readonly CPos Forbidden = CPos.Zero;
		public readonly bool Water = false;
		public readonly string ResultPath = "";
		public readonly int ForbiddenTerrainType = -1;
		public override object Create(ActorInitializer init) => new ShoreMovementProbe(this);
	}

	public class ShoreMovementProbe : ITick
	{
		readonly ShoreMovementProbeInfo info;
		int ticks;
		bool complete;

		public ShoreMovementProbe(ShoreMovementProbeInfo info) { this.info = info; }

		void ITick.Tick(Actor self)
		{
			if (complete) return;
			ticks++;
			if (ticks < 10) return;
			var mobile = self.Trait<Mobile>();
			var tile = self.World.Map.Tiles[self.Location].Type;
			var isWater = tile == 1050 || tile == 1060 || tile == 1070 || tile == 1080;
			if (isWater != info.Water || self.World.Map.Tiles[self.Location].Type == info.ForbiddenTerrainType)
				throw new InvalidOperationException("Shore probe entered the wrong terrain domain: " + self.Info.Name);
			if (info.Water)
			{
				var physical = self.World.Map.CellContaining(self.CenterPosition);
				var physicalTile = self.World.Map.Tiles[physical].Type;
				if ((physicalTile != 1050 && physicalTile != 1060 && physicalTile != 1070 && physicalTile != 1080) || self.CenterPosition.Z != 0)
					throw new InvalidOperationException("Water probe physically left its height-zero water surface: " + self.Info.Name);
			}
			if (ticks == 10)
			{
				var path = mobile.PathFinder.FindPathToTargetCell(self, new[] { self.Location }, info.Target, BlockedByActor.Immovable);
				var forbiddenPath = mobile.PathFinder.FindPathToTargetCell(self, new[] { self.Location }, info.Forbidden, BlockedByActor.Immovable);
				if (path.Count == 0 || forbiddenPath.Count != 0 || mobile.CanEnterCell(info.Forbidden))
					throw new InvalidOperationException($"Shore probe path assertion failed: {self.Info.Name}; " +
						$"start={self.Location}, target={info.Target}, targetTile={self.World.Map.Tiles[info.Target].Type}, " +
						$"path={path.Count}, forbiddenPath={forbiddenPath.Count}, canEnterForbidden={mobile.CanEnterCell(info.Forbidden)}.");
				self.QueueActivity(false, mobile.MoveTo(info.Target));
			}
			if (self.Location == info.Target && self.IsIdle)
			{
				complete = true;
				var message = $"SHORE PROBE PASS {self.Info.Name}: arrived {info.Target} at tick {ticks}; forbidden {info.Forbidden} rejected.";
				Log.Write("debug", message);
				if (!string.IsNullOrEmpty(info.ResultPath)) PlateauMovementProbe.WriteResult(info.ResultPath, message);
				Game.TakeScreenshot();
			}
			else if (ticks > 1500)
				throw new InvalidOperationException("Shore probe movement timed out: " + self.Info.Name);
		}
	}
}
