using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.UtilityCommands;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Diagnostic real dock production, naval path and reciprocal travel-time checks.")]
	public class NavalRouteProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly int BodyRank = 0;
		public override object Create(ActorInitializer init) => new NavalRouteProbe(this);
	}

	public class NavalRouteProbe : ITick, IPostWorldLoaded
	{
		readonly NavalRouteProbeInfo info;
		readonly List<CPos> ports = new();
		readonly List<Actor> docks = new();
		readonly HashSet<Actor> produced = new();
		readonly HashSet<Actor> beforeProduction = new();
		readonly string[] units = { "dd", "lst" };
		readonly Dictionary<string, HashSet<string>> allowedTerrain = new();
		WorldRenderer renderer;
		Actor ship;
		CPos target;
		int ticks, phase, port, unit, phaseStart, forward;
		bool complete;
		public NavalRouteProbe(NavalRouteProbeInfo info) { this.info = info; }
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr) { renderer = wr; }
		void Record(string message) => PlateauMovementProbe.WriteResult(info.ResultPath, message);

		void Initialize(World world)
		{
			var map = world.Map;
			var owner = world.LocalPlayer;
			if (owner == null) throw new InvalidOperationException("Naval diagnostic needs a local owner.");
			owner.Shroud.ExploreAll();
			var locomotors = map.Rules.Actors["world"].TraitInfos<LocomotorInfo>().ToArray();
			Dictionary<CPos, int> Domain(string name)
			{
				var speeds = locomotors.Single(l => l.Name == name).TerrainSpeeds;
				return map.AllCells.Where(map.Contains).Where(c => speeds.TryGetValue(map.GetTerrainInfo(c).Type, out var s) && s.Speed > 0)
					.ToDictionary(c => c, c => speeds[map.GetTerrainInfo(c).Type].Speed);
			}
			foreach (var name in units)
			{
				var locomotor = map.Rules.Actors[name].TraitInfo<MobileInfo>().Locomotor;
				allowedTerrain[name] = locomotors.Single(l => l.Name == locomotor).TerrainSpeeds.Where(p => p.Value.Speed > 0).Select(p => p.Key).ToHashSet();
			}
			var water = Domain("naval");
			var land = Domain("foot");
			var bodies = RubberduckNavalAuditCommand.Bodies(water).OrderByDescending(b => b.Count).ToArray();
			if (info.BodyRank < 0 || info.BodyRank >= bodies.Length || bodies[info.BodyRank].Count < 9)
				throw new InvalidOperationException("No usable diagnostic water body.");
			var body = bodies[info.BodyRank];
			var dockRules = map.Rules.Actors["syrd"];
			var building = dockRules.TraitInfo<BuildingInfo>();
			var reserved = new HashSet<CPos>();
			var spawns = map.ActorDefinitions.Where(n => n.Value.Value == "mpspawn")
				.Select(n => new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value).ToArray();
			foreach (var spawn in spawns)
			{
				var walk = RubberduckNavalAuditCommand.Distances(land, new[] { spawn });
				var banks = walk.Where(p => !water.ContainsKey(p.Key) && RubberduckNavalAuditCommand.Steps.Any(s => body.Contains(p.Key + s))).ToArray();
				if (banks.Length == 0) throw new InvalidOperationException("Start cannot reach the selected sea: " + spawn);
				var candidates = banks.SelectMany(bank => Enumerable.Range(-4, 7).SelectMany(x => Enumerable.Range(-4, 7)
					.Select(y => (Dock: bank.Key + new CVec(x, y), Bank: bank.Key, Cost: bank.Value))))
					.Where(p => building.Tiles(p.Dock).All(c => body.Contains(c) && !reserved.Contains(c)))
					.OrderBy(p => p.Cost).ThenBy(p => (p.Dock + new CVec(1, 1) - p.Bank).LengthSquared)
					.ThenBy(p => p.Dock.X).ThenBy(p => p.Dock.Y).ToArray();
				var found = false;
				var placementRejected = 0; var launchRejected = 0; var graphRejected = 0; var landRejected = 0;
				foreach (var candidate in candidates)
				{
					if (!world.CanPlaceBuilding(candidate.Dock, dockRules, building, null)) { placementRejected++; continue; }
					var goals = body.Where(c => !reserved.Contains(c) && !ports.Contains(c) &&
						Math.Max(Math.Abs(c.X - candidate.Dock.X - 1), Math.Abs(c.Y - candidate.Dock.Y - 1)) == 3)
						.OrderBy(c => (c - candidate.Bank).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y).ToArray();
					if (goals.Length == 0) { launchRejected++; continue; }
					// A legal yard can still seal a narrow basin. Do not manufacture a
					// route failure by blocking earlier test ports with later fixtures.
					var occupied = reserved.Concat(building.Tiles(candidate.Dock)).ToHashSet();
					if (ports.Any(occupied.Contains)) { graphRejected++; continue; }
					var remaining = body.Where(c => !occupied.Contains(c)).ToDictionary(c => c, _ => 100);
					// A bank-nearest first launch can itself be trapped in a tiny pocket
					// behind its yard. Anchor every port in the remaining main sea, not
					// in whichever singleton the first tie-break happens to choose.
					var connected = RubberduckNavalAuditCommand.Bodies(remaining).OrderByDescending(b => b.Count).FirstOrDefault();
					if (connected == null || connected.Count < remaining.Count - Math.Max(8, body.Count / 100) || ports.Any(c => !connected.Contains(c)))
					{ graphRejected++; continue; }
					goals = goals.Where(connected.Contains).ToArray();
					if (goals.Length == 0) { graphRejected++; continue; }
					// CanPlaceBuilding only checks the footprint. Production chooses among
					// four occupied corner exits, so each needs a cardinal step into the
					// main sea; a diagonal-only gap would require cutting land or the yard.
					if (dockRules.TraitInfos<ExitInfo>().Any(exit => !RubberduckNavalAuditCommand.Steps
						.Where(s => s.X == 0 || s.Y == 0).Any(s => connected.Contains(candidate.Dock + exit.ExitCell + s))))
					{ launchRejected++; continue; }
					// Confirm bank access with the real height-aware foot pathfinder, not just the audit graph.
					var walker = world.CreateActor(false, "engineer", new TypeDictionary { new OwnerInit(owner), new LocationInit(spawn) });
					var path = walker.Trait<Mobile>().PathFinder.FindPathToTargetCell(walker, new[] { spawn }, candidate.Bank, BlockedByActor.Immovable);
					walker.Dispose();
					if (path.Count == 0 && spawn != candidate.Bank) { landRejected++; continue; }
					var dock = world.CreateActor("syrd", new TypeDictionary { new OwnerInit(owner), new LocationInit(candidate.Dock), new SkipMakeAnimsInit() });
					dock.Trait<RallyPoint>().Path = new List<CPos> { goals[0] };
					docks.Add(dock); ports.Add(goals[0]);
					foreach (var cell in building.Tiles(candidate.Dock)) reserved.Add(cell);
					Record($"NAVAL DOCK PASS player={ports.Count} dock={candidate.Dock} bank={candidate.Bank} accessCost={candidate.Cost} actualFootPath={path.Count} launch={goals[0]} bodyCells={body.Count} remainingSea={connected.Count}/{remaining.Count}");
					found = true;
					break;
				}
				if (!found) throw new InvalidOperationException($"No legal, reachable shipyard footprint for start {spawn}; banks={banks.Length} candidates={candidates.Length} placementRejected={placementRejected} launchRejected={launchRejected} graphRejected={graphRejected} landRejected={landRejected}.");
			}
			if (ports.Count < 2) throw new InvalidOperationException("At least two naval starts required.");
			renderer.Viewport.Center(map.CenterOfCell(ports[0]));
			phaseStart = ticks;
		}

		void Move(Actor self, CPos destination)
		{
			target = destination;
			var mobile = ship.Trait<Mobile>();
			var path = mobile.PathFinder.FindPathToTargetCell(ship, new[] { ship.Location }, target, BlockedByActor.Immovable);
			if (path.Count == 0 && ship.Location != target) throw new InvalidOperationException($"Real naval path missing: {ship.Location} -> {target}");
			Record($"NAVAL LEG unit={ship.Info.Name} from={ship.Location} to={target} pathCells={path.Count} startTick={ticks}");
			// Equal initial heading conventions keep reciprocal timing comparisons meaningful.
			mobile.Facing = (self.World.Map.CenterOfCell(target) - ship.CenterPosition).Yaw;
			ship.QueueActivity(false, mobile.MoveTo(target));
			phaseStart = ticks;
		}

		void ValidatePosition(Actor self)
		{
			var physical = self.World.Map.CellContaining(ship.CenterPosition);
			var terrain = self.World.Map.GetTerrainInfo(physical).Type;
			if (!allowedTerrain[units[unit]].Contains(terrain) || ship.CenterPosition.Z != 0)
				throw new InvalidOperationException($"Ship left movement domain: unit={ship.Info.Name} logical={ship.Location} physical={physical}/{terrain}/{ship.CenterPosition} target={target} phase={phase} tick={ticks}");
		}

		void ITick.Tick(Actor self)
		{
			if (complete) return;
			ticks++;
			if (ticks < 10) return;
			if (ticks == 10) { self.World.AddFrameEndTask(Initialize); return; }
			if (ticks - phaseStart > (phase == 1 ? 500 : 6000))
				throw new InvalidOperationException($"Naval diagnostic timeout phase={phase} port={port} unit={units[unit]} ship={ship?.Info.Name} location={ship?.Location} idle={ship?.IsIdle} activity={ship?.CurrentActivity?.GetType().Name} launch={ports[port]}");
			if (phase == 0)
			{
				if (ticks - phaseStart < 20) return;
				// Do not mistake an unrelated existing ship (for example a start unit)
				// for this yard's newly produced vessel.
				beforeProduction.Clear();
				beforeProduction.UnionWith(self.World.Actors);
				if (!docks[port].Trait<Production>().Produce(docks[port], self.World.Map.Rules.Actors[units[unit]], "Ship",
					new TypeDictionary { new OwnerInit(self.World.LocalPlayer) }, 0))
					throw new InvalidOperationException("Actual naval production exit is blocked at " + docks[port].Location);
				phase = 1; phaseStart = ticks; return;
			}
			if (phase == 1)
			{
				if (ship == null)
					ship = self.World.Actors.SingleOrDefault(a => a.Info.Name == units[unit] && a.Owner == self.World.LocalPlayer &&
						!beforeProduction.Contains(a) && !produced.Contains(a) && (a.Location - docks[port].Location).LengthSquared <= 25);
				if (ship == null) return;
				ValidatePosition(self);
				if (!ship.IsIdle || ship.Location != ports[port]) return;
				produced.Add(ship);
				Record($"NAVAL PRODUCTION PASS player={port + 1} unit={units[unit]} launchTicks={ticks - phaseStart}");
				phase = 2; Move(self, ports[(port + 1) % ports.Count]); return;
			}
			if (ship == null || ship.IsDead) throw new InvalidOperationException("Naval test ship was lost.");
			ValidatePosition(self);
			if (!ship.IsIdle || ship.Location != target) return;
			if (phase == 2)
			{
				forward = ticks - phaseStart;
				phase = 3; Move(self, ports[port]); return;
			}
			var reverse = ticks - phaseStart;
			Record($"NAVAL ROUTE player={port + 1} to={(port + 1) % ports.Count + 1} unit={units[unit]} forwardTicks={forward} reverseTicks={reverse}");
			if (Math.Abs(forward - reverse) > Math.Max(25, Math.Min(forward, reverse) / 10))
				throw new InvalidOperationException("Reciprocal naval times differ by more than 10%/25 ticks.");
			ship.Dispose(); ship = null;
			port++;
			if (port == ports.Count) { port = 0; unit++; }
			if (unit == units.Length)
			{
				complete = true;
				Record($"NAVAL COMPLETE docks={docks.Count} productions={produced.Count} legs={produced.Count * 2}; real reciprocal times, not universal strategic fairness.");
				Game.TakeScreenshot(); return;
			}
			phase = 0; phaseStart = ticks;
		}
	}
}
