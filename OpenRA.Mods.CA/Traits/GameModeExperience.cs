using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Periodically grants Commander experience to every member of the team controlling this checkpoint.")]
	public class CheckpointExperienceInfo : TraitInfo
	{
		[Desc("Commander experience granted to each allied player per interval.")]
		public readonly int Amount = 5;

		[Desc("Number of game ticks between experience grants.")]
		public readonly int Interval = 150;

		public override object Create(ActorInitializer init)
		{
			return new CheckpointExperience(init.Self, this);
		}
	}

	public class CheckpointExperience : ITick
	{
		readonly Actor self;
		readonly CheckpointExperienceInfo info;
		int ticks;

		public CheckpointExperience(Actor self, CheckpointExperienceInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void ITick.Tick(Actor actor)
		{
			if (++ticks < info.Interval)
				return;
			ticks = 0;

			var checkpoint = self.Trait<Checkpoint>();
			if (!checkpoint.Captured || self.Owner.NonCombatant)
				return;

			foreach (var player in self.World.Players.Where(player => player.Playable && !player.NonCombatant &&
				player.WinState == WinState.Undefined && player.RelationshipWith(self.Owner) == PlayerRelationship.Ally))
			{
				// PlayerPromotions lives in OpenRA.Mods.Cameo, which is a runtime sibling
				// rather than a compile-time dependency of this assembly.
				var promotions = player.PlayerActor.TraitsImplementing<object>()
					.FirstOrDefault(trait => trait.GetType().Name == "PlayerPromotions");
				promotions?.GetType().GetMethod("GiveExperienceIgnoringBlock")
					?.Invoke(promotions, new object[] { info.Amount });
			}
		}
	}
}
