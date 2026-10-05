using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Asserts real ascent and descent in isolated plateau calibration maps.")]
	public class PlateauMovementProbeInfo : TraitInfo
	{
		public readonly CPos Target = CPos.Zero;
		public readonly byte Ramp = 1;
		public readonly CPos CliffTop = CPos.Zero;
		public readonly CPos CliffBottom = CPos.Zero;
		public readonly bool ObserveArmyFollower = false;
		public readonly bool BlockedCliffFoot = false;
		public readonly bool ValidateRampGuards = false;
		public readonly bool LoweredCliffFoot = false;
		public readonly bool GroundRoute = false;
		public readonly bool ValidateTerrainTilt = false;
		public readonly bool ExpectTerrainTilt = false;
		public readonly bool ParkOnRampAfterTest = false;
		public readonly CPos ParkingCell = CPos.Zero;
		public readonly byte ParkingHeight = 1;
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new PlateauMovementProbe(this);
	}

	public class PlateauMovementProbe : ITick
	{
		readonly PlateauMovementProbeInfo info;
		CPos start;
		CPos previous;
		int ticks;
		int visitedLevels;
		int stage;
		int returnAt;
		bool onSlope;
		bool observedTilt;
		bool groundTargetVisited;
		int parkingTicks;

		public PlateauMovementProbe(PlateauMovementProbeInfo info) { this.info = info; }

		internal static void WriteResult(string path, string message)
		{
			Log.Write("debug", message);
			if (string.IsNullOrEmpty(path)) return;
			// Map rules must not gain arbitrary filesystem write access through a probe.
			var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ymca-terrain-probes"));
			var fullPath = System.IO.Path.GetFullPath(path);
			var name = System.IO.Path.GetFileNameWithoutExtension(fullPath);
			var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			if (!string.Equals(System.IO.Path.GetDirectoryName(fullPath), root, comparison) ||
				System.IO.Path.GetExtension(fullPath) != ".log" || name.Length != 64 || !name.All(Uri.IsHexDigit))
				throw new InvalidOperationException("Terrain probe results must use the dedicated temporary log directory.");
			System.IO.File.AppendAllText(fullPath, message + Environment.NewLine);
		}

		void TickLowered(Actor self)
		{
			if (stage == 2) return;
			var map = self.World.Map;
			var mobile = self.Trait<Mobile>();
			if (++ticks == 1)
			{
				start = self.Location;
				if (start != info.CliffTop || map.Height[start] != 0 || map.Height[info.CliffBottom] != 0 ||
					map.Ramp[start] != 0 || map.Ramp[info.CliffBottom] != 0 || map.Tiles[start].Type != 1000 || map.Tiles[info.CliffBottom].Type != 1000 ||
					mobile.Locomotor.MovementCostToEnterCell(self, start, info.CliffBottom, BlockedByActor.None, null) == PathGraph.MovementCostForUnreachableCell ||
					mobile.Locomotor.MovementCostToEnterCell(self, info.CliffBottom, start, BlockedByActor.None, null) == PathGraph.MovementCostForUnreachableCell)
					throw new InvalidOperationException("Lowered cliff still blocks entry: " + self.Info.Name);
				WriteResult(info.ResultPath, "LOWERED FOOT PASS " + self.Info.Name);
			}
			var target = stage == 0 ? info.CliffBottom : start;
			if (ticks == 10 || (stage == 1 && ticks == returnAt))
			{
				var path = mobile.PathFinder.FindPathToTargetCell(self, new[] { self.Location }, target, BlockedByActor.Immovable);
				if (path.Count == 0 || path.Any(c => map.Height[c] != 0 || map.Ramp[c] != 0))
					throw new InvalidOperationException("Lowered cliff path is not flat: " + self.Info.Name);
				self.QueueActivity(false, mobile.MoveTo(target));
			}
			if (self.Location == target && self.IsIdle)
			{
				if (self.CenterPosition.Z != 0 || Math.Abs(map.DistanceAboveTerrain(self.CenterPosition).Length) > 2)
					throw new InvalidOperationException("Lowered cliff vehicle is not on the ground.");
				WriteResult(info.ResultPath, $"LOWERED MOVEMENT PASS {self.Info.Name}: leg {stage}, tick {ticks}.");
				stage++;
				returnAt = ticks + 25;
			}
			if (ticks > 600) throw new InvalidOperationException("Lowered cliff probe timed out: " + self.Info.Name);
		}

		void TickGroundRoute(Actor self)
		{
			if (stage == 2) return;
			var map = self.World.Map;
			var mobile = self.Trait<Mobile>();
			if (++ticks == 1)
			{
				start = self.Location;
				if (map.Tiles[info.CliffTop].Type != 3992 || map.Height[info.CliffTop] != 4 || mobile.CanEnterCell(info.CliffTop))
					throw new InvalidOperationException("Raised mountain barrier admits a ground unit.");
				WriteResult(info.ResultPath, "GROUND BARRIER PASS " + self.Info.Name);
			}
			var physical = map.CellContaining(self.CenterPosition);
			if (map.Height[physical] != 0 || map.Ramp[physical] != 0 || map.Tiles[physical].Type == 3992)
				throw new InvalidOperationException($"Ground route entered raised or blocked terrain: {self.Info.Name}, tick {ticks}, physical {physical}, location {self.Location}, previous {previous}.");
			previous = physical;
			var target = stage == 0 ? info.Target : start;
			if (!groundTargetVisited && ticks >= 10 && physical == target && self.Location == target)
			{
				groundTargetVisited = true;
				WriteResult(info.ResultPath, $"GROUND TARGET VISIT {self.Info.Name}: stage={stage}, tick={ticks}, idle={self.IsIdle}.");
			}
			if (ticks == 10 || stage == 1 && ticks == returnAt)
			{
				var path = mobile.PathFinder.FindPathToTargetCell(self, new[] { self.Location }, target, BlockedByActor.Immovable);
				if (path.Count == 0 || path.Any(c => map.Height[c] != 0 || map.Ramp[c] != 0 || map.Tiles[c].Type == 3992))
					throw new InvalidOperationException("Mountain rendering broke a flat ground route.");
				self.QueueActivity(false, mobile.MoveTo(target));
			}
			// Remember an actual physical visit: a friendly vehicle may nudge
			// infantry before its move activity becomes idle. Merely stopping
			// near the destination never satisfies groundTargetVisited.
			if (ticks >= 10 && groundTargetVisited && self.IsIdle)
			{
				if (self.CenterPosition.Z != 0 || Math.Abs(map.DistanceAboveTerrain(self.CenterPosition).Length) > 2)
					throw new InvalidOperationException("Ground route unit lost contact with terrain.");
				WriteResult(info.ResultPath, $"GROUND ROUTE PASS {self.Info.Name}: leg {stage}, tick {ticks}.");
				stage++; groundTargetVisited = false; returnAt = ticks + 25;
			}
			if (ticks % 1500 == 0)
			{
				WriteResult(info.ResultPath, $"GROUND ROUTE WAIT {self.Info.Name}: stage={stage}, location={self.Location}, physical={physical}, target={target}, idle={self.IsIdle}, position={self.CenterPosition}.");
				if (self.IsIdle)
					WriteResult(info.ResultPath, "GROUND ROUTE NEAR " + string.Join("; ", self.World.Actors.Where(a => a.IsInWorld && a.TraitOrDefault<Mobile>() != null && (a.Location - target).LengthSquared <= 16).Select(a => a.Info.Name + "@" + a.Location)) + "; enter=" + mobile.CanEnterCell(target) + "; path=" + string.Join("/", mobile.PathFinder.FindPathToTargetCell(self, new[] { self.Location }, target, BlockedByActor.Immovable)));
			}
			if (ticks > 6000) throw new InvalidOperationException($"Ground route probe timed out: {self.Info.Name}, stage={stage}, location={self.Location}, physical={physical}, target={target}, idle={self.IsIdle}.");
		}

		void ITick.Tick(Actor self)
		{
			if (info.GroundRoute) { TickGroundRoute(self); return; }
			if (info.LoweredCliffFoot) { TickLowered(self); return; }
			if (stage == 3) return;
			var map = self.World.Map;
			if (stage == 2)
			{
				if (!info.ParkOnRampAfterTest) return;
				if (++parkingTicks > 500) throw new InvalidOperationException("Vehicle did not park on its ramp.");
				if (self.IsIdle)
				{
					var orientation = self.Trait<BodyOrientation>().QuantizeOrientation(self.Orientation);
					var tilted = orientation.Pitch != WAngle.Zero || orientation.Roll != WAngle.Zero;
					if (map.Ramp[self.Location] != info.Ramp || map.Height[self.Location] != info.ParkingHeight ||
						tilted != info.ExpectTerrainTilt || Math.Abs(map.DistanceAboveTerrain(self.CenterPosition).Length) > 2)
						throw new InvalidOperationException("Stationary vehicle slope contact assertion failed: " + self.Info.Name);
					WriteResult(info.ResultPath, $"PLATEAU PARK PASS {self.Info.Name}: pitch={orientation.Pitch}, roll={orientation.Roll}, position={self.CenterPosition}.");
					stage = 3;
				}
				return;
			}
			var mobile = self.Trait<Mobile>();
			ticks++;
			if (ticks == 1) start = previous = map.CellContaining(self.CenterPosition);
			if (ticks < 10) return;
			var physicalCell = map.CellContaining(self.CenterPosition);
			if (map.Tiles[physicalCell].Type == 3992 || Math.Abs(map.Height[physicalCell] - map.Height[previous]) > 1)
				throw new InvalidOperationException($"Plateau probe crossed a steep wall: {self.Info.Name}, tick={ticks}, stage={stage}, previous={previous}/h{map.Height[previous]}, physical={physicalCell}/h{map.Height[physicalCell]}/tile{map.Tiles[physicalCell].Type}, location={self.Location}, position={self.CenterPosition}.");
			previous = physicalCell;
			if (info.ObserveArmyFollower)
			{
				if (ticks == 1500)
				{
					if ((physicalCell - start).LengthSquared < 16 || self.Trait<ArmyFollower>().IsTraitDisabled)
						throw new InvalidOperationException("Army follower did not exercise its route: " + self.Info.Name);
					WriteResult(info.ResultPath, $"PLATEAU FOLLOW PASS {self.Info.Name}: 1500 ticks of actual ArmyFollower movement without cliff entry or elevation jumps.");
					stage = 2;
				}
				return;
			}
			if (map.Ramp[physicalCell] == info.Ramp)
			{
				visitedLevels |= 1 << map.Height[physicalCell];
				var renderedOrientation = self.Trait<BodyOrientation>().QuantizeOrientation(self.Orientation);
				var tilted = renderedOrientation.Pitch != WAngle.Zero || renderedOrientation.Roll != WAngle.Zero;
				observedTilt |= tilted;
				if (info.ValidateTerrainTilt && !info.ExpectTerrainTilt && tilted)
					throw new InvalidOperationException("Sprite vehicle unexpectedly tilted: " + self.Info.Name);
				if (self.CenterPosition.Z > 0 && self.CenterPosition.Z < 4 * 724) onSlope = true;
			}
			void Move(CPos target)
			{
				var path = mobile.PathFinder.FindPathToTargetCell(self, new[] { self.Location }, target, BlockedByActor.Immovable);
				if (path.Count == 0 || !path.Any(c => map.Ramp[c] == info.Ramp))
					throw new InvalidOperationException("Plateau probe did not route through its ascent: " + self.Info.Name);
				Log.Write("debug", $"PLATEAU PATH {self.Info.Name}: {string.Join(";", path.Select(c => $"{c}/h{map.Height[c]}/r{map.Ramp[c]}"))}");
				self.QueueActivity(false, mobile.MoveTo(target));
			}
			if (ticks == 10)
			{
				if (info.ValidateRampGuards)
				{
					var ramps = map.AllCells.Where(c => map.Contains(c) && map.Ramp[c] == info.Ramp).ToArray();
					var guards = map.AllCells.Where(c => map.Contains(c) && map.Height[c] == 4 && map.Tiles[c].Type == 3992).ToArray();
					if (ramps.Length != 12 || Enumerable.Range(0, 4).Any(h => ramps.Count(c => map.Height[c] == h) != 3) ||
						guards.Length != 8 || guards.Any(c => mobile.CanEnterCell(c)))
						throw new InvalidOperationException("Three-wide ramp lanes or retaining guards are invalid.");
					WriteResult(info.ResultPath, "PLATEAU RAMP GUARDS PASS " + self.Info.Name + ": 12 ramp cells, 8 blocked guards.");
				}
				if (info.BlockedCliffFoot)
				{
					var approach = info.CliffBottom + (info.CliffBottom - info.CliffTop);
					if (map.Height[info.CliffTop] != 4 || map.Tiles[info.CliffTop].Type != 1000 ||
						map.Height[info.CliffBottom] != 0 || map.Tiles[info.CliffBottom].Type != 3992 ||
						map.Height[approach] != 0 || map.Tiles[approach].Type != 1000 ||
						mobile.Locomotor.MovementCostToEnterCell(self, info.CliffTop, info.CliffBottom, BlockedByActor.None, null) != PathGraph.MovementCostForUnreachableCell ||
						mobile.Locomotor.MovementCostToEnterCell(self, approach, info.CliffBottom, BlockedByActor.None, null) != PathGraph.MovementCostForUnreachableCell ||
						mobile.PathFinder.FindPathToTargetCell(self, new[] { approach }, info.CliffBottom, BlockedByActor.Immovable).Count != 0)
						throw new InvalidOperationException("Native cliff foot admits entry from high or low terrain: " + self.Info.Name);
					WriteResult(info.ResultPath, $"PLATEAU FOOT PASS {self.Info.Name}: native foot rejects high/low entry and pathfinding.");
				}
				else
				{
					if (map.Height[info.CliffTop] != 4 || map.Height[info.CliffBottom] != 0 ||
						map.Tiles[info.CliffTop].Type != 1000 || map.Tiles[info.CliffBottom].Type != 1000 ||
						mobile.Locomotor.MovementCostToEnterCell(self, info.CliffTop, info.CliffBottom, BlockedByActor.None, null) != PathGraph.MovementCostForUnreachableCell ||
						mobile.Locomotor.MovementCostToEnterCell(self, info.CliffBottom, info.CliffTop, BlockedByActor.None, null) != PathGraph.MovementCostForUnreachableCell)
						throw new InvalidOperationException("Plateau steep edge was not blocked in both directions: " + self.Info.Name);
					WriteResult(info.ResultPath, $"PLATEAU EDGE PASS {self.Info.Name}: clear-terrain height discontinuity rejected both ways.");
				}
				Move(info.Target);
			}
			if (stage == 1 && ticks == returnAt) Move(start);
			var target = stage == 0 ? info.Target : start;
			if (self.Location == target && self.IsIdle)
			{
				if (visitedLevels != 15 || !onSlope || Math.Abs(map.DistanceAboveTerrain(self.CenterPosition).Length) > 2 ||
					self.CenterPosition.Z != (stage == 0 ? 4 * 724 : 0))
					throw new InvalidOperationException($"Plateau elevation assertion failed: {self.Info.Name}, levels={visitedLevels}, Z={self.CenterPosition.Z}.");
				if (info.ValidateTerrainTilt)
				{
					if (observedTilt != info.ExpectTerrainTilt)
						throw new InvalidOperationException("Vehicle terrain tilt assertion failed: " + self.Info.Name);
					WriteResult(info.ResultPath, $"PLATEAU TILT PASS {self.Info.Name}: expected={info.ExpectTerrainTilt}, observed={observedTilt}.");
				}
				WriteResult(info.ResultPath, $"PLATEAU PROBE PASS {self.Info.Name}: {(stage == 0 ? "ascent" : "descent")}, ramp {info.Ramp}, all four levels, tick {ticks}.");
				stage++;
				visitedLevels = 0;
				onSlope = false;
				observedTilt = false;
				if (stage == 1) returnAt = ticks + 100;
				else if (info.ParkOnRampAfterTest)
				{
					var path = mobile.PathFinder.FindPathToTargetCell(self, new[] { self.Location }, info.Target, BlockedByActor.Immovable);
					var parkingCell = info.ParkingCell != CPos.Zero ? info.ParkingCell :
						path.First(c => map.Ramp[c] == info.Ramp && map.Height[c] == info.ParkingHeight);
					self.QueueActivity(false, mobile.MoveTo(parkingCell));
				}
			}
			if (ticks > 2000) throw new InvalidOperationException("Plateau probe timed out: " + self.Info.Name);
		}
	}
}
