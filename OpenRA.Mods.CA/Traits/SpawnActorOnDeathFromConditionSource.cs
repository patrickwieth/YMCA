#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// Track actual external-condition tokens rather than a separate expiry timer. This also
	// handles revocation, source caps and owner changes performed by ExternalCondition.
	internal sealed class ConditionSourceTracker<T> where T : class
	{
		readonly List<(int Token, T Owner)> sources = new();

		public void Record(int token, T owner, Func<int, bool> valid)
		{
			sources.RemoveAll(s => !valid(s.Token));
			if (valid(token))
				sources.Add((token, owner));
		}

		public T Latest(Func<int, bool> valid)
		{
			sources.RemoveAll(s => !valid(s.Token));
			return sources.Count == 0 ? null : sources[sources.Count - 1].Owner;
		}
	}

	[Desc("Spawns an actor for the owner who applied the most recent still-active tracked external condition, not the killing player.")]
	public class SpawnActorOnDeathFromConditionSourceInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		public readonly string Actor = null;

		[FieldLoader.Require]
		public readonly string SourceCondition = null;

		public readonly int Probability = 100;
		public readonly bool SpawnAfterDefeat = false;
		public readonly bool SkipMakeAnimations = false;

		public override object Create(ActorInitializer init) => new SpawnActorOnDeathFromConditionSource(init, this);
	}

	public class SpawnActorOnDeathFromConditionSource : ConditionalTrait<SpawnActorOnDeathFromConditionSourceInfo>, INotifyKilled, INotifyRemovedFromWorld
	{
		readonly ConditionSourceTracker<Player> sources = new();
		readonly string faction;
		Player mutationOwner;

		public SpawnActorOnDeathFromConditionSource(ActorInitializer init, SpawnActorOnDeathFromConditionSourceInfo info)
			: base(info)
		{
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		public void RecordSource(Actor self, string condition, int token, Player owner)
		{
			if (condition == Info.SourceCondition)
				sources.Record(token, owner, self.TokenValid);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled || !self.IsInWorld)
				return;

			// Snapshot the player at application time: later capture/death of the source
			// actor, or a different player landing the killing blow, must not transfer it.
			var owner = sources.Latest(self.TokenValid);
			if (owner == null || self.World.SharedRandom.Next(100) > Info.Probability)
				return;

			mutationOwner = owner;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (mutationOwner == null || (self.Owner.WinState == WinState.Lost && !Info.SpawnAfterDefeat))
				return;

			var init = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(self.Location),
				new CenterPositionInit(self.CenterPosition),
				new FactionInit(faction),
				new OwnerInit(mutationOwner)
			};

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised)
				init.Add(new EffectiveOwnerInit(self.EffectiveOwner.Owner));
			if (Info.SkipMakeAnimations)
				init.Add(new SkipMakeAnimsInit());
			foreach (var modifier in self.TraitsImplementing<IDeathActorInitModifier>())
				modifier.ModifyDeathActorInit(self, init);

			self.World.AddFrameEndTask(w => w.CreateActor(Info.Actor, init));
		}
	}
}
