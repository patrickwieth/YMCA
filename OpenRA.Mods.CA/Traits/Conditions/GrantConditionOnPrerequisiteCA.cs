#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition to the actor this is attached to when prerequisites are available.")]
	public class GrantConditionOnPrerequisiteCAInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("List of required prerequisites.")]
		public readonly string[] Prerequisites = System.Array.Empty<string>();

		public override object Create(ActorInitializer init) { return new GrantConditionOnPrerequisiteCA(init.Self, this); }
	}

	public class GrantConditionOnPrerequisiteCA : INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOwnerChanged
	{
		readonly GrantConditionOnPrerequisiteCAInfo info;

		bool wasAvailable;
		GrantConditionOnPrerequisiteManagerCA globalManager;
		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnPrerequisiteCA(Actor self, GrantConditionOnPrerequisiteCAInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			globalManager = self.Owner.PlayerActor.Trait<GrantConditionOnPrerequisiteManagerCA>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (info.Prerequisites.Length > 0)
				globalManager.Register(self, this, info.Prerequisites);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (info.Prerequisites.Length > 0)
				globalManager.Unregister(self, this, info.Prerequisites);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			globalManager = newOwner.PlayerActor.Trait<GrantConditionOnPrerequisiteManagerCA>();
		}

		public void PrerequisitesUpdated(Actor self, bool available)
		{
			if (available == wasAvailable)
				return;

			// fix for CA - only difference to standard GrantConditionOnPrerequisite is this part is wrapped in an AddFrameEndTask
			self.World.AddFrameEndTask(w =>
			{
				if (available && conditionToken == Actor.InvalidConditionToken)
					conditionToken = self.GrantCondition(info.Condition);
				else if (!available && conditionToken != Actor.InvalidConditionToken)
					conditionToken = self.RevokeCondition(conditionToken);

				wasAvailable = available;
			});
		}
	}
}
