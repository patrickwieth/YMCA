#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class GrantConditionOnHarvestingInfo : PausableConditionalTraitInfo, Requires<HarvesterInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant while harvesting.")]
		public readonly string Condition = null;

		[Desc("Duration of harvesting condition.")]
		public readonly int Duration = 10;

		public override object Create(ActorInitializer init) { return new GrantConditionOnHarvesting(init, this); }
	}

	public class GrantConditionOnHarvesting : PausableConditionalTrait<GrantConditionOnHarvestingInfo>, ITick, ISync, INotifyCreated, INotifyHarvestAction, INotifyDockClientMoving
	{
		readonly GrantConditionOnHarvestingInfo info;
		readonly Harvester harvester;
		readonly string conditionToGrant;
		readonly Actor self;
		int token = Actor.InvalidConditionToken;
		IConditionTimerWatcher[] watchers;

		[Sync]
		public int Ticks { get; private set; }

		public GrantConditionOnHarvesting(ActorInitializer init, GrantConditionOnHarvestingInfo info)
			: base(info)
		{
			this.info = info;
			conditionToGrant = info.Condition;
			self = init.Self;
			harvester = init.Self.Trait<Harvester>();
		}

		protected override void Created(Actor self)
		{
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();

			base.Created(self);
		}

		void INotifyHarvestAction.Harvested(Actor self, string resourceType)
		{
			if (token == Actor.InvalidConditionToken)
			{
				Ticks = info.Duration;
				token = self.GrantCondition(conditionToGrant);
			}
		}

		void RevokeCondition(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled && token != Actor.InvalidConditionToken)
				RevokeCondition(self);

			if (IsTraitPaused || IsTraitDisabled)
				return;

			foreach (var w in watchers)
				w.Update(info.Duration, Ticks);

			if (token == Actor.InvalidConditionToken)
				return;

			if (--Ticks < 1)
				RevokeCondition(self);
		}

		void INotifyHarvestAction.MovingToResources(Actor self, CPos targetCell) { }
		void INotifyHarvestAction.MovementCancelled(Actor self) { }
		void INotifyDockClientMoving.MovingToDock(Actor self, Actor refineryActor, IDockHost host, bool forceEnter) { }
		void INotifyDockClientMoving.MovementCancelled(Actor self) { }

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }
	}
}
