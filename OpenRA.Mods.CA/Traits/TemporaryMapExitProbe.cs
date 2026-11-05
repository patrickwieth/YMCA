using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// Opt-in diagnostic fixture only: register its own package, then exit through
	// the ordinary application shutdown path rather than killing the process.
	public class TemporaryMapExitProbeInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new TemporaryMapExitProbe();
	}

	public class TemporaryMapExitProbe : IPostWorldLoaded, ITick
	{
		int ticks;
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr) => TemporaryGeneratedMaps.Register(world.Map.Package.Name);
		void ITick.Tick(Actor self)
		{
			if (++ticks == 20) Game.Exit();
		}
	}
}
