using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("One actor-wide alternating barrel phase shared by weapon spin-up stages.")]
	public sealed class SharedBarrelSequenceInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new SharedBarrelSequence();
	}

	public sealed class SharedBarrelSequence : ISync
	{
		[Sync]
		public int NextBarrel { get; private set; }

		public int TakeNext()
		{
			var selected = NextBarrel;
			NextBarrel = 1 - NextBarrel;
			return selected;
		}
	}

	[Desc("Single-shot armament using the actor's shared two-barrel phase.",
		"Preserves alternation across Gatling stages, cooldowns and target changes.")]
	public sealed class SharedBarrelArmamentInfo : ArmamentInfo, Requires<SharedBarrelSequenceInfo>
	{
		public override object Create(ActorInitializer init) => new SharedBarrelArmament(init.Self, this);

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);
			if (LocalOffset.Length != 2 || WeaponInfo.Burst != 1 || FireDelay != 0)
				throw new YamlException("SharedBarrelArmament requires exactly two offsets, Burst1 and no delayed fire.");
		}
	}

	public sealed class SharedBarrelArmament : Armament
	{
		readonly SharedBarrelSequence sequence;

		public SharedBarrelArmament(Actor self, SharedBarrelArmamentInfo info)
			: base(self, info)
		{
			sequence = self.Trait<SharedBarrelSequence>();
		}

		protected override void FireBarrel(Actor self, IFacing facing, in Target target, Barrel barrel)
		{
			// The normal armament has already checked target/cadence/ammo here.
			// Send the same selected barrel to the projectile and muzzle renderer.
			base.FireBarrel(self, facing, target, Barrels[sequence.TakeNext()]);
		}
	}
}
