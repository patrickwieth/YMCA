using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Adds player-colored masks to the configured foundation family without recoloring its concrete or building.")]
	public class WithBibRemapInfo : TraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>, IRenderActorPreviewSpritesInfo
	{
		public readonly string ImagePrefix = "sovietbib";
		[Desc("Also apply the mask to gap fillers. Disable for building-only corner accents.")]
		public readonly bool RemapConnections = true;
		[PaletteReference(true)]
		public readonly string Palette = "bibplayer";
		[Desc("Optional extra foundation prefixes and their player-palette prefixes for shared buildings.")]
		public readonly Dictionary<string, string> AdditionalPalettes = new();

		public string PaletteFor(string image)
		{
			if (image == null) return null;
			foreach (var pair in AdditionalPalettes)
				if (image.StartsWith(pair.Key, System.StringComparison.Ordinal)) return pair.Value;
			return image.StartsWith(ImagePrefix, System.StringComparison.Ordinal) ? Palette : null;
		}

		public override object Create(ActorInitializer init) => new WithBibRemap(this);

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			foreach (var bib in init.Actor.TraitInfos<WithIdleOverlayInfo>().Where(b => PaletteFor(b.Image) != null && (b is WithFactionBibOverlayInfo factionBib ? factionBib.MatchesPreview(init) : b.EnabledByDefault)))
			{
				var animation = new Animation(init.World, bib.Image) { IsDecoration = true };
				animation.PlayRepeating("remap");
				var body = init.Actor.TraitInfo<BodyOrientationInfo>();
				var facing = init.GetValue<FacingInit, WAngle>(WAngle.Zero);
				WVec Offset() => body.LocalToWorld(bib.Offset.Rotate(body.QuantizeOrientation(WRot.FromYaw(facing), facings)));
				int ZOffset() { var offset = Offset(); return offset.Y + offset.Z + 2; }
				yield return new SpriteActorPreview(animation, Offset, ZOffset,
					init.WorldRenderer.Palette(PaletteFor(bib.Image) + init.Get<OwnerInit>().InternalName));
			}
		}
	}

	public class WithBibRemap : INotifyCreated
	{
		public readonly WithBibRemapInfo Info;
		public WithBibRemap(WithBibRemapInfo info) { Info = info; }
		public bool Matches(string image) => Info.PaletteFor(image) != null;

		void INotifyCreated.Created(Actor self)
		{
			var sprites = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();
			foreach (var bib in self.TraitsImplementing<WithIdleOverlay>().Where(b => Matches(b.Info.Image)))
			{
				var animation = new Animation(self.World, bib.Info.Image) { IsDecoration = true };
				animation.PlayRepeating("remap");
				sprites.Add(new AnimationWithOffset(animation,
					() => body.LocalToWorld(bib.Info.Offset.Rotate(body.QuantizeOrientation(self.Orientation))),
					() => bib.IsTraitDisabled,
					p => RenderUtils.ZOffsetFromCenter(self, p, 2)), Info.PaletteFor(bib.Info.Image), true);
			}
		}
	}
}
