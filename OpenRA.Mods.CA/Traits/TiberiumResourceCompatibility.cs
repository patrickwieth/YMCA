using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class TiberiumResourceCompatibilityInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new TiberiumResourceCompatibility();
	}

	// Game loading must normalize before resources are admitted. The editor's
	// change listeners need initialized valuation/rendering first, so use its
	// ordinary cell-change events after WorldLoaded. Neither path saves a package.
	public class TiberiumResourceCompatibility : INotifyCreated, IPostWorldLoaded
	{
		void INotifyCreated.Created(Actor self)
		{
			if (self.World.Type != WorldType.Editor) Normalize(self.World.Map);
		}

		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type == WorldType.Editor) Normalize(world.Map);
		}

		static void Normalize(Map map)
		{
			foreach (var cell in map.AllCells)
			{
				var resource = map.Resources[cell];
				var type = resource.Type == 1 ? (byte)3 : resource.Type == 2 ? (byte)4 : resource.Type;
				if (type != resource.Type) map.Resources[cell] = new ResourceTile(type, resource.Index);
			}
		}
	}
}
