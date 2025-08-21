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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class GrantConditionOnDockingHarvesterInfo : TraitInfo, Requires<HarvesterInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant when docking.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnDockingHarvester(init, this); }
	}

	public class GrantConditionOnDockingHarvester : INotifyDockClient
	{
		readonly Harvester harvester;
		readonly string conditionToGrant;
		readonly Actor self;
		int token = Actor.InvalidConditionToken;

		public GrantConditionOnDockingHarvester(ActorInitializer init, GrantConditionOnDockingHarvesterInfo info)
		{
			conditionToGrant = info.Condition;
			self = init.Self;
			harvester = init.Self.Trait<Harvester>();
		}

		void INotifyDockClient.Docked(Actor self, Actor dock)
		{
			token = self.GrantCondition(conditionToGrant);
		}

		void INotifyDockClient.Undocked(Actor self, Actor dock)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}
	}
}
