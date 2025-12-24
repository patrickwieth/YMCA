#region Copyright & License Information
/*
 * Copyright 2015-2025 OpenRA.Mods.CA Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	sealed class PassengerPayloadInit : ValueActorInit<Actor>, ISuppressInitExport
	{
		public PassengerPayloadInit(Actor passenger)
			: base(passenger) { }
	}

	public class PassengerPayloadInfo : TraitInfo
	{
		[Desc("Offset applied when releasing the passenger (relative to the projectile).")]
		public readonly WVec DropOffset = WVec.Zero;

		[Desc("Queue a parachute activity for the passenger on release.")]
		public readonly bool QueueParachute = true;

		[Desc("Release the passenger once the projectile is within this horizontal distance of its target. Zero keeps existing \"on death\" behaviour.")]
		public readonly WDist ReleaseDistance = WDist.Zero;

		[Desc("Ticks after projectile spawn when the passenger should be released. Zero disables time-based release.")]
		public readonly int ReleaseAfterTicks = 0;

		[Desc("Kill the host projectile immediately after releasing early to trigger its death effects.")]
		public readonly bool KillOnEarlyRelease = true;

		[Desc("Issue an attack-move after landing this many cells beyond the drop position along the projectile's travel direction. Zero disables.")]
		public readonly int AttackMoveCells = 0;

		public override object Create(ActorInitializer init) { return new PassengerPayload(init, this); }
	}

	public class PassengerPayload : INotifyKilled, INotifyRemovedFromWorld, ITick
	{
		readonly PassengerPayloadInfo info;
		readonly Actor passenger;
		readonly Func<Target> targetProvider;
		readonly long releaseDistanceSquared;
		readonly int releaseTickThreshold;
		readonly int attackMoveCells;
		int ticks;
		bool released;

		public PassengerPayload(ActorInitializer init, PassengerPayloadInfo info)
		{
			this.info = info;
			passenger = init.GetOrDefault<PassengerPayloadInit>(info)?.Value;
			var caMissile = init.Self.TraitOrDefault<MissileBase>();
			if (caMissile != null)
				targetProvider = () => caMissile.Target;
			else
			{
				var ballisticMissile = init.Self.TraitOrDefault<BallisticMissile>();
				if (ballisticMissile != null)
					targetProvider = () => ballisticMissile.Target;
			}

			if (info.ReleaseDistance.Length > 0)
			{
				var dist = (long)info.ReleaseDistance.Length;
				releaseDistanceSquared = dist * dist;
			}

			releaseTickThreshold = info.ReleaseAfterTicks;
			attackMoveCells = info.AttackMoveCells;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			ReleasePassenger(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			ReleasePassenger(self);
		}

		void ITick.Tick(Actor self)
		{
			if (released || passenger == null)
				return;

			ticks++;

			var shouldRelease = false;

			if (releaseTickThreshold > 0 && ticks >= releaseTickThreshold)
				shouldRelease = true;
			else if (releaseDistanceSquared > 0 && targetProvider != null)
			{
				var target = targetProvider();
				if (target.Type != TargetType.Invalid)
				{
					var delta = target.CenterPosition - self.CenterPosition;
					var horizontalDistanceSquared = (long)delta.HorizontalLengthSquared;
					if (horizontalDistanceSquared <= releaseDistanceSquared)
						shouldRelease = true;
				}
			}

			if (!shouldRelease)
				return;

			ReleasePassenger(self);

			if (info.KillOnEarlyRelease && !self.IsDead)
				self.Kill(self);
		}

		void ReleasePassenger(Actor self)
		{
			if (released || passenger == null)
				return;

			released = true;

			var dropPosition = self.CenterPosition + info.DropOffset;
			self.World.AddFrameEndTask(w =>
			{
				if (passenger.IsDead)
					return;

				var pos = passenger.Trait<IPositionable>();
				var cell = w.Map.CellContaining(dropPosition);
				var subCell = pos.GetAvailableSubCell(cell);
				pos.SetPosition(passenger, cell, subCell);
				pos.SetPosition(passenger, dropPosition);

				w.Add(passenger);

				if (info.QueueParachute)
					passenger.QueueActivity(new Parachute(passenger));

				QueueAttackMove(self, passenger, dropPosition);
			});
		}

		void QueueAttackMove(Actor host, Actor passenger, WPos dropPosition)
		{
			if (attackMoveCells <= 0)
				return;

			var move = passenger.TraitOrDefault<IMove>();
			if (move == null)
				return;

			var offset = GetAttackMoveOffset(host, dropPosition);
			if (offset == WVec.Zero)
				return;

			var attackPos = dropPosition + offset;
			var targetCell = host.World.Map.CellContaining(attackPos);
			passenger.QueueActivity(new AttackMoveActivity(passenger, () =>
				move.MoveTo(targetCell, 1, evaluateNearestMovableCell: true, targetLineColor: Color.OrangeRed)));
		}

		WVec GetAttackMoveOffset(Actor host, WPos dropPosition)
		{
			var desired = 1024 * attackMoveCells;
			if (desired <= 0)
				return WVec.Zero;

			WVec direction = WVec.Zero;
			if (targetProvider != null)
			{
				var target = targetProvider();
				if (target.Type != TargetType.Invalid)
					direction = new WVec(target.CenterPosition.X - dropPosition.X, target.CenterPosition.Y - dropPosition.Y, 0);
			}

			if (direction.HorizontalLengthSquared == 0)
				direction = new WVec(0, -1024, 0).Rotate(host.Orientation);

			var length = direction.HorizontalLength;
			if (length == 0)
				return WVec.Zero;

			return new WVec(direction.X * desired / length, direction.Y * desired / length, 0);
		}
	}
}
