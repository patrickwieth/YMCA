using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Detonates child weapons around the actual impact position, preserving its world altitude.")]
	public sealed class AirburstClusterWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference, FieldLoader.Require]
		public readonly string Weapon = null;

		[FieldLoader.Require]
		public readonly WVec[] Offsets = Array.Empty<WVec>();

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (Offsets.Length == 0 || Offsets.Any(o => o.Z != 0))
				throw new YamlException("AirburstCluster requires nonempty, horizontal offsets.");
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException($"Unknown airburst weapon '{Weapon}'.");
			if (weapon.Warheads.Any(w => w is AirburstClusterWarhead))
				throw new YamlException("Airburst payload must not recursively contain clusters.");
		}

		public IEnumerable<WPos> ImpactPositions(WPos impact)
		{
			foreach (var offset in Offsets)
				yield return impact + offset;
		}

		// Same dispatch path used by DoImpact and the headless regression command.
		internal void ForEachImpact(WarheadArgs args, Action<Target, WarheadArgs> apply)
		{
			foreach (var position in ImpactPositions(args.ImpactPosition))
			{
				var childArgs = new WarheadArgs(args)
				{
					Weapon = weapon,
					ImpactPosition = position,
					ImpactOrientation = args.ImpactOrientation
				};
				apply(Target.FromPos(position), childArgs);
			}
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (target.Type != TargetType.Invalid)
				ForEachImpact(args, (childTarget, childArgs) => weapon.Impact(childTarget, childArgs));
		}
	}
}
