using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum ArmyLeaderGroup : byte
	{
		Red,
		Blue,
		Black,
	}

	[Desc("Identifies an Operational mode Army Leader.")]
	public class ArmyLeaderInfo : TraitInfo
	{
		public readonly ArmyLeaderGroup Group = ArmyLeaderGroup.Red;

		public override object Create(ActorInitializer init)
		{
			return new ArmyLeader(this);
		}
	}

	public class ArmyLeader
	{
		public readonly ArmyLeaderGroup Group;

		public ArmyLeader(ArmyLeaderInfo info)
		{
			Group = info.Group;
		}
	}

	[Desc("Makes a unit follow its assigned Army Leader using attack-move.")]
	public class ArmyFollowerInfo : ConditionalTraitInfo
	{
		[Desc("Maximum distance maintained from the Army Leader.")]
		public readonly WDist FollowRange = WDist.FromCells(5);

		public override object Create(ActorInitializer init)
		{
			return new ArmyFollower(init.Self, this);
		}
	}

	public class ArmyFollower : ConditionalTrait<ArmyFollowerInfo>, INotifyCreated, ITick
	{
		readonly Actor self;
		readonly ArmyFollowerInfo info;
		IMove move;
		Actor assignedLeader;
		ArmyLeaderGroup group = ArmyLeaderGroup.Red;
		int retryTicks;

		public ArmyFollower(Actor self, ArmyFollowerInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
		}

		protected override void Created(Actor actor)
		{
			move = self.TraitOrDefault<IMove>();
			base.Created(actor);
			FollowAssignedLeader();
		}

		protected override void TraitEnabled(Actor actor)
		{
			base.TraitEnabled(actor);
			FollowAssignedLeader();
		}

		public ArmyLeaderGroup Group => group;

		public void Assign(ArmyLeaderGroup assignedGroup)
		{
			group = assignedGroup;
			assignedLeader = null;
			FollowAssignedLeader();
		}

		void ITick.Tick(Actor actor)
		{
			if (assignedLeader == null && ++retryTicks >= 25)
			{
				retryTicks = 0;
				FollowAssignedLeader();
			}
		}

		void FollowAssignedLeader()
		{
			if (IsTraitDisabled || move == null)
				return;
			var leader = self.World.ActorsWithTrait<ArmyLeader>()
				.Where(candidate => !candidate.Actor.IsDead && candidate.Actor.Owner == self.Owner &&
					candidate.Trait.Group == group)
				.Select(candidate => candidate.Actor)
				.FirstOrDefault();
			if (leader == null)
				return;

			assignedLeader = leader;
			var target = Target.FromActor(leader);
			self.QueueActivity(false, new AttackMoveActivity(self, () =>
				move.MoveFollow(self, target, WDist.Zero, info.FollowRange, targetLineColor: Color.OrangeRed)));
		}
	}

	[Desc("Spawns the initial Red Army Leader at the player spawn in Operational mode.")]
	public class ArmyLeaderSpawnerInfo : ConditionalTraitInfo
	{
		[ActorReference]
		public readonly string Actor = "army.leader.red";

		public override object Create(ActorInitializer init)
		{
			return new ArmyLeaderSpawner(init.Self, this);
		}
	}

	public class ArmyLeaderSpawner : ConditionalTrait<ArmyLeaderSpawnerInfo>
	{
		readonly Actor self;
		readonly ArmyLeaderSpawnerInfo info;
		bool spawnRequested;

		public ArmyLeaderSpawner(Actor self, ArmyLeaderSpawnerInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
		}

		protected override void Created(Actor actor)
		{
			base.Created(actor);
			TrySpawnLeader();
		}

		protected override void TraitEnabled(Actor actor)
		{
			base.TraitEnabled(actor);
			TrySpawnLeader();
		}

		void TrySpawnLeader()
		{
			if (spawnRequested || IsTraitDisabled || self.Owner.NonCombatant)
				return;
			spawnRequested = true;
			SpawnLeader(self, info.Actor);
		}

		public static void SpawnLeader(Actor source, string actorName)
		{
			var actorInfo = source.World.Map.Rules.Actors[actorName];
			var leaderInfo = actorInfo.TraitInfo<ArmyLeaderInfo>();
			if (source.World.ActorsWithTrait<ArmyLeader>().Any(candidate =>
				candidate.Actor.Owner == source.Owner && candidate.Trait.Group == leaderInfo.Group && !candidate.Actor.IsDead))
				return;

			var home = source.Owner.HomeLocation;
			var offset = leaderInfo.Group switch
			{
				ArmyLeaderGroup.Red => new CVec(-2, 0),
				ArmyLeaderGroup.Blue => new CVec(0, -2),
				_ => new CVec(2, 0),
			};
			var location = home + offset;
			if (!source.World.Map.Contains(location))
				location = home;
			var initializers = new TypeDictionary
			{
				new OwnerInit(source.Owner),
				new LocationInit(location),
			};
			source.World.AddFrameEndTask(world => world.CreateActor(actorName, initializers));
		}
	}

	[Desc("Spawns an Army Leader when a Commander promotion actor is created.")]
	public class SpawnArmyLeaderInfo : TraitInfo
	{
		[ActorReference]
		public readonly string Actor = null;

		public override object Create(ActorInitializer init)
		{
			return new SpawnArmyLeader(init.Self, this);
		}
	}

	public class SpawnArmyLeader : INotifyCreated
	{
		readonly Actor self;
		readonly SpawnArmyLeaderInfo info;

		public SpawnArmyLeader(Actor self, SpawnArmyLeaderInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void INotifyCreated.Created(Actor actor)
		{
			ArmyLeaderSpawner.SpawnLeader(self, info.Actor);
		}
	}

	[Desc("Stores the Army Leader assignment for units produced by this building.")]
	public class ArmyProductionAssignmentInfo : TraitInfo
	{
		public readonly Color RedColor = Color.Red;
		public readonly Color BlueColor = Color.Blue;
		public readonly Color BlackColor = Color.Black;

		public override object Create(ActorInitializer init)
		{
			return new ArmyProductionAssignment(init.Self, this);
		}
	}

	public class ArmyProductionAssignment : INotifyProduction, IResolveOrder, ISync, ISelectionBar
	{
		readonly Actor self;
		readonly ArmyProductionAssignmentInfo info;

		[Sync]
		int selectedGroup = (int)ArmyLeaderGroup.Red;

		public ArmyProductionAssignment(Actor self, ArmyProductionAssignmentInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void INotifyProduction.UnitProduced(Actor self, Actor produced, CPos exit)
		{
			if (TryGetSelection(produced.Info.Name, out var selection))
			{
				selectedGroup = (int)selection;
				return;
			}

			produced.TraitOrDefault<ArmyFollower>()?.Assign((ArmyLeaderGroup)selectedGroup);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SetArmyLeaderGroup" && order.ExtraData <= (uint)ArmyLeaderGroup.Black)
				selectedGroup = (int)order.ExtraData;
		}

		float ISelectionBar.GetValue()
		{
			return GeneratedMapMode.Is(self, "Operational") ? 1f : 0f;
		}

		bool ISelectionBar.DisplayWhenEmpty => false;

		Color ISelectionBar.GetColor()
		{
			return (ArmyLeaderGroup)selectedGroup switch
			{
				ArmyLeaderGroup.Red => info.RedColor,
				ArmyLeaderGroup.Blue => info.BlueColor,
				_ => info.BlackColor,
			};
		}

		static bool TryGetSelection(string actorName, out ArmyLeaderGroup group)
		{
			switch (actorName)
			{
				case "army.assignment.red":
					group = ArmyLeaderGroup.Red;
					return true;
				case "army.assignment.blue":
					group = ArmyLeaderGroup.Blue;
					return true;
				case "army.assignment.black":
					group = ArmyLeaderGroup.Black;
					return true;
				default:
					group = default;
					return false;
			}
		}
	}

	[Desc("Marks the defensive gathering point used by Operational mode bots.")]
	public class ArmyRallyPointInfo : TraitInfo
	{
		public override object Create(ActorInitializer init)
		{
			return new ArmyRallyPoint();
		}
	}

	public class ArmyRallyPoint { }

	[Desc("Controls Army Leaders and production assignments for Operational mode bots.")]
	public class OperationalArmyLeaderBotModuleInfo : ConditionalTraitInfo
	{
		public readonly int MinimumOrderInterval = 125;
		public readonly int MaximumOrderInterval = 200;
		public readonly int MinimumArmyValue = 4000;
		public readonly int MinimumCombinedArmyValue = 6000;
		public readonly int RetreatArmyValue = 1250;
		public readonly int RetreatConfirmationChecks = 3;
		public readonly int AttackAdvanceCells = 6;

		public override object Create(ActorInitializer init)
		{
			return new OperationalArmyLeaderBotModule(init.Self, this);
		}
	}

	public class OperationalArmyLeaderBotModule : ConditionalTrait<OperationalArmyLeaderBotModuleInfo>, IBotTick
	{
		readonly Actor self;
		readonly OperationalArmyLeaderBotModuleInfo info;
		readonly Dictionary<ArmyLeaderGroup, bool> attacking = new();
		readonly Dictionary<ArmyLeaderGroup, int> retreatChecks = new();
		int orderTicks;

		public OperationalArmyLeaderBotModule(Actor self, OperationalArmyLeaderBotModuleInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (IsTraitDisabled || --orderTicks > 0)
				return;
			orderTicks = self.World.LocalRandom.Next(info.MinimumOrderInterval, info.MaximumOrderInterval);

			UnlockLeaders();
			var leaders = self.World.ActorsWithTrait<ArmyLeader>()
				.Where(candidate => candidate.Actor.Owner == self.Owner && !candidate.Actor.IsDead)
				.OrderBy(candidate => candidate.Trait.Group)
				.ToArray();
			if (leaders.Length == 0)
				return;

			var targets = self.World.Actors
				.Where(actor => actor.IsInWorld && !actor.IsDead && actor.Owner.RelationshipWith(self.Owner) == PlayerRelationship.Enemy &&
					actor.Info.HasTraitInfo<BuildingInfo>())
				.ToArray();
			var rallyPoint = self.World.ActorsWithTrait<ArmyRallyPoint>()
				.Where(candidate => !candidate.Actor.IsDead)
				.Select(candidate => candidate.Actor)
				.OrderBy(actor => (actor.CenterPosition - self.World.Map.CenterOfCell(self.Owner.HomeLocation)).HorizontalLengthSquared)
				.FirstOrDefault();
			var rallyCell = rallyPoint?.Location ?? self.Owner.HomeLocation;

			var followers = self.World.ActorsWithTrait<ArmyFollower>()
				.Where(candidate => candidate.Actor.Owner == self.Owner && !candidate.Actor.IsDead && candidate.Actor.IsInWorld)
				.ToArray();
			var combinedArmyValue = followers.Sum(candidate =>
				candidate.Actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0);
			var launchCombinedWave = combinedArmyValue >= info.MinimumCombinedArmyValue;
			foreach (var leader in leaders)
			{
				var group = leader.Trait.Group;
				var groupFollowers = followers.Where(candidate => candidate.Trait.Group == group).ToArray();
				var armyValue = groupFollowers.Sum(candidate =>
					candidate.Actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0);
				var isAttacking = attacking.TryGetValue(group, out var attack) && attack;
				if (isAttacking)
				{
					var shouldRetreat = targets.Length == 0 || armyValue <= info.RetreatArmyValue;
					retreatChecks[group] = shouldRetreat
						? retreatChecks.GetValueOrDefault(group) + 1
						: 0;
					if (retreatChecks[group] >= info.RetreatConfirmationChecks)
						isAttacking = false;
				}
				else if ((armyValue >= info.MinimumArmyValue ||
					launchCombinedWave && armyValue > info.RetreatArmyValue) && targets.Length > 0)
				{
					isAttacking = true;
					retreatChecks[group] = 0;
				}
				attacking[group] = isAttacking;

				var destination = rallyCell;
				if (isAttacking)
				{
					var target = targets.OrderBy(actor =>
						(actor.CenterPosition - leader.Actor.CenterPosition).HorizontalLengthSquared).First();
					destination = AttackWaypoint(groupFollowers, leader.Actor.Location, target.Location);
				}
				bot.QueueOrder(new Order("Move", leader.Actor, Target.FromCell(self.World, destination), false));
			}

			// Keep new production with gathering armies. This prevents reinforcements
			// from streaming one-by-one across the map after an attack has launched.
			var gatheringLeaders = leaders.Where(leader => !attacking[leader.Trait.Group]).ToArray();
			if (gatheringLeaders.Length == 0)
				gatheringLeaders = leaders;
			var assignments = self.World.ActorsWithTrait<ArmyProductionAssignment>()
				.Where(candidate => candidate.Actor.Owner == self.Owner && !candidate.Actor.IsDead)
				.OrderBy(candidate => candidate.Actor.ActorID)
				.ToArray();
			for (var index = 0; index < assignments.Length; index++)
				bot.QueueOrder(new Order("SetArmyLeaderGroup", assignments[index].Actor, false)
				{
					ExtraData = (uint)gatheringLeaders[index % gatheringLeaders.Length].Trait.Group,
				});
		}

		CPos AttackWaypoint(TraitPair<ArmyFollower>[] followers, CPos leader, CPos target)
		{
			if (followers.Length == 0)
				return leader;

			var totalValue = followers.Sum(candidate =>
				candidate.Actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0);
			var caughtUpValue = followers.Where(candidate =>
			{
				var dx = candidate.Actor.Location.X - leader.X;
				var dy = candidate.Actor.Location.Y - leader.Y;
				return dx * dx + dy * dy <= 100;
			}).Sum(candidate => candidate.Actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0);

			// Hold position instead of being pulled backwards by stragglers. Once most
			// of the wave has caught up, advance another short step towards the target.
			if (totalValue > 0 && caughtUpValue * 100 < totalValue * 60)
				return leader;

			var dxToTarget = target.X - leader.X;
			var dyToTarget = target.Y - leader.Y;
			var length = Math.Sqrt(dxToTarget * dxToTarget + dyToTarget * dyToTarget);
			if (length <= info.AttackAdvanceCells)
				return target;

			var waypoint = new CPos(
				(int)Math.Round(leader.X + dxToTarget / length * info.AttackAdvanceCells),
				(int)Math.Round(leader.Y + dyToTarget / length * info.AttackAdvanceCells));
			return self.World.Map.Contains(waypoint) ? waypoint : leader;
		}

		void UnlockLeaders()
		{
			// PlayerPromotions is supplied by OpenRA.Mods.Cameo, which is loaded
			// alongside this assembly but is intentionally not a compile-time dependency.
			var promotions = self.TraitsImplementing<object>()
				.FirstOrDefault(trait => trait.GetType().Name == "PlayerPromotions");
			var pointsField = promotions?.GetType().GetField("Points");
			var takePointMethod = promotions?.GetType().GetMethod("TakePoint");
			if (pointsField == null || takePointMethod == null)
				return;

			foreach (var (group, actor) in new[]
			{
				(ArmyLeaderGroup.Blue, "army.leader.blue"),
				(ArmyLeaderGroup.Black, "army.leader.black"),
			})
			{
				var points = (int)pointsField.GetValue(promotions);
				if (points <= 0 || self.World.ActorsWithTrait<ArmyLeader>().Any(candidate =>
					candidate.Actor.Owner == self.Owner && candidate.Trait.Group == group && !candidate.Actor.IsDead))
					continue;
				takePointMethod.Invoke(promotions, new object[] { 1 });
				ArmyLeaderSpawner.SpawnLeader(self, actor);
			}
		}
	}
}
