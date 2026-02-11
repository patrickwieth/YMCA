#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Ensures Double MCV start gives nukular MCVs when Nukular mode is enabled. Attach this to the world actor.")]
	public class NukularDoubleStartingUnitsInfo : TraitInfo
	{
		[Desc("Lobby option id for starting units.")]
		public readonly string StartingUnitsOptionId = "startingunits";

		[Desc("Lobby option id for nukular mode.")]
		public readonly string NukularOptionId = "nukular";

		[Desc("Starting units class that should receive double nukular MCVs.")]
		public readonly string StartingUnitsClass = "doubled";

		[ActorReference]
		[Desc("Base MCV actor ids to convert into nukular variants.")]
		public readonly string[] McvActors = { "mcv", "amcv", "smcv", "chnmcv" };

		public override object Create(ActorInitializer init) { return new NukularDoubleStartingUnits(init.Self, this); }
	}

	class NukularDoubleStartingUnits : ITick, INotifyCreated
	{
		readonly NukularDoubleStartingUnitsInfo info;
		readonly HashSet<string> mcvActors;
		bool pending;
		bool enabled;

		public NukularDoubleStartingUnits(Actor self, NukularDoubleStartingUnitsInfo info)
		{
			this.info = info;
			mcvActors = new HashSet<string>(info.McvActors.Select(a => a.ToLowerInvariant()));
		}

		void INotifyCreated.Created(Actor self)
		{
			var settings = self.World.LobbyInfo.GlobalSettings;
			var nukularEnabled = settings.OptionOrDefault(info.NukularOptionId, false);
			var startingUnits = settings.OptionOrDefault(info.StartingUnitsOptionId, info.StartingUnitsClass);

			enabled = nukularEnabled && startingUnits == info.StartingUnitsClass;
			pending = enabled;
		}

		void ITick.Tick(Actor self)
		{
			if (!pending)
				return;

			pending = false;
			var world = self.World;
			var rules = world.Map.Rules.Actors;

			foreach (var actor in world.Actors)
			{
				if (!actor.IsInWorld || actor.Owner == null || !actor.Owner.Playable)
					continue;

				var baseId = actor.Info.Name.ToLowerInvariant();
				if (!mcvActors.Contains(baseId))
					continue;

				var nukularId = baseId + ".nukular";
				if (!rules.ContainsKey(nukularId))
					continue;

				var facing = actor.TraitOrDefault<IFacing>();
				var transform = new Transform(nukularId)
				{
					Faction = actor.Owner.Faction.InternalName,
					SkipMakeAnims = true
				};

				if (facing != null)
					transform.Facing = facing.Facing;

				actor.CancelActivity();
				actor.QueueActivity(transform);
			}
		}
	}
}
