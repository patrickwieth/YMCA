using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	static class GeneratedMapMode
	{
		public static bool Is(Actor actor, string mode)
		{
			return actor.World.Map.Categories.Contains(mode, StringComparer.OrdinalIgnoreCase);
		}
	}

	[Desc("Grants a condition when the generated map has the requested fixed game mode category.")]
	public class GrantConditionOnMapModeInfo : TraitInfo
	{
		public readonly string Mode = "Tactical";

		[GrantedConditionReference]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init)
		{
			return new GrantConditionOnMapMode(init.Self, this);
		}
	}

	public class GrantConditionOnMapMode : INotifyCreated, INotifyActorDisposing
	{
		readonly GrantConditionOnMapModeInfo info;
		int token = Actor.InvalidConditionToken;

		public GrantConditionOnMapMode(Actor self, GrantConditionOnMapModeInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (GeneratedMapMode.Is(self, info.Mode))
				token = self.GrantCondition(info.Condition);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}
	}

	[Desc("A building-local production queue enabled only in Operational and Strategic generated maps.")]
	public class GeneratedModeProductionQueueInfo : ProductionQueueInfo
	{
		public readonly string[] Modes = { "Operational", "Strategic" };

		public override object Create(ActorInitializer init)
		{
			return new GeneratedModeProductionQueue(init, this);
		}
	}

	public class GeneratedModeProductionQueue : ProductionQueue
	{
		public GeneratedModeProductionQueue(ActorInitializer init, GeneratedModeProductionQueueInfo info)
			: base(init, info)
		{
			Enabled &= info.Modes.Any(mode => GeneratedMapMode.Is(init.Self, mode));
		}
	}

	[Desc("A shared production queue disabled in Operational and Strategic generated maps.")]
	public class TacticalProductionQueueInfo : ClassicProductionQueueInfo
	{
		public readonly string[] DisabledModes = { "Operational", "Strategic" };

		public override object Create(ActorInitializer init)
		{
			return new TacticalProductionQueue(init, this);
		}
	}

	public class TacticalProductionQueue : ClassicProductionQueue
	{
		public TacticalProductionQueue(ActorInitializer init, TacticalProductionQueueInfo info)
			: base(init, info)
		{
			Enabled &= !info.DisabledModes.Any(mode => GeneratedMapMode.Is(init.Self, mode));
		}
	}

	[Desc("Wins a Strategic generated map after the team controls every checkpoint for the configured duration.")]
	public class CheckpointVictoryConditionsInfo : ConditionalTraitInfo, Requires<MissionObjectivesInfo>
	{
		public readonly int HoldDuration = 1500;
		public readonly string Objective = "Hold all checkpoints for 60 seconds.";

		public override object Create(ActorInitializer init)
		{
			return new CheckpointVictoryConditions(init.Self, this);
		}
	}

	public class CheckpointVictoryConditions : ConditionalTrait<CheckpointVictoryConditionsInfo>, ITick
	{
		readonly Player player;
		readonly MissionObjectives objectives;
		int objectiveId = -1;
		int ticksLeft;

		public CheckpointVictoryConditions(Actor self, CheckpointVictoryConditionsInfo info)
			: base(info)
		{
			player = self.Owner;
			objectives = self.Trait<MissionObjectives>();
			ticksLeft = info.HoldDuration;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || player.NonCombatant || player.WinState != WinState.Undefined)
				return;
			if (objectiveId < 0)
				objectiveId = objectives.Add(player, Info.Objective, "Primary", inhibitAnnouncement: true);

			var checkpoints = self.World.ActorsHavingTrait<StrategicPoint>().Where(actor => !actor.IsDead).ToArray();
			var holdingAll = checkpoints.Length > 0 && checkpoints.All(actor => actor.Owner.RelationshipWith(player) == PlayerRelationship.Ally);
			if (!holdingAll)
			{
				ticksLeft = Info.HoldDuration;
				return;
			}

			if (--ticksLeft <= 0)
				objectives.MarkCompleted(player, objectiveId);
		}
	}
}
