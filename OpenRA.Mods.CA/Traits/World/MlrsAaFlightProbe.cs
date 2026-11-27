using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Projectiles;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class MlrsAaFlightProbeInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new MlrsAaFlightProbe();
	}
	public sealed class MlrsAaFlightProbe : ITick
	{
		sealed class Shot
		{
			public string Weapon;
			public int Ground;
			public WPos Target;
			public MissileCA Missile;
			public List<object> Trace = new();
			public bool Finished;
			public int MissDistance;
		}
		readonly List<Shot> shots = new();
		readonly List<object> combat = new();
		WPos raisedOrigin;
		Actor launcher, aircraft;
		Health targetHealth;
		int initialHealth;
		object[] flightResults;
		bool flightsPassed, combatPassed = true;
		static readonly FieldInfo Position = typeof(MissileCA).GetField("pos", BindingFlags.NonPublic | BindingFlags.Instance);
		public MlrsAaFlightProbe()
		{
			if (Environment.GetEnvironmentVariable("YMCA_MLRS_AA_PROBE") != "1") throw new InvalidOperationException("Diagnostic opt-in required.");
		}
		void ITick.Tick(Actor self)
		{
			var world = self.World;
			if (world.WorldTick == 5)
			{
				foreach (var raised in new[] { false, true })
				{
					var center = world.Map.AllCells.Select(c => world.Map.CenterOfCell(c)).First(p =>
						(raised ? p.Z >= 2800 : p.Z == 0) && Enumerable.Range(-3, 7).All(i =>
							world.Map.Contains(world.Map.CellContaining(p + new WVec(i * 1024, 0, 0))) &&
							world.Map.CenterOfCell(world.Map.CellContaining(p + new WVec(i * 1024, 0, 0))).Z == p.Z) &&
						Enumerable.Range(-2, 17).All(x => Enumerable.Range(-2, 5).All(y =>
						{
							var c = world.Map.CellContaining(p + new WVec(x * 1024, y * 1024, 0));
							return world.Map.Contains(c) && (raised || world.Map.CenterOfCell(c).Z == 0);
						})));
					if (raised) raisedOrigin = center;
					foreach (var weaponName in new[] { "227mmaa", "227mmaa.dedicated", "227mmaa.upg", "227mmaah" })
					{
						var weapon = world.Map.Rules.Weapons[weaponName];
						var source = center + new WVec(0, 0, 200);
						var target = center + new WVec(weaponName.EndsWith("dedicated", StringComparison.Ordinal) ? 12 * 1024 : 10 * 1024, 0, 2560);
						var args = new ProjectileArgs { Weapon = weapon, SourceActor = world.LocalPlayer.PlayerActor,
							Source = source, CurrentSource = () => source, PassiveTarget = target, GuidedTarget = Target.FromPos(target),
							Facing = (target - source).Yaw + new WAngle(100), DamageModifiers = Array.Empty<int>(),
							RangeModifiers = Array.Empty<int>(), InaccuracyModifiers = Array.Empty<int>() };
						var missile = (MissileCA)weapon.Projectile.Create(args);
						shots.Add(new Shot { Weapon = weaponName, Ground = center.Z, Target = target, Missile = missile });
						world.AddFrameEndTask(w => w.Add(missile));
					}
				}
			}
			if (world.WorldTick == 210 || world.WorldTick == 410)
			{
				launcher = world.CreateActor("mlrs.aa", new TypeDictionary { new OwnerInit(world.LocalPlayer), new LocationInit(world.Map.CellContaining(raisedOrigin)) });
				var target = raisedOrigin + new WVec(12 * 1024, 0, 2560);
				aircraft = world.CreateActor("orca", new TypeDictionary { new OwnerInit(world.Players.First(p => p.InternalName == "Creeps")),
					new LocationInit(world.Map.CellContaining(target)), new CenterPositionInit(target) });
				aircraft.TraitOrDefault<AutoTarget>()?.SetStance(aircraft, UnitStance.HoldFire);
				targetHealth = aircraft.Trait<Health>(); initialHealth = targetHealth.HP;
				foreach (var attack in launcher.TraitsImplementing<AttackBase>())
					attack.AttackTarget(Target.FromActor(aircraft), AttackSource.Default, false, true);
			}
			if (aircraft != null && !aircraft.IsDead && aircraft.IsInWorld)
			{
				var shift = world.WorldTick < 410 ? 0 : ((world.WorldTick - 410) % 60 < 30 ? (world.WorldTick - 410) % 30 : 30 - (world.WorldTick - 410) % 30) * 96 - 1440;
				aircraft.Trait<Aircraft>().SetPosition(aircraft, raisedOrigin + new WVec(12 * 1024, shift, 2560));
			}
			if (world.WorldTick == 390 || world.WorldTick == 590)
			{
				combat.Add(new { Moving = world.WorldTick == 590, Damage = initialHealth - targetHealth.HP });
				combatPassed &= initialHealth > targetHealth.HP;
				var oldLauncher = launcher; var oldAircraft = aircraft;
				world.AddFrameEndTask(w => { if (oldLauncher.IsInWorld) oldLauncher.Dispose(); if (oldAircraft.IsInWorld) oldAircraft.Dispose(); });
				launcher = aircraft = null;
			}
			if (world.WorldTick == 600)
				File.WriteAllText(Path.Combine(Platform.SupportDir, "mlrs-aa-flight.json"), JsonConvert.SerializeObject(new { Passed = flightsPassed && combatPassed && combat.Count == 2, Flights = flightResults, Combat = combat }, Formatting.Indented));
			if (world.WorldTick <= 5 || shots.Count == 0) return;
			foreach (var shot in shots.Where(s => !s.Finished))
			{
				var p = (WPos)Position.GetValue(shot.Missile);
				shot.MissDistance = (shot.Target - p).Length;
				shot.Trace.Add(new { Tick = world.WorldTick - 5, Position = p.ToString(), AboveTerrain = world.Map.DistanceAboveTerrain(p).Length, Distance = shot.MissDistance });
				shot.Finished = !world.Effects.Contains(shot.Missile);
			}
			if (shots.All(s => s.Finished) || world.WorldTick == 200)
			{
				flightsPassed = shots.All(s => s.Finished && s.MissDistance < 1024);
				flightResults = shots.Select(s => (object)new { s.Weapon, s.Ground, s.Finished, s.MissDistance, s.Trace }).ToArray();
				shots.Clear();
			}
		}
	}
}
