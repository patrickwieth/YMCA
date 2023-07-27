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

		[Desc("The terrain types that this actor should avoid wandering on to.")]
		public readonly HashSet<string> AvoidTerrainTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new Autobattler(init.Self, this); }
	}

	public class Autobattler : ConditionalTrait<AutobattlerInfo>, INotifyIdle
	{
		readonly Actor self;
		readonly AutobattlerInfo info;
		IResolveOrder move;
		Actor nextCheckpoint;
		Actor potentialNextCheckpoint;
		public int Hierarchy;
		int idleCountdown;
		int effectiveMoveRadius;
		int ticksIdle;

		public Autobattler(Actor self, AutobattlerInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
			effectiveMoveRadius = info.ScatterMoveRadius;
			idleCountdown = info.ScatterInterval;
		}

		protected override void Created(Actor self)
		{
			move = self.Trait<IMove>() as IResolveOrder;

			base.Created(self);
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--idleCountdown == 0)
			{
				var targetCell = PickScatterTargetLocation();
					if (targetCell.HasValue)
						DoScatter(self, targetCell.Value);

				idleCountdown = info.ScatterInterval;
				return;
			}

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

				attackMoveToNextCheckpoint(self);
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
				nextCheckpoint = potentialNextCheckpoint;
				Hierarchy = potentialNextCheckpoint.TraitOrDefault<Checkpoint>().Hierarchy;
			}
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

		CPos? PickScatterTargetLocation()
		{
			var target = self.CenterPosition + new WVec(0, -1024 * effectiveMoveRadius, 0).Rotate(WRot.FromFacing(self.World.SharedRandom.Next(255)));
			var targetCell = self.World.Map.CellContaining(target);

			if (!self.World.Map.Contains(targetCell))
			{
				// If MoveRadius is too big there might not be a valid cell to order the attack to (if actor is on a small island and can't leave)
				if (++ticksIdle % info.ReduceMoveRadiusDelay == 0)
					effectiveMoveRadius--;

				// We'll be back the next tick; better to sit idle for a few seconds than prolong this tick indefinitely with a loop
				return null;
			}

			if (info.AvoidTerrainTypes.Count > 0)
			{
				var terrainType = self.World.Map.GetTerrainInfo(targetCell).Type;
				if (Info.AvoidTerrainTypes.Contains(terrainType))
					return null;
			}

			ticksIdle = 0;
			effectiveMoveRadius = info.ScatterMoveRadius;

			return targetCell;
		}

		public virtual void DoScatter(Actor self, CPos targetCell)
		{
			move.ResolveOrder(self, new Order("Move", self, Target.FromCell(self.World, targetCell), false));
		}
	}
}
