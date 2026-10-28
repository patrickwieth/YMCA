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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Grants the mutation condition and records its source player for death-spawn ownership.")]
	public class GeneSpliceWarhead : GrantExternalConditionWarhead
	{
		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (target.Type == TargetType.Invalid)
				return;

			var source = args.SourceActor;
			var owner = source.Owner;
			var actors = target.Type == TargetType.Actor ? new[] { target.Actor } :
				source.World.FindActorsInCircle(target.CenterPosition, Range);
			foreach (var actor in actors)
			{
				if (!IsValidAgainst(actor, source))
					continue;

				var condition = actor.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == Condition && t.CanGrantCondition(source));
				if (condition == null)
					continue;

				var token = condition.GrantCondition(actor, source, Duration);
				if (token == Actor.InvalidConditionToken)
					continue;

				foreach (var spawn in actor.TraitsImplementing<SpawnActorOnDeathFromConditionSource>())
					spawn.RecordSource(actor, Condition, token, owner);
			}
		}
	}
}
