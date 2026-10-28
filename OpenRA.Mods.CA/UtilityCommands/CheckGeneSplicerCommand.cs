using System;
using System.Linq;
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
			Game.ModData = utility.ModData;
			var rules = utility.ModData.DefaultRules;
			foreach (var name in new[] { "genesplicebeam", "genesplicebeam1", "genesplicebeam2", "genesplicebeam3", "genesplicebeam4", "zombieattack" })
			{
				var weapon = rules.Weapons[name];
				var marker = weapon.Warheads.OfType<GrantExternalConditionWarhead>().Single(w => w.Condition == "genespliced");
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

			var victim = rules.Actors["conscript"].TraitInfos<SpawnActorOnDeathInfo>().Single(t => t.Actor == "zombie.mutating");
			if (victim.RequiresCondition?.Expression != "genespliced" || victim.OwnerType != OwnerType.Killer || victim.Probability != 100)
				throw new InvalidOperationException("Victim mutation must remain conditional and preserve killer ownership/probability.");
			var mutation = rules.Actors["zombie.mutating"].TraitInfos<SpawnActorOnDeathInfo>().Single(t => t.Actor == "zombie");
			if (mutation.RequiresCondition != null || mutation.OwnerType != OwnerType.Victim || mutation.Probability != 100 || mutation.SpawnAfterDefeat)
				throw new InvalidOperationException("Mutation actor must unconditionally produce the final zombie for its existing owner.");
			if (!rules.Actors["zombie"].HasTraitInfo<MobileInfo>() || !rules.Actors["zombie"].TraitInfos<ArmamentInfo>().Any(a => a.Weapon == "ZombieAttack"))
				throw new InvalidOperationException("Final zombie is missing its movement/attack traits.");
			Console.WriteLine("Gene Splicer regression checks passed: marked victim -> mutation -> armed zombie, with ownership preserved.");
		}
	}
}
