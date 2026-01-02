#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class BallisticMissileCAFly : Activity
	{
		readonly BallisticMissileCA sbm;
		readonly WPos initPos;
		readonly WPos actualTargetPos;
		readonly WPos flightTargetPos;
		readonly int duration;
		readonly float flightMultiplier;
		int ticks;
		WAngle facing;

		public BallisticMissileCAFly(Actor self, Target t, BallisticMissileCA sbm = null)
		{
			if (sbm == null)
				this.sbm = self.Trait<BallisticMissileCA>();
			else
				this.sbm = sbm;

			initPos = self.CenterPosition;
			actualTargetPos = t.CenterPosition; // fixed position == no homing
			flightMultiplier = (this.sbm.Info as BallisticMissileCAInfo)?.FlightTargetMultiplier ?? 1f;
			flightTargetPos = CalculateFlightTarget();
			var horizontalDistance = (flightTargetPos - initPos).HorizontalLength;
			duration = Math.Max(horizontalDistance / this.sbm.Info.Speed, 1);
			facing = (actualTargetPos - initPos).Yaw;
			sbm.Facing = GetEffectiveFacing();
		}

		WPos CalculateFlightTarget()
		{
			var multiplier = Math.Max(1f, flightMultiplier);
			if (multiplier == 1)
				return actualTargetPos;

			var offset = actualTargetPos - initPos;
			var horizontal = new WVec((int)(offset.X * multiplier), (int)(offset.Y * multiplier), 0);
			var vertical = new WVec(0, 0, offset.Z);
			return initPos + horizontal + vertical;
		}

		WAngle GetEffectiveFacing()
		{
			var at = (float)ticks / Math.Max(duration - 1, 1);
			var attitude = sbm.Info.LaunchAngle.Tan() * (1 - 2 * at) / (4 * 1024);

			// HACK HACK HACK
			// BodyOrientation does a 90° rotation on isometric worlds.
			// This calculation needs to be updated to accomodate that.
			var u = (facing.Angle % 512) / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		void FlyToward(Actor self, BallisticMissileCA sbm)
		{
			var pos = WPos.LerpQuadratic(initPos, flightTargetPos, sbm.Info.LaunchAngle, ticks, duration);
			sbm.SetPosition(self, pos);
			sbm.Facing = GetEffectiveFacing();
		}

		public override bool Tick(Actor self)
		{
			if (ticks >= duration)
			{
				var altitude = self.World.Map.DistanceAboveTerrain(actualTargetPos).Length;
				var groundPos = altitude > 0
					? actualTargetPos - new WVec(0, 0, altitude)
					: actualTargetPos;
				sbm.SetPosition(self, groundPos);
				Queue(new CallFunc(() => self.Kill(self)));
				return true;
			}

			FlyToward(self, sbm);
			ticks++;
			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(actualTargetPos);
		}
	}
}
