#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Render a spinning voxel.")]
	public class WithVoxelSpinningBodyInfo : ConditionalTraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>
	{
		public readonly string Sequence = "idle";

		[Desc("The rate of the voxel animation.")]
		public readonly int Ticks = 60;

		[Desc("The rotation vector.")]
		public readonly WVec Axis = new WVec(0, 0, 1024);

		[Desc("The offset vector.")]
		public readonly WVec Offset = new WVec(0, 0, 0);

		[Desc("Defines if the Voxel should have a shadow.")]
		public readonly bool ShowShadow = true;

		[Desc("Reset the frames to first frame when the trait is disabled.")]
		public readonly bool ResetFramesWhenDisabled = false;

		public override object Create(ActorInitializer init) { return new WithVoxelSpinningBody(init.Self, this); }

		public IEnumerable<ModelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			var voxel = init.World.ModelCache.GetModelSequence(image, Sequence);
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			yield return new ModelAnimation(voxel, () => Offset.Rotate(orientation()),
				() => body.QuantizeOrientation(orientation(), facings),
				() => false, () => 0, ShowShadow);
		}
	}

	public class WithVoxelSpinningBody : ConditionalTrait<WithVoxelSpinningBodyInfo>, ITick, IAutoMouseBounds
	{
		readonly WithVoxelSpinningBodyInfo info;
		readonly RenderVoxels rv;
		readonly ModelAnimation modelAnimation;
		int tick;

		public WithVoxelSpinningBody(Actor self, WithVoxelSpinningBodyInfo info)
			: base(info)
		{
			this.info = info;

			var body = self.Trait<BodyOrientation>();
			rv = self.Trait<RenderVoxels>();

			var voxel = self.World.ModelCache.GetModelSequence(rv.Image, info.Sequence);

			modelAnimation = new ModelAnimation(voxel, () => info.Offset.Rotate(self.Orientation),
				() => {
					WRot rotation = new WRot(info.Axis, new WAngle(1024 * tick / info.Ticks));
					return body.QuantizeOrientation(self.Orientation.Rotate(rotation));
				},
				() => IsTraitDisabled, () => 0, info.ShowShadow);

			rv.Add(modelAnimation);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			tick++;

			if (tick < info.Ticks)
				return;

			tick = 0;
		}

		Rectangle IAutoMouseBounds.AutoMouseoverBounds(Actor self, WorldRenderer wr)
		{
			return modelAnimation.ScreenBounds(self.CenterPosition, wr, rv.Info.Scale);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (Info.ResetFramesWhenDisabled)
			{
				tick = 0;
			}
		}
	}
}
