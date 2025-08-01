﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Graphics
{
	public class WithMindControlArcCAInfo : TraitInfo
	{
		[Desc("Color of the arc")]
		public readonly Color Color = Color.Red;

		public readonly bool UsePlayerColor = false;

		public readonly int Transparency = 255;

		[Desc("Drawing from self.CenterPosition draws the curve from the foot. Add this much for better looks.")]
		public readonly WVec Offset = new WVec(0, 0, 0);

		[Desc("The angle of the arc of the beam.")]
		public readonly WAngle Angle = new WAngle(64);

		[Desc("Controls how fine-grained the resulting arc should be.")]
		public readonly int QuantizedSegments = 16;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("The width of the zap.")]
		public readonly WDist Width = new WDist(43);

		public override object Create(ActorInitializer init) { return new WithMindControlArcCA(init.Self, this); }
	}

	public class WithMindControlArcCA : IRenderAboveShroudWhenSelected, INotifySelected, INotifyCreated
	{
		readonly WithMindControlArcCAInfo info;
		MindControllerCA mindController;
		MindControllableCA mindControllable;

		public WithMindControlArcCA(Actor self, WithMindControlArcCAInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			mindController = self.TraitOrDefault<MindControllerCA>();
			mindControllable = self.TraitOrDefault<MindControllableCA>();
		}

		void INotifySelected.Selected(Actor a) { }

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			var color = Color.FromArgb(info.Transparency, info.UsePlayerColor ? self.Owner.Color : info.Color);

			if (mindController != null)
			{
				foreach (var s in mindController.Slaves)
					yield return new ArcRenderable(
						self.CenterPosition + info.Offset,
						s.CenterPosition + info.Offset,
						info.ZOffset, info.Angle, color, info.Width, info.QuantizedSegments);
				yield break;
			}

			if (mindControllable == null || mindControllable.Master == null || !mindControllable.Master.IsInWorld)
				yield break;

			yield return new ArcRenderable(
				mindControllable.Master.CenterPosition + info.Offset,
				self.CenterPosition + info.Offset,
				info.ZOffset, info.Angle, color, info.Width, info.QuantizedSegments);
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return false; } }
	}
}
