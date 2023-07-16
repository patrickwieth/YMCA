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
	[Desc("Actor moves from Checkpoint to Checkpoint and fights autonomously.")]
	public class AutobattlerInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new Autobattler(init, this); }
	}

	public class Autobattler : ConditionalTrait<AutobattlerInfo>, INotifyIdle
	{
		Actor Self;
		Actor nextCheckpoint;
		Actor potentialNextCheckpoint;
		readonly AutobattlerInfo info;
		public int Hierarchy;

		public Autobattler(ActorInitializer init, AutobattlerInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
		}

		public void findNextCheckpoint(bool ascending) {

			TextNotificationsManager.Debug("own hierarchy "+Hierarchy);
			if (nextCheckpoint != null)
			{
				TextNotificationsManager.Debug("old Checkpoint Hierarchy "+nextCheckpoint.TraitOrDefault<Checkpoint>().Hierarchy);

				if (ascending)
					potentialNextCheckpoint = Self.World.ActorsHavingTrait<Checkpoint>()
						.Where(a => a.TraitOrDefault<Checkpoint>().Hierarchy > Hierarchy)
						.ClosestTo(Self.World.Map.CenterOfCell(nextCheckpoint.TraitOrDefault<Checkpoint>().RallyPoint.Path.FirstOrDefault()));
				else
					potentialNextCheckpoint = Self.World.ActorsHavingTrait<Checkpoint>()
						.Where(a => a.TraitOrDefault<Checkpoint>().Hierarchy < Hierarchy)
						.ClosestTo(Self.World.Map.CenterOfCell(nextCheckpoint.TraitOrDefault<Checkpoint>().RallyPoint.Path.FirstOrDefault()));
			}

			//TextNotificationsManager.Debug("rallypoint"+nextCheckpoint.TraitOrDefault<Checkpoint>().RallyPoint.Path.FirstOrDefault());
			if (potentialNextCheckpoint != null) {
				TextNotificationsManager.Debug("new Checkpoint "+potentialNextCheckpoint);
				TextNotificationsManager.Debug("new Checkpoint Hierarchy "+potentialNextCheckpoint.TraitOrDefault<Checkpoint>().Hierarchy);

				nextCheckpoint = potentialNextCheckpoint;
				Hierarchy = potentialNextCheckpoint.TraitOrDefault<Checkpoint>().Hierarchy;
			}
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled)
				return;

			Self = self;
			nextCheckpoint = self.World.ActorsHavingTrait<Checkpoint>()
				.Where(a => !a.IsDead).ClosestTo(self);

			if (nextCheckpoint != null)
			{
				// set the Checkpoint Hierarchy for this unit
				Hierarchy = nextCheckpoint.TraitOrDefault<Checkpoint>().Hierarchy;

				var cpOrigin = self.Owner.PlayerActor.TraitOrDefault<CheckpointOrigin>();
				if (cpOrigin != null)
				{
					// check if the player already has a home checkpoint, if not set it
					if (cpOrigin.HomeCheckpoint == null)
					{
						cpOrigin.initCheckpointOrigin(self);
					}
				}
				else
					TextNotificationsManager.Debug("No CheckpointOrigin on player, this is bad.");

				attackMoveToNextCheckpoint(self);
			}
			else
				TextNotificationsManager.Debug("No Checkpoint found, this map is not suited for Autobattle mode!");
		}

		public void handleCheckpoint(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var cpOrigin = self.Owner.PlayerActor.TraitOrDefault<CheckpointOrigin>();
			if (cpOrigin != null)
			{
				if (cpOrigin.HierarchyAscending)
				{
					findNextCheckpoint(true);
				}
				else {
					findNextCheckpoint(false);
				}

				TextNotificationsManager.Debug("new order "+nextCheckpoint);
				attackMoveToNextCheckpoint(self);
			}
		}

		void attackMoveToNextCheckpoint(Actor self)
		{
			var move = self.TraitOrDefault<IMove>();
			if (move != null && nextCheckpoint != null)
			{
				//TextNotificationsManager.Debug("order:"+nextCheckpoint);
				var location = self.World.Map.CellContaining(nextCheckpoint.CenterPosition);
				self.QueueActivity(false, new AttackMoveActivity(self, () => move.MoveTo(location, 0)));
			}
		}
	}
}
