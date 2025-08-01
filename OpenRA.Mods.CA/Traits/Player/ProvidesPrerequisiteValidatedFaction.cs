#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class ProvidesPrerequisiteValidatedFactionInfo : ConditionalTraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("The prerequisite type that this provides. If left empty it defaults to the actor's name.")]
		public readonly string Prerequisite = null;

		[Desc("Only grant this prerequisite when you have these prerequisites.")]
		public readonly string[] RequiresPrerequisites = System.Array.Empty<string>();

		[Desc("Only grant this prerequisite for certain factions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		[Desc("Should it recheck everything when it is captured?")]
		public readonly bool ResetOnOwnerChange = false;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return new string[] { Prerequisite ?? info.Name };
		}

		public override object Create(ActorInitializer init) { return new ProvidesPrerequisiteValidatedFaction(init, this); }
	}

	public class ProvidesPrerequisiteValidatedFaction : ConditionalTrait<ProvidesPrerequisiteValidatedFactionInfo>, ITechTreePrerequisite, INotifyOwnerChanged, INotifyCreated
	{
		readonly string prerequisite;

		bool enabled;
		TechTree techTree;
		string faction;

		public ProvidesPrerequisiteValidatedFaction(ActorInitializer init, ProvidesPrerequisiteValidatedFactionInfo info)
			: base(info)
		{
			prerequisite = info.Prerequisite;

			if (string.IsNullOrEmpty(prerequisite))
				prerequisite = init.Self.Info.Name;

			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (!enabled)
					yield break;

				yield return prerequisite;
			}
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;

			techTree = playerActor.Trait<TechTree>();

			var validFactions = self.TraitOrDefault<ValidFactions>();

			// if ValidFactions trait is present and the current faction is not listed in it, switch the faction to a valid one if a player of that faction exists in the game
			if (
				validFactions != null
				&& Info.Factions.Count > 0
				&& !validFactions.Info.Factions.Contains(faction))
			{
				var capturedFactionManager = playerActor.TraitOrDefault<CapturedFactionsManager>();
				var capturedFactions = capturedFactionManager != null ? capturedFactionManager.Factions : new HashSet<string>();
				var players = self.World.Players
					.Where(p => !p.NonCombatant && p.Playable)
					.OrderByDescending(p => p.Faction.InternalName == playerActor.Owner.Faction.InternalName)
					.ThenByDescending(p => capturedFactions.Contains(p.Faction.InternalName));

				foreach (var p in players)
				{
					if (validFactions.Info.Factions.Contains(p.Faction.InternalName))
					{
						faction = p.Faction.InternalName;
						break;
					}
				}
			}

			Update();

			base.Created(self);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			techTree = newOwner.PlayerActor.Trait<TechTree>();

			if (Info.ResetOnOwnerChange)
				faction = newOwner.Faction.InternalName;

			Update();
		}

		void Update()
		{
			enabled = !IsTraitDisabled;
			if (IsTraitDisabled)
				return;

			if (Info.Factions.Count > 0)
				enabled = Info.Factions.Contains(faction);

			if (Info.RequiresPrerequisites.Length > 0 && enabled)
				enabled = techTree.HasPrerequisites(Info.RequiresPrerequisites);
		}

		protected override void TraitEnabled(Actor self)
		{
			Update();
			techTree.ActorChanged(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			Update();
			techTree.ActorChanged(self);
		}
	}
}
