using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Opt-in client regression for shared Soviet building art and actual production palette refresh.")]
	public class ProductionIconProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new ProductionIconProbe(this);
	}

	public class ProductionIconProbe : ITickRender
	{
		readonly ProductionIconProbeInfo info;
		int frames;
		public ProductionIconProbe(ProductionIconProbeInfo info) { this.info = info; }
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (++frames != 40 && frames != 80) return;
			var world = self.World;
			var helipad = frames == 40;
			var type = helipad ? "Building" : "Defense";
			var name = helipad ? "hpad.soviet" : "silo.soviet";
			var expected = helipad ? "hpad" : "silo";
			var actor = world.Map.Rules.Actors[name];
			var image = actor.TraitInfo<RenderSpritesInfo>().GetImage(actor, world.LocalPlayer.Faction.InternalName);
			if (image != expected) throw new InvalidOperationException("Soviet shared image mapping regressed: " + name);
			var animation = new Animation(world, image);
			foreach (var sequence in new[] { "icon", "idle", "damaged-idle", "make", helipad ? "active" : "stages" })
			{
				animation.Play(sequence);
				if (animation.Image == null) throw new InvalidOperationException("Missing resolved sprite: " + image + "/" + sequence);
			}
			var queue = world.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>().Single(q => q.Info.Type == type);
			Ui.Root.Get<ProductionPaletteWidget>("PRODUCTION_PALETTE").CurrentQueue = queue;
			PlateauMovementProbe.WriteResult(info.ResultPath, $"PRODUCTION ICON PASS {name}: shared sequences and real {type} palette refresh.");
			if (!helipad) Game.TakeScreenshot();
		}
	}
}
