using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Warheads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-gene-splicer", "Check resolved mutation warhead ordering, preserved weapon values and the zombie spawn chain.")]
	public sealed class CheckGeneSplicerCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-gene-splicer";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			CheckSourceTracking();
			Game.ModData = utility.ModData;
			var rules = utility.ModData.DefaultRules;
			foreach (var name in new[] { "genesplicebeam", "genesplicebeam1", "genesplicebeam2", "genesplicebeam3", "genesplicebeam4", "zombieattack" })
			{
				var weapon = rules.Weapons[name];
				var marker = weapon.Warheads.OfType<GeneSpliceWarhead>().Single(w => w.Condition == "genespliced");
				var markerIndex = weapon.Warheads.IndexOf(marker);
				var damage = weapon.Warheads.OfType<DamageWarhead>().ToArray();
				if (marker.Delay != 0 || damage.Any(w => weapon.Warheads.IndexOf(w) < markerIndex))
					throw new InvalidOperationException(name + ": mutation must precede all damage, including on lethal first hits.");

				var zombie = name == "zombieattack";
				if (marker.Range.Length != 256 || marker.Duration != (zombie ? 10 : 100) || !marker.ValidTargets.Contains("Infantry") ||
					weapon.ReloadDelay != 60 || weapon.Range.Length != (zombie ? 1536 : 9728) ||
					weapon.Warheads.OfType<SpreadDamageWarhead>().Single().Damage != (zombie ? 10000 : 2000))
					throw new InvalidOperationException(name + ": existing mutation/weapon values changed.");
				if (!zombie && (weapon.Burst != 11 || weapon.Warheads.OfType<DriverPoisonDamageWarhead>().Single().Damage != 4000))
					throw new InvalidOperationException(name + ": burst or driver damage changed.");
				Console.WriteLine(name + ": mutation before damage, original values preserved.");
			}

			var victim = rules.Actors["conscript"].TraitInfo<SpawnActorOnDeathFromConditionSourceInfo>();
			if (victim.Actor != "zombie.mutating" || victim.SourceCondition != "genespliced" || victim.RequiresCondition?.Expression != "genespliced" || victim.Probability != 100)
				throw new InvalidOperationException("Victim mutation must remain conditional and use the mutation source owner.");
			if (rules.Actors["zombie.mutating"].HasTraitInfo<SpawnActorOnDeathFromConditionSourceInfo>())
				throw new InvalidOperationException("Mutation intermediary must not require another infection.");
			var mutation = rules.Actors["zombie.mutating"].TraitInfos<SpawnActorOnDeathInfo>().Single(t => t.Actor == "zombie");
			if (mutation.RequiresCondition != null || mutation.OwnerType != OpenRA.Mods.Common.Traits.OwnerType.Victim || mutation.Probability != 100 || mutation.SpawnAfterDefeat)
				throw new InvalidOperationException("Mutation actor must unconditionally produce the final zombie for its existing owner.");
			if (!rules.Actors["zombie"].HasTraitInfo<MobileInfo>() || !rules.Actors["zombie"].TraitInfos<ArmamentInfo>().Any(a => a.Weapon == "ZombieAttack"))
				throw new InvalidOperationException("Final zombie is missing its movement/attack traits.");
			Console.WriteLine("Gene Splicer regression checks passed: marked victim -> mutation -> armed zombie, owned by the mutation source.");
		}

		static void CheckSourceTracking()
		{
			var sources = new ConditionSourceTracker<string>();
			var valid = new HashSet<int> { 1, 2, 3 };
			if (sources.Latest(valid.Contains) != null)
				throw new InvalidOperationException("Unmarked victim must have no mutation owner.");
			sources.Record(1, "splicer-owner", valid.Contains);
			if (sources.Latest(valid.Contains) != "splicer-owner")
				throw new InvalidOperationException("Source player must be retained independently of the killer.");
			sources.Record(2, "zombie-owner", valid.Contains);
			if (sources.Latest(valid.Contains) != "zombie-owner")
				throw new InvalidOperationException("Latest active infection must determine ownership.");
			valid.Remove(2);
			if (sources.Latest(valid.Contains) != "splicer-owner")
				throw new InvalidOperationException("Expired/revoked infection must fall back to an older active infection.");
			valid.Clear();
			sources.Record(3, "invalid-source", valid.Contains);
			if (sources.Latest(valid.Contains) != null)
				throw new InvalidOperationException("Expired/revoked/failed marks must not retain an owner.");
			valid.Add(4);
			sources.Record(4, "fresh-source", valid.Contains);
			if (sources.Latest(valid.Contains) != "fresh-source")
				throw new InvalidOperationException("Reinfection must use the new source owner.");
			Console.WriteLine("Source tracking: latest active mark, expiry/revocation, invalid token and reinfection checks passed.");
		}
	}
}
