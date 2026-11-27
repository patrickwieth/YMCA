using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Isolated handcrafted-map acceptance probe; never included in production map rules.")]
	public sealed class HandcraftedRuntimeProbeInfo : TraitInfo
	{
		public readonly bool ExpectedOilGroundAccess = true;
		public readonly bool CheckHarvestRoutes = false;
		public override object Create(ActorInitializer init) => new HandcraftedRuntimeProbe(this);
	}

	public sealed class HandcraftedRuntimeProbe : ITick
	{
		readonly List<(Actor Actor, CPos Target)> movers = new();
		readonly List<object> routes = new();
		readonly HashSet<Actor> arrived = new();
		readonly HandcraftedRuntimeProbeInfo info;
		public HandcraftedRuntimeProbe(HandcraftedRuntimeProbeInfo info) { this.info = info; }
		bool started;
		bool finished;
		int startTick;

		void ITick.Tick(Actor self)
		{
			if (finished || self.World.WorldTick < 10)
				return;
			if (!started)
			{
				started = true;
				self.World.AddFrameEndTask(w => Start(w));
				return;
			}

			if (movers.Count == 0)
				return;
			foreach (var mover in movers.Where(m => !arrived.Contains(m.Actor)))
			{
				if (!self.World.Map.Contains(mover.Actor.Location))
					throw new InvalidOperationException("Native unit escaped handcrafted footprint.");
				if (mover.Actor.Location == mover.Target)
				{
					arrived.Add(mover.Actor);
					self.World.AddFrameEndTask(_ => mover.Actor.Dispose());
				}
			}
			if (info.CheckHarvestRoutes && self.World.WorldTick % 250 == 0)
				File.WriteAllText(Path.Combine(Platform.SupportDir, "harvest-movement-progress.json"), JsonConvert.SerializeObject(
					movers.Where(m => !arrived.Contains(m.Actor)).Select(m => new { Unit = m.Actor.Info.Name,
						Position = m.Actor.Location.ToString(), Target = m.Target.ToString(), Activity = m.Actor.CurrentActivity?.GetType().Name }), Formatting.Indented));
			if (movers.All(m => arrived.Contains(m.Actor)))
			{
				finished = true;
				var output = Path.Combine(Platform.SupportDir, "handcrafted-runtime.json");
				File.WriteAllText(output, JsonConvert.SerializeObject(new
				{
					Map = self.World.Map.Title, Passed = true, NativeRoutes = routes,
					ActualMovers = movers.Select(m => m.Actor.Info.Name).ToArray(),
					ArrivalTicks = self.World.WorldTick - startTick,
				}, Formatting.Indented));
			}
			else if (self.World.WorldTick - startTick > 6000)
				throw new InvalidOperationException("Handcrafted real movement timed out: " + string.Join("; ", movers.Select(m => m.Actor.Info.Name + " " + m.Actor.Location + " -> " + m.Target)));
		}

		void Start(World world)
		{
			var starts = world.Map.ActorDefinitions.Where(n => n.Value.Value == "mpspawn")
				.Select(n => new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value).ToArray();
			var yards = world.Map.Rules.Actors.Values.Where(a => !a.Name.StartsWith("^", StringComparison.Ordinal) &&
				a.HasTraitInfo<BaseBuildingInfo>() && a.HasTraitInfo<BuildingInfo>()).ToArray();
			foreach (var yard in yards)
				foreach (var start in starts)
				{
					var building = yard.TraitInfo<BuildingInfo>();
					var existingMcv = world.Actors.FirstOrDefault(a => a.Info.HasTraitInfo<BaseBuildingInfo>() && a.TraitOrDefault<Mobile>() != null && (a.Location - start).LengthSquared < 36);
					if (!world.Map.FindTilesInCircle(start, 5).Any(c => world.CanPlaceBuilding(c, yard, building, existingMcv)))
						throw new InvalidOperationException("No legal existing construction-yard footprint near start " + start + ": " + yard.Name);
				}
			routes.Add(new { ConstructionYardTypes = yards.Length, StartsWithLegalFootprint = starts.Length, FootprintsModified = false });
			if (info.CheckHarvestRoutes)
			{
				var resources = world.Map.AllCells.Where(world.Map.Contains).Where(c => world.Map.Resources[c].Type != 0).ToArray();
				for (var i = 0; i < starts.Length; i++)
				{
					var harvester = world.CreateActor("harv", new TypeDictionary { new OwnerInit(world.LocalPlayer), new LocationInit(starts[i]) });
					var mobile = harvester.Trait<Mobile>();
					var target = default(CPos); List<CPos> bestPath = null;
					int LowerBound(CPos c) => Math.Max(Math.Abs(c.X - starts[i].X), Math.Abs(c.Y - starts[i].Y));
					// Do not truncate by straight-line proximity: a deposit under the
					// back of a cliff may be nearby on screen but far away by road.
					foreach (var cell in resources.OrderBy(LowerBound))
					{
						if (bestPath != null && LowerBound(cell) >= bestPath.Count) break;
						var path = mobile.PathFinder.FindPathToTargetCell(harvester, new[] { starts[i] }, cell, BlockedByActor.Immovable);
						if (path.Count > 0 && (bestPath == null || path.Count < bestPath.Count)) { bestPath = path; target = cell; }
					}
					if (bestPath == null || bestPath.Count > 60)
						throw new InvalidOperationException("No short native harvester route near start " + (i + 1));
					routes.Add(new { Unit = "harv", Start = i + 1, Cells = bestPath.Count, Resource = target.ToString() });
					harvester.QueueActivity(false, mobile.MoveTo(target));
					movers.Add((harvester, target));
				}
			}
			foreach (var type in new[] { "engineer", "jeep", "mtnk" })
			{
				var probe = world.CreateActor(false, type, new TypeDictionary { new OwnerInit(world.LocalPlayer), new LocationInit(starts[0]) });
				var mobile = probe.Trait<Mobile>();
				var approaches = starts.Select(start => world.Map.FindTilesInCircle(start, 5)
					.Where(c => mobile.CanEnterCell(c) && movers.All(m => (m.Target - c).LengthSquared >= 16)).First()).ToArray();
				for (var i = 0; i < approaches.Length; i++)
					for (var j = i + 1; j < approaches.Length; j++)
					{
						var path = mobile.PathFinder.FindPathToTargetCell(probe, new[] { approaches[i] }, approaches[j], BlockedByActor.Immovable);
						if (path.Count == 0 || path.Any(c => !world.Map.Contains(c)))
							throw new InvalidOperationException($"Native {type} route failed between starts {i + 1} and {j + 1}.");
						routes.Add(new { Unit = type, From = i + 1, To = j + 1, Cells = path.Count });
					}
				if (type == "engineer")
					foreach (var oil in world.Actors.Where(a => a.Info.Name == "oilb"))
					{
						var footprint = oil.Info.TraitInfo<BuildingInfo>().Tiles(oil.Location).ToHashSet();
						var banks = footprint.SelectMany(c => Enumerable.Range(-1, 3).SelectMany(x => Enumerable.Range(-1, 3).Select(y => c + new CVec(x, y))))
							.Distinct().Where(c => !footprint.Contains(c) && world.Map.Contains(c) && mobile.CanEnterCell(c)).ToArray();
						foreach (var start in approaches)
						{
							var reachable = banks.Any(c => c == start || mobile.PathFinder.FindPathToTargetCell(probe, new[] { start }, c, BlockedByActor.Immovable).Count > 0);
							if (reachable != info.ExpectedOilGroundAccess)
								throw new InvalidOperationException("Oil access differs from the original map at " + oil.Location + " from " + start);
						}
						routes.Add(new { Unit = type, OilDerrick = oil.Location.ToString(), OriginalGroundAccessPreserved = true, GroundReachable = info.ExpectedOilGroundAccess, CheckedStarts = approaches.Length });
					}
				probe.Dispose();
				var mover = world.CreateActor(type, new TypeDictionary { new OwnerInit(world.LocalPlayer), new LocationInit(approaches[0]) });
				mover.QueueActivity(false, mover.Trait<Mobile>().MoveTo(approaches[approaches.Length - 1]));
				movers.Add((mover, approaches[approaches.Length - 1]));
			}
			// Also test a genuine naval route on the largest original water body.
			var water = world.Map.AllCells.Where(world.Map.Contains).Where(c => world.Map.GetTerrainInfo(c).Type == "Water").ToHashSet();
			if (water.Count > 0)
			{
				var components = new List<List<CPos>>();
				while (water.Count > 0)
				{
					var group = new List<CPos>(); var pending = new Queue<CPos>();
					var first = water.OrderBy(c => c.Y).ThenBy(c => c.X).First(); water.Remove(first); pending.Enqueue(first);
					while (pending.Count > 0)
					{
						var p = pending.Dequeue(); group.Add(p);
						foreach (var d in new[] { new CVec(1, 0), new CVec(-1, 0), new CVec(0, 1), new CVec(0, -1) })
							if (water.Remove(p + d)) pending.Enqueue(p + d);
					}
					components.Add(group);
				}
				var body = components.OrderByDescending(g => g.Count).First();
				if (body.Count >= 16)
				{
					var source = body.First(); var target = body.OrderByDescending(c => (c - source).LengthSquared).First();
					var boat = world.CreateActor("dd", new TypeDictionary { new OwnerInit(world.LocalPlayer), new LocationInit(source) });
					var mobile = boat.Trait<Mobile>();
					var path = mobile.PathFinder.FindPathToTargetCell(boat, new[] { source }, target, BlockedByActor.Immovable);
					if (path.Count == 0 || path.Any(c => !world.Map.Contains(c))) throw new InvalidOperationException("Native naval route failed.");
					routes.Add(new { Unit = "dd", WaterBodyCells = body.Count, Cells = path.Count });
					boat.QueueActivity(false, mobile.MoveTo(target)); movers.Add((boat, target));
				}
			}
			startTick = world.WorldTick;
		}
	}
}
