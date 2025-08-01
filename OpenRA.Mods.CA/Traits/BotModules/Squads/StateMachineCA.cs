#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	class StateMachineCA
	{
		IState currentState;
		IState previousState;

		public void Update(SquadCA squad)
		{
			if (currentState != null)
				currentState.Tick(squad);
		}

		public void ChangeState(SquadCA squad, IState newState, bool rememberPrevious)
		{
			if (rememberPrevious)
				previousState = currentState;

			if (currentState != null)
				currentState.Deactivate(squad);

			if (newState != null)
				currentState = newState;

			if (currentState != null)
				currentState.Activate(squad);
		}

		public void RevertToPreviousState(SquadCA squad, bool saveCurrentState)
		{
			ChangeState(squad, previousState, saveCurrentState);
		}
	}

	interface IState
	{
		void Activate(SquadCA bot);
		void Tick(SquadCA bot);
		void Deactivate(SquadCA bot);
	}
}
