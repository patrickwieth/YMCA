using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	// Added only by the height-combat diagnostic map. Does not alter damage or flight.
	public sealed class HeightImpactTraceWarhead : Warhead
	{
		public override void DoImpact(in Target target, WarheadArgs args)
		{
			// A projectile may outlive its shooter during the source-removal check.
			if (args.SourceActor != null && !args.SourceActor.Disposed)
				args.SourceActor.TraitOrDefault<HeightCombatProbe>()?.RecordImpact(args.SourceActor, args.ImpactPosition);
		}
	}
}
