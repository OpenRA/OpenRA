#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.AI
{
	class StateMachine
	{
		IState currentState;
		IState previousState;

		public void Update(Squad squad)
		{
			if (currentState != null)
				currentState.Tick(squad);
		}

		public void ChangeState(Squad squad, IState newState, bool rememberPrevious)
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

		public void RevertToPreviousState(Squad squad, bool saveCurrentState)
		{
			ChangeState(squad, previousState, saveCurrentState);
		}
	}

	interface IState
	{
		void Activate(Squad bot);
		void Tick(Squad bot);
		void Deactivate(Squad bot);
	}
}
