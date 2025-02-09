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
using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Actor moves from Checkpoint to Checkpoint and fights autonomously.")]
	public class AutobattlerInfo : ConditionalTraitInfo
	{
		public readonly int ScatterMoveRadius = 1;

		[Desc("How long until scattering idle autobattle units")]
		public readonly int ScatterInterval = 10;

		[Desc("Number of ticks to wait before decreasing the effective move radius.")]
		public readonly int ReduceMoveRadiusDelay = 5;

		[Desc("The interval of game updates between autobattle units are sent forward")]
		public readonly int waveInterval = 1000;

		[Desc("The terrain types that this actor should avoid wandering on to.")]
		public readonly HashSet<string> AvoidTerrainTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new Autobattler(init.Self, this); }
	}

	public class Autobattler : ConditionalTrait<AutobattlerInfo>, ITick
	{
		readonly Actor self;
		readonly AutobattlerInfo info;
		IResolveOrder move;
		Actor nextCheckpoint;
		Actor potentialNextCheckpoint;
		IEnumerable<Actor> potentialInitCheckpoints;
		public int Hierarchy;
		int idleCountdown;
		int effectiveMoveRadius;
		int ticksIdle;
		int ticks;

		public Autobattler(Actor self, AutobattlerInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
			idleCountdown = info.ScatterInterval;
		}

		protected override void Created(Actor self)
		{
			if (IsTraitDisabled)
				return;

			move = self.Trait<IMove>() as IResolveOrder;

			base.Created(self);

			// here should be a check if the homeCheckpoint is set, if so skip setting
			var cpOrigin = self.Owner.PlayerActor.TraitOrDefault<CheckpointOrigin>();
			if (cpOrigin == null) {
				TextNotificationsManager.Debug("No CheckpointOrigin on player, this is bad, Autobattle mode does not work.");
			}
			else
			{
				if (cpOrigin.HomeCheckpoint == null)
				{
					potentialInitCheckpoints = self.World.ActorsHavingTrait<Checkpoint>().Where(a => !a.IsDead)
																			.OrderByDescending(x => x.TraitOrDefault<Checkpoint>().Hierarchy);
					nextCheckpoint = potentialInitCheckpoints.FirstOrDefault();
					potentialNextCheckpoint = potentialInitCheckpoints.LastOrDefault();

					if (nextCheckpoint != null && potentialNextCheckpoint != null)
					{
						potentialInitCheckpoints = new List<Actor> { nextCheckpoint, potentialNextCheckpoint};

						setNextCheckpoint(potentialInitCheckpoints.ClosestTo(self.World.Map.CenterOfCell(self.Owner.HomeLocation)));

						// check if the player already has a home checkpoint, if not set it
						if (cpOrigin.HomeCheckpoint == null)
						{
							cpOrigin.initCheckpointOrigin(nextCheckpoint);
						}
					}
					else TextNotificationsManager.Debug("No Checkpoint found, this map is not suited for Autobattle mode!");
				}
				else
				{
					setNextCheckpoint(cpOrigin.HomeCheckpoint);
				}
			}


		}

		void ITick.Tick(Actor self) {
			//TextNotificationsManager.Debug(ticks.ToString());
			if (++ticks >= info.waveInterval) {
				ticks = 0;
				attackMoveToNextCheckpoint(self);
			}
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
			}
		}

		public void findNextCheckpoint(bool ascending) {
			if (nextCheckpoint != null)
			{
				if (ascending)
					potentialNextCheckpoint = self.World.ActorsHavingTrait<Checkpoint>()
						.Where(a => a.TraitOrDefault<Checkpoint>().Hierarchy > Hierarchy)
						.ClosestTo(self.World.Map.CenterOfCell(nextCheckpoint.TraitOrDefault<Checkpoint>().RallyPoint.Path.FirstOrDefault()));
				else
					potentialNextCheckpoint = self.World.ActorsHavingTrait<Checkpoint>()
						.Where(a => a.TraitOrDefault<Checkpoint>().Hierarchy < Hierarchy)
						.ClosestTo(self.World.Map.CenterOfCell(nextCheckpoint.TraitOrDefault<Checkpoint>().RallyPoint.Path.FirstOrDefault()));
			}

			//TextNotificationsManager.Debug("rallypoint"+nextCheckpoint.TraitOrDefault<Checkpoint>().RallyPoint.Path.FirstOrDefault());
			if (potentialNextCheckpoint != null) {
				setNextCheckpoint(potentialNextCheckpoint);
			}
		}

		void setNextCheckpoint(Actor next) {
			nextCheckpoint = next;
			Hierarchy = next.TraitOrDefault<Checkpoint>().Hierarchy;
			ticks = next.TraitOrDefault<Checkpoint>().Ticks;
		}

		void attackMoveToNextCheckpoint(Actor self)
		{
			var move = self.TraitOrDefault<IMove>();
			if (move != null && nextCheckpoint != null)
			{
				//TextNotificationsManager.Debug("order:"+nextCheckpoint);
				var location = self.World.Map.CellContaining(nextCheckpoint.CenterPosition);
				self.QueueActivity(false, new AttackMoveActivity(self, () => move.MoveTo(location, 1, evaluateNearestMovableCell: true, targetLineColor: Color.OrangeRed)));
				//move.ResolveOrder(self, new Order("AttackMove", null, Target.FromCell(self.World, location), false));
			}
		}

	}
}
