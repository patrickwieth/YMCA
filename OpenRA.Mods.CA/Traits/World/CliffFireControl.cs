using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Projectiles;
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Shared logical terrain collision and stance-respecting firing-position policy.")]
	public sealed class CliffFireControlInfo : TraitInfo
	{
		// Half one native height step. Calibration is covered by the isolated probe.
		public readonly WDist EdgeTolerance = new(362);
		public readonly int SearchRadius = 12;
		public override object Create(ActorInitializer init) => new CliffFireControl(init.Self.World, this);
	}
	public sealed class CliffFireControl : ITerrainFireControl
	{
		readonly World world;
		readonly CliffFireControlInfo info;
		static readonly CVec[] EdgeDirections = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
		sealed class Retry { public int After; }
		readonly ConditionalWeakTable<Actor, Retry> retries = new();
		public CliffFireControl(World world, CliffFireControlInfo info) { this.world = world; this.info = info; }

		static bool Physical(WeaponInfo weapon) => weapon.Range > WDist.Zero && weapon.Warheads.OfType<DamageWarhead>().Any(w => w.Damage > 0);

		public bool Handles(WeaponInfo weapon) => Physical(weapon);

		public bool TryBlock(WeaponInfo weapon, WPos from, WPos to, out WPos hit)
		{
			hit = to;
			return Physical(weapon) && Trace(world.Map, from, to, info.EdgeTolerance.Length, out hit);
		}

		// Symmetric sampling of the entire segment also prevents fast-projectile tunneling.
		internal static bool Trace(Map map, WPos from, WPos to, int tolerance, out WPos hit)
		{
			hit = to;
			var steps = Math.Max(1, ((to - from).Length + 63) / 64);
			for (var i = 0; i <= steps; i++)
			{
				var p = WPos.Lerp(from, to, i, steps);
				if (!map.Contains(map.CellContaining(p))) continue;
				var above = map.DistanceAboveTerrain(p).Length;
				var terrain = p.Z - above;
				var allowance = terrain > 0 ? tolerance : 0;
				if (above >= -allowance) continue;
				if (terrain > 0 && above >= -allowance - UpperLipAllowance(map, p)) continue;
				hit = p;
				return true;
			}
			return false;
		}

		// A narrow bevel in combat geometry only: up to one height step at the
		// rim, tapering to zero half a cell inland. Ramps are already continuous.
		static int UpperLipAllowance(Map map, WPos position)
		{
			var cell = map.CellContaining(position);
			if (map.Ramp[cell] != 0) return 0;
			var center = map.CenterOfCell(cell);
			var cut = 0;
			foreach (var direction in EdgeDirections)
			{
				var neighbor = cell + direction;
				if (!map.Contains(neighbor) || map.Ramp[neighbor] != 0) continue;
				var drop = center.Z - map.CenterOfCell(neighbor).Z;
				if (drop <= 0) continue;
				var delta = map.Offset(direction, 0);
				var square = (long)delta.X * delta.X + (long)delta.Y * delta.Y;
				var along = (long)(position.X - center.X) * delta.X + (long)(position.Y - center.Y) * delta.Y;
				var distance = (int)Math.Max(0, (square / 2 - along) * 1024 / square);
				if (distance >= 512) continue;
				cut = Math.Max(cut, Math.Min(drop, map.Offset(CVec.Zero, 1).Z) * (512 - distance) / 512);
			}
			return cut;
		}

		public bool CanFire(Actor self, Armament arm, Target target, WPos position)
		{
			if (target.Type != TargetType.Actor || !target.Actor.CanBeViewedByPlayer(self.Owner) || !Physical(arm.Weapon)) return true;
			var projectile = arm.Weapon.Projectile;
			if (projectile == null) return true;
			// Explicit terrain-following or steep-launch missiles keep their own flight
			// planning. Ordinary low-launch missiles use a conservative direct corridor.
			int Signed(WAngle a) => a.Angle > 512 ? a.Angle - 1024 : a.Angle;
			if (projectile is MissileInfo missile && (missile.TerrainHeightAware || Signed(missile.MinimumLaunchAngle) >= 128)) return true;
			if (projectile is MissileCAInfo missileCA && (missileCA.TerrainHeightAware || Signed(missileCA.MinimumLaunchAngle) >= 128)) return true;
			var name = projectile.GetType().Name;
			if (projectile is not BulletInfo && projectile is not BulletCAInfo && projectile is not MissileInfo && projectile is not MissileCAInfo &&
				name != "LaserZapInfo" && name != "LaserZapCAInfo" && name != "PlasmaBeamInfo" &&
				name != "RailgunInfo" && name != "AreaBeamInfo" && name != "InstantHitInfo" &&
				name != "TeslaZapInfo" && name != "ElectricBoltInfo" && name != "LinearPulseInfo" && name != "RadBeamInfo") return true;
			var angle = projectile is BulletInfo b ? b.LaunchAngle.Min() : projectile is BulletCAInfo ca ? ca.LaunchAngle.Min() : WAngle.Zero;
			// Do not let a long gun barrel grant one-way fire to a hull hidden behind
			// the crest. Unit-vs-unit direct fire also needs a reciprocal body corridor.
			if (angle == WAngle.Zero && self.Info.HasTraitInfo<MobileInfo>() && target.Actor.Info.HasTraitInfo<MobileInfo>() &&
				Trace(world.Map, position, target.CenterPosition, info.EdgeTolerance.Length, out _)) return false;
			foreach (var barrel in arm.Barrels)
			{
				var source = position + arm.MuzzleOffset(self, barrel);
				var followsGround = projectile is LinearPulseInfo pulse && pulse.ForceGround;
				if (followsGround) source -= new WVec(0, 0, world.Map.DistanceAboveTerrain(source).Length);
				var end = arm.Weapon.TargetActorCenter ? target.CenterPosition : target.Positions.ClosestToIgnoringPath(source);
				var steps = Math.Max(1, ((end - source).Length + 127) / 128);
				var previous = source;
				var blocked = false;
				for (var i = 1; i <= steps; i++)
				{
					var next = WPos.LerpQuadratic(source, end, angle, i, steps);
					if (followsGround && world.Map.Contains(world.Map.CellContaining(next)))
						next -= new WVec(0, 0, world.Map.DistanceAboveTerrain(next).Length);
					if (Trace(world.Map, previous, next, info.EdgeTolerance.Length, out _)) { blocked = true; break; }
					previous = next;
				}
				if (!blocked) return true;
			}
			return false;
		}

		public Activity FindFiringPosition(Actor self, IEnumerable<Armament> armaments, Target target)
		{
			var mobile = self.TraitOrDefault<Mobile>();
			if (mobile == null || mobile.IsTraitDisabled || mobile.IsTraitPaused || target.Type != TargetType.Actor || !target.Actor.CanBeViewedByPlayer(self.Owner)) return null;
			var retry = retries.GetOrCreateValue(self);
			if (world.WorldTick < retry.After) return null;
			retry.After = world.WorldTick + 25;
			var arms = armaments.Where(a => !a.IsTraitDisabled && !a.IsTraitPaused && a.Weapon.IsValidAgainst(target, world, self)).ToArray();
			var origin = self.Location;
			bool Accept(CPos cell)
			{
				var p = world.Map.CenterOfCell(cell);
				return cell != origin && arms.Any(a => target.IsInRange(p, a.MaxRange()) &&
					(a.Weapon.MinRange == WDist.Zero || !target.IsInRange(p, a.Weapon.MinRange)) && CanFire(self, a, target, p));
			}
			var path = mobile.PathFinder.FindPathToTargetCellByPredicate(self, new[] { origin }, Accept, BlockedByActor.Immovable,
				customCost: c => (c - origin).LengthSquared <= info.SearchRadius * info.SearchRadius ? 0 : int.MaxValue);
			return path.Count == 0 ? null : mobile.MoveTo(path[0]);
		}
	}
}
