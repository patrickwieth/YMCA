#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Allows custom formation orders to slow actors that reach their destination too early.")]
	public class CustomFormationSpeedControllerInfo : TraitInfo
	{
		[Desc("Minimum percent movement speed to allow for actors that are much closer to their assigned waypoint.")]
		public readonly int MinSpeedPercent = 55;

		[Desc("Ignore slowdown if the distance difference to the furthest actor is below this threshold (in WDist).")]
		public readonly int IgnoreDistanceThreshold = 256;

		[Desc("Base number of ticks to keep the slowdown active.")]
		public readonly int BaseDurationTicks = 15;

		[Desc("Divisor used to convert additional distance (in WDist) into slowdown ticks. Larger values result in shorter slowdowns.")]
		public readonly int DistanceToDurationDivisor = 256;

		[Desc("Maximum number of ticks to keep a slowdown active. Set to 0 to disable clamping.")]
		public readonly int MaxDurationTicks = 240;

		public override object Create(ActorInitializer init) { return new CustomFormationSpeedController(this); }
	}

	public class CustomFormationSpeedController : ITick, ISpeedModifier
	{
		readonly CustomFormationSpeedControllerInfo info;
		int currentModifier = 100;
		int ticksRemaining;

		public CustomFormationSpeedController(CustomFormationSpeedControllerInfo info)
		{
			this.info = info;
		}

		int ISpeedModifier.GetSpeedModifier() => currentModifier;

		void ITick.Tick(Actor self)
		{
			if (ticksRemaining <= 0)
				return;

			ticksRemaining--;
			if (ticksRemaining <= 0)
				Reset();
		}

		public void ApplyFormationSpacing(int furthestDistance, int actorDistance)
		{
			if (furthestDistance <= 0)
			{
				Reset();
				return;
			}

			var difference = furthestDistance - actorDistance;
			if (difference <= info.IgnoreDistanceThreshold)
			{
				Reset();
				return;
			}

			var normalized = Math.Clamp((float)actorDistance / furthestDistance, 0f, 1f);
			var minPercent = Math.Clamp(info.MinSpeedPercent, 1, 100);
			var targetModifier = minPercent + (int)Math.Round((100 - minPercent) * normalized);
			targetModifier = Math.Clamp(targetModifier, minPercent, 100);

			var divisor = Math.Max(1, info.DistanceToDurationDivisor);
			var duration = info.BaseDurationTicks + (difference / divisor);
			if (info.MaxDurationTicks > 0)
				duration = Math.Min(duration, info.MaxDurationTicks);

			ApplySpeedPercent(targetModifier, duration);
		}

		public void ApplySpeedPercent(int percent, int? durationOverride = null)
		{
			var minPercent = Math.Clamp(info.MinSpeedPercent, 1, 100);
			currentModifier = Math.Clamp(percent, minPercent, 100);

			var duration = durationOverride ?? info.BaseDurationTicks;
			if (info.MaxDurationTicks > 0)
				duration = Math.Min(duration, info.MaxDurationTicks);

			ticksRemaining = Math.Max(1, duration);
		}

		public void Reset()
		{
			currentModifier = 100;
			ticksRemaining = 0;
		}
	}
}
