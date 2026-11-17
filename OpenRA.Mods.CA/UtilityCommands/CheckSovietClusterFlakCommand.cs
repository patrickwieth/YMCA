using System;
using System.IO;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Mods.CA.Projectiles;
using OpenRA.Mods.CA.Warheads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckSovietClusterFlakCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-soviet-cluster-flak";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Check cluster dispatch, preserved air altitude and the Allied-relative damage budget.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var alliedArm = mod.DefaultRules.Actors["agun"].TraitInfos<ArmamentInfo>().Single();
			var allied = mod.DefaultRules.Weapons[alliedArm.Weapon.ToLowerInvariant()];
			var aa = mod.DefaultRules.Weapons["soviet-cluster-aa"];
			var cluster = aa.Warheads.OfType<AirburstClusterWarhead>().Single();
			var payload = mod.DefaultRules.Weapons[cluster.Weapon.ToLowerInvariant()];
			var damage = payload.Warheads.OfType<SpreadDamageWarhead>().Single();
			var alliedDamage = allied.Warheads.OfType<SpreadDamageWarhead>().Single();
			if (allied.Burst != 1 || aa.Burst != 1 || aa.ReloadDelay != 4 * allied.ReloadDelay || cluster.Offsets.Length != 4 || aa.Warheads.OfType<DamageWarhead>().Any())
				throw new InvalidDataException("Unexpected cadence/cluster count/extra parent damage.");
			if (damage.Damage * 4 * allied.ReloadDelay != alliedDamage.Damage * aa.ReloadDelay || !damage.ValidTargets.Equals(alliedDamage.ValidTargets) || !aa.ValidTargets.Equals(allied.ValidTargets))
				throw new InvalidDataException("AA damage budget or target filter differs.");
			if (damage.Versus.Count != alliedDamage.Versus.Count || damage.Versus.Any(kv => !alliedDamage.Versus.TryGetValue(kv.Key, out var value) || value != kv.Value))
				throw new InvalidDataException("Changed armor multipliers.");
			var bullet = (BulletCAInfo)aa.Projectile;
			if (cluster.Offsets.Any(o => o.Length + bullet.Inaccuracy.Length >= damage.Range[1].Length) || damage.Falloff[0] != 100 || damage.Falloff[1] != 100)
				throw new InvalidDataException("Central full-damage overlap lost.");
			foreach (var altitude in new[] { -500, 0, 1500, 10000 })
			{
				var position = new WPos(512, -1024, altitude);
				var original = new WarheadArgs { Weapon = aa, ImpactPosition = position, ImpactOrientation = WRot.FromYaw(new WAngle(192)), Source = new WPos(0, 0, 256), WeaponTarget = Target.FromPos(new WPos(9999, 9999, 9999)), DamageModifiers = new[] { 125, 80 } };
				var count = 0;
				cluster.ForEachImpact(original, (target, child) =>
				{
					if (child.ImpactPosition != position + cluster.Offsets[count] || child.ImpactPosition.Z != altitude || target.CenterPosition != child.ImpactPosition || child.Weapon != payload || child.Source != original.Source || child.SourceActor != original.SourceActor || child.ImpactOrientation != original.ImpactOrientation || !child.DamageModifiers.SequenceEqual(original.DamageModifiers) || child.WeaponTarget.CenterPosition != original.WeaponTarget.CenterPosition)
						throw new InvalidDataException("Cluster altitude, position, source or modifiers lost.");
					count++;
				});
				if (count != 4 || cluster.ImpactPositions(position).Distinct().Count() != 4 || original.ImpactPosition != position || original.Weapon != aa)
					throw new InvalidDataException("Cluster count/parent mutation.");
			}
			Console.WriteLine($"Cluster flak: {allied.ReloadDelay}->{aa.ReloadDelay} ticks, {alliedDamage.Damage} vs 4x{damage.Damage}, equal nominal DPS; four altitude-preserving dispatches at four tested heights, no parent damage; armor/AA filters retained. Live firing pending.");
		}
	}
}
