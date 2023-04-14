#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Can be slaved to a spawner.")]
	public class AutobattlerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new Autobattler(init, this); }
	}

	public class Autobattler : ITick, INotifyCreated, INotifyIdle
	{
		public readonly AutobattlerInfo Info;

		Actor nextCheckpoint;

		public Autobattler(ActorInitializer init, AutobattlerInfo info)
		{
			Info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			Created(self);
		}

		protected virtual void Created(Actor self)
		{
			nextCheckpoint = self.World.ActorsHavingTrait<Checkpoint>()
				.Where(a => !a.IsDead).ClosestTo(self);
		}

		void ITick.Tick(Actor self)
		{

		}

		public void setNextCheckpoint(Actor cp) {

			nextCheckpoint = cp;

		}

		void INotifyIdle.TickIdle(Actor self)
		{
			var move = self.TraitOrDefault<IMove>();
			if (move != null && nextCheckpoint != null)
			{
				var location = self.World.Map.CellContaining(nextCheckpoint.CenterPosition);
				self.QueueActivity(false, new AttackMoveActivity(self, () => move.MoveTo(location, 0)));
			}
		}
	}
}
