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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Lists valid factions for ProvidesPrerequisiteValidatedFaction.")]
	public class ValidFactionsInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Valid factions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new ValidFactions(init, this); }
	}

	public class ValidFactions
	{
		public readonly ValidFactionsInfo Info;

		public ValidFactions(ActorInitializer init, ValidFactionsInfo info)
		{
			Info = info;
		}
	}
}
