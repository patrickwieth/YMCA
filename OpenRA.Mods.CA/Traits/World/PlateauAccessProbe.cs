using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class PlateauAccessProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly bool CheckSsm = false;
		public override object Create(ActorInitializer init)
		{
			if (Environment.GetEnvironmentVariable("YMCA_PLATEAU_ACCESS_PROBE") != "1") throw new InvalidOperationException("Diagnostic opt-in required.");
			return new PlateauAccessProbe(this);
		}
	}
	public sealed class PlateauAccessProbe : ITick, ITickRender
	{
		readonly PlateauAccessProbeInfo info;
		Actor carrier, cargo, ssm, ssmTarget;
		CPos ssmStart;
		int ssmOrderTick;
		bool ssmPassed;
		CPos landing, tankLanding, tankTarget, ground;
		bool carried, tankCarried, moving;
		int phase;
		public PlateauAccessProbe(PlateauAccessProbeInfo info) { this.info = info; }
		void ITick.Tick(Actor self)
		{
			var world = self.World;
			if (phase == 0 && world.WorldTick >= 10)
			{
				phase = 1;
				world.AddFrameEndTask(w => Start(w));
				return;
			}
			if (phase == 2)
			{
				carried |= !cargo.IsInWorld;
				if (carried && cargo.IsInWorld && cargo.Location == landing)
				{
					var transform = cargo.Trait<Transforms>();
					if (!transform.CanDeploy()) throw new InvalidOperationException("Delivered hilltop MCV cannot deploy.");
					transform.DeployTransform(false); phase = 3;
				}
			}
			if (phase == 3 && world.Actors.Any(a => a.Info.Name == "fact" && (a.Location - landing).LengthSquared < 9))
			{
				var power = world.Map.Rules.Actors["powr"]; var bi = power.TraitInfo<BuildingInfo>();
				var yard = world.Actors.First(a => a.Info.Name == "fact" && (a.Location - landing).LengthSquared < 9);
				var places = world.Map.FindTilesInCircle(landing, 8).Where(c => bi.Tiles(c).All(p => world.Map.Contains(p) && world.Map.Height[p] == 4) &&
					world.CanPlaceBuilding(c, power, bi, null) && bi.IsCloseEnoughToBase(world, world.LocalPlayer, power, yard, c)).ToArray();
				// Wait for the deployed yard's normal construction animation/base-area activation.
				if (places.Length > 0)
				{
					phase = 4;
					world.AddFrameEndTask(w => {
						w.CreateActor("powr", new TypeDictionary { new OwnerInit(w.LocalPlayer), new LocationInit(places[0]) });
						cargo = w.CreateActor("mtnk", new TypeDictionary { new OwnerInit(w.LocalPlayer), new LocationInit(ground) }); Deliver(w, tankLanding);
					});
				}
			}
			if (phase == 4)
			{
				tankCarried |= !cargo.IsInWorld;
				if (tankCarried && cargo.IsInWorld && cargo.Location == tankLanding && !moving)
				{ cargo.QueueActivity(false, cargo.Trait<Mobile>().MoveTo(tankTarget)); moving = true; }
				if (moving && cargo.Location == tankTarget)
				{
					phase = info.CheckSsm ? 6 : 5;
					if (info.CheckSsm) world.AddFrameEndTask(w => StartSsm(w));
				}
			}
			if (phase == 6 && ssm != null)
			{
				if (world.WorldTick == ssmOrderTick)
					foreach (var attack in ssm.TraitsImplementing<AttackBase>()) attack.AttackTarget(Target.FromActor(ssmTarget), AttackSource.Default, false, true);
				if (ssmTarget.Trait<Health>().HP < ssmTarget.Trait<Health>().MaxHP && ssm.Location != ssmStart)
				{ ssmPassed = true; phase = 5; }
			}
			if (phase == 5 && !File.Exists(info.ResultPath))
			{
					Directory.CreateDirectory(Path.GetDirectoryName(info.ResultPath));
					File.WriteAllText(info.ResultPath + ".json", JsonConvert.SerializeObject(new { Map = world.Map.Title, Passed = true,
						McvPickedUp = carried, McvDeployedOnPlateau = true, AdjacentBuildingFootprint = true, NormalBaseProximity = true, PowerPlantPlaced = true, SsmRequired = info.CheckSsm, SsmRepositionedAndDamagedTarget = ssmPassed, TankPickedUp = tankCarried,
						TankMovedOnPlateau = moving, Landing = landing.ToString(), TankLanding = tankLanding.ToString(), TankTarget = tankTarget.ToString(), Tick = world.WorldTick }, Formatting.Indented));
					File.WriteAllText(info.ResultPath, "PASS: Carryall MCV delivery and deployment, additional hilltop building footprint, tank delivery and actual roof movement\n");
			}
			if (phase != 5 && world.WorldTick > 4500) throw new InvalidOperationException($"Plateau access timed out phase={phase} cargo={cargo?.Location} carrier={carrier?.Location} carried={carried}/{tankCarried}");
		}
		void Start(World world)
		{
			var fact = world.Map.Rules.Actors["fact"]; var building = fact.TraitInfo<BuildingInfo>();
			var roofs = world.Map.AllCells.Where(c => world.Map.Contains(c) && world.Map.Height[c] == 4 && world.Map.Ramp[c] == 0 && world.Map.GetTerrainInfo(c).Type == "Clear").ToHashSet();
			var found = roofs.Where(c => building.Tiles(c).All(roofs.Contains) && world.CanPlaceBuilding(c, fact, building, null)).ToArray();
			if (found.Length == 0) throw new InvalidOperationException($"No hilltop construction-yard footprint. roofs={roofs.Count}, shapeFits={roofs.Count(c => building.Tiles(c).All(roofs.Contains))}, heights={string.Join(",", world.Map.AllCells.Where(world.Map.Contains).GroupBy(c => world.Map.Height[c]).Select(g => g.Key + ":" + g.Count()))}, roofTypes={string.Join(",", world.Map.AllCells.Where(c => world.Map.Contains(c) && world.Map.Height[c] == 4).GroupBy(c => world.Map.GetTerrainInfo(c).Type).Select(g => g.Key + ":" + g.Count()))}");
			var center = new CPos((int)roofs.Average(c => c.X), (int)roofs.Average(c => c.Y));
			landing = found.OrderBy(c => (c - center).LengthSquared).First() + new CVec(1, 1);
			var directions = new[] { new CVec(1, 0), new CVec(0, 1), new CVec(-1, 0), new CVec(0, -1) };
			var paths = roofs.Where(c => (c - landing).LengthSquared > 100).SelectMany(c => directions.Select(d => (Cell: c, Direction: d)))
				.Where(p => Enumerable.Range(0, 4).All(i => roofs.Contains(p.Cell + p.Direction * i))).ToArray();
			if (paths.Length == 0) throw new InvalidOperationException("No separate connected hilltop tank path.");
			var path = paths.OrderBy(p => (p.Cell - landing).LengthSquared).First();
			tankLanding = path.Cell; tankTarget = tankLanding + path.Direction * 3;
			ground = world.Map.AllCells.First(c => world.Map.Contains(c) && world.Map.Height[c] == 0 && world.Map.GetTerrainInfo(c).Type == "Clear" &&
				(c - landing).LengthSquared > 100 && (c - landing).LengthSquared < 900 && !world.ActorMap.GetActorsAt(c).Any());
			cargo = world.CreateActor("mcv", new TypeDictionary { new OwnerInit(world.LocalPlayer), new LocationInit(ground) });
			carrier = world.CreateActor("ocar", new TypeDictionary { new OwnerInit(world.LocalPlayer), new LocationInit(ground + new CVec(2, 0)) });
			Deliver(world, landing); phase = 2;
		}
		void StartSsm(World world)
		{
			var enemy = world.Players.First(p => p.InternalName == "Creeps");
			var seen = new System.Collections.Generic.HashSet<(CPos, CPos)>();
			var free = 0; var blocked = 0;
			foreach (var roof in world.Map.AllCells.Where(c => world.Map.Contains(c) && world.Map.Height[c] == 4))
			foreach (var direction in new[] { new CVec(1, 0), new CVec(0, 1), new CVec(1, 1), new CVec(1, -1) })
			foreach (var span in new[] { 4, 5, 6, 7, 8 })
			{
				var from = roof - direction * span; var to = roof + direction * span;
				if (!seen.Add((from, to)) || (to - from).LengthSquared > 16 * 16) continue;
				bool Free(CPos c) => world.Map.Contains(c) && world.Map.Height[c] == 0 && world.Map.GetTerrainInfo(c).Type == "Clear" && !world.ActorMap.GetActorsAt(c).Any();
				if (!Free(from) || !Free(to) || (from - landing).LengthSquared < 36 || (to - landing).LengthSquared < 36) continue;
				free++;
				var unit = world.CreateActor("ssm.multi", new TypeDictionary { new OwnerInit(world.LocalPlayer), new LocationInit(from) });
				unit.Trait<AutoTarget>().SetStance(unit, UnitStance.HoldFire);
				var target = world.CreateActor("mcv", new TypeDictionary { new OwnerInit(enemy), new LocationInit(to) });
				var t = Target.FromActor(target); var arms = unit.TraitsImplementing<Armament>().ToArray();
				if (!arms.Any(a => TerrainFireControl.CanFire(unit, a, t, unit.CenterPosition)))
				{
					blocked++;
					if (TerrainFireControl.FindFiringPosition(unit, arms, t) != null)
					{ ssm = unit; ssmTarget = target; ssmStart = from; ssmOrderTick = world.WorldTick + 30; return; }
				}
				unit.Dispose(); target.Dispose();
			}
			throw new InvalidOperationException($"No blocked but locally repositionable SSM case on native Nexus: free={free}, blocked={blocked}.");
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (phase >= 2) wr.Viewport.Center(self.World.Map.CenterOfCell(landing));
		}

		void Deliver(World world, CPos destination)
		{
			carrier.QueueActivity(false, new PickupUnit(carrier, cargo, 10, null));
			carrier.QueueActivity(true, new DeliverUnit(carrier, Target.FromCell(world, destination), WDist.Zero, null));
		}
	}
}
