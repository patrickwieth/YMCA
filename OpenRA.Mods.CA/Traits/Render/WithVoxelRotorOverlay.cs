using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Rotor overlay registered to a fixed-pixel voxel model on a rectangular grid.")]
	public sealed class WithVoxelRotorOverlayInfo : WithIdleOverlayInfo, IRenderActorPreviewSpritesInfo
	{
		public override object Create(ActorInitializer init) => new WithVoxelRotorOverlay(init.Self, this);

		public static WVec ProjectOffset(BodyOrientationInfo body, WVec offset, WRot orientation, MapGrid grid)
		{
			var v = body.LocalToWorld(offset.Rotate(orientation));
			// Voxel camera projection is in pixels, independent of the map's tile aspect ratio.
			return new WVec(v.X * grid.TileSize.Height / grid.TileSize.Width, v.Y, v.Z);
		}

		public new IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault) yield break;
			var dynamicFacing = init.GetOrDefault<DynamicFacingInit>();
			var fixedFacing = init.GetValue<FacingInit, WAngle>(WAngle.Zero);
			Func<WAngle> facing = dynamicFacing == null ? () => fixedFacing : dynamicFacing.Value;
			var anim = new Animation(init.World, Image ?? image, facing);
			anim.PlayRepeating(Sequence);
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			WVec Position() => ProjectOffset(body, Offset, body.QuantizeOrientation(WRot.FromYaw(facing()), facings), init.World.Map.Grid);
			yield return new SpriteActorPreview(anim, Position, () => Position().Y + Position().Z + 1, p);
		}
	}

	public sealed class WithVoxelRotorOverlay : PausableConditionalTrait<WithVoxelRotorOverlayInfo>
	{
		public WithVoxelRotorOverlay(Actor self, WithVoxelRotorOverlayInfo info) : base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();
			var bodyInfo = self.Info.TraitInfo<BodyOrientationInfo>();
			var facing = self.Trait<IFacing>();
			var overlay = new Animation(self.World, info.Image ?? rs.GetImage(self),
				() => body.QuantizeFacing(facing.Facing), () => IsTraitPaused);
			overlay.PlayRepeating(info.Sequence);
			rs.Add(new AnimationWithOffset(overlay,
				() => WithVoxelRotorOverlayInfo.ProjectOffset(bodyInfo, info.Offset, body.QuantizeOrientation(self.Orientation), self.World.Map.Grid),
				() => IsTraitDisabled, p => RenderUtils.ZOffsetFromCenter(self, p, 1)), info.Palette, info.IsPlayerPalette);
		}
	}
}
