using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Projectiles;
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class MissileHeightProbeInfo : TraitInfo
	{
		public readonly bool Water;
		public override object Create(ActorInitializer init) => new MissileHeightProbe(Water);
	}
	public sealed class MissileHeightProbe : ITick
	{
		sealed class Shot
		{
			public string Weapon;
			public int LaunchAltitude, TargetAltitude, Range, Ground;
			public IProjectile Projectile;
			public WPos Source, Target, Last;
			public FieldInfo Position;
			public bool Finished;
			public int Ticks;
		}
		readonly List<Shot> shots = new();
		bool done;
		readonly bool water;
		public MissileHeightProbe(bool water)
		{
			if (Environment.GetEnvironmentVariable("YMCA_MISSILE_HEIGHT_PROBE") != "1") throw new InvalidOperationException("Diagnostic opt-in required.");
			this.water = water;
		}
		void ITick.Tick(Actor self)
		{
			if (done) return;
			var world = self.World;
			if (world.WorldTick == 5)
			{
				var origin = world.Map.CenterOfCell(new CPos(112, 0));
				foreach (var pair in world.Map.Rules.Weapons.OrderBy(p => p.Key))
				{
					var weapon = pair.Value;
					if (weapon.Projectile is not (MissileCAInfo or MissileInfo) || weapon.Range.Length <= 0) continue;
					var type = weapon.Projectile.GetType();
					var bound = (string)type.GetField("BoundToTerrainType").GetValue(weapon.Projectile);
					if (water ? bound != "Water" : !string.IsNullOrEmpty(bound)) continue;
					foreach (var launchAltitude in water ? new[] { 0 } : new[] { 200, 2560 })
					foreach (var targetAltitude in water ? new[] { 0 } : weapon.ValidTargets.Contains("Air") || weapon.ValidTargets.Contains("AirSmall")
						? weapon.ValidTargets.Contains("Ground") ? new[] { 0, 2560 } : new[] { 2560 }
						: new[] { 0 })
					{
						// Remove random miss offsets only in this diagnostic copy. Production
						// range, speed, pitch, steering, fuel and terrain policy stay intact.
						var info = (IProjectileInfo)Activator.CreateInstance(type);
						foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) field.SetValue(info, field.GetValue(weapon.Projectile));
						type.GetField("Inaccuracy").SetValue(info, WDist.Zero);
						type.GetField("LockOnInaccuracy").SetValue(info, WDist.Zero);
						type.GetField("LockOnProbability").SetValue(info, 100);
						var distance = Math.Max(weapon.MinRange.Length + 512, Math.Min(24 * 1024, weapon.Range.Length - 512));
						var source = origin + new WVec(0, 0, launchAltitude);
						var target = origin + new WVec(distance, 0, targetAltitude);
						var args = new ProjectileArgs { Weapon = weapon, SourceActor = world.LocalPlayer.PlayerActor,
							Source = source, CurrentSource = () => source, PassiveTarget = target, GuidedTarget = Target.FromPos(target),
							Facing = (target - source).Yaw, DamageModifiers = Array.Empty<int>(), RangeModifiers = Array.Empty<int>(), InaccuracyModifiers = Array.Empty<int>() };
						var projectile = info.Create(args);
						shots.Add(new Shot { Weapon = pair.Key, LaunchAltitude = launchAltitude, TargetAltitude = targetAltitude, Range = distance,
							Ground = origin.Z, Source = source, Target = target, Last = source, Projectile = projectile,
							Position = projectile.GetType().GetField("pos", BindingFlags.NonPublic | BindingFlags.Instance) });
						world.AddFrameEndTask(w => w.Add(projectile));
					}
				}
			}
			if (world.WorldTick <= 5 || shots.Count == 0) return;
			foreach (var shot in shots.Where(s => !s.Finished))
			{
				shot.Last = (WPos)shot.Position.GetValue(shot.Projectile);
				shot.Ticks = world.WorldTick - 5;
				shot.Finished = !world.Effects.Contains(shot.Projectile);
			}
			if (world.WorldTick >= 405 || shots.All(s => s.Finished))
			{
				done = true;
				File.WriteAllText(Path.Combine(Platform.SupportDir, "missile-height.json"), JsonConvert.SerializeObject(new { Passed = true,
					Shots = shots.Select(s => new { s.Weapon, s.LaunchAltitude, s.TargetAltitude, s.Ground, s.Range, s.Finished, s.Ticks,
						MissDistance = (s.Target - s.Last).Length, Travel = (s.Last - s.Source).HorizontalLength,
						Altitude = s.Last.Z - s.Ground, RelativePosition = (s.Last - s.Source).ToString() }) }, Formatting.Indented));
			}
		}
	}
}
