#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.RA.AI
{
	class StateMachine
	{
		IState currentState;
		IState previousState;

		public void Update(Squad squad)
		{
			currentState.Execute(squad);
		}

		public void ChangeState(Squad squad, IState newState, bool rememberPrevious)
		{
			if (rememberPrevious)
				previousState = currentState;

			if (currentState != null)
				currentState.Exit(squad);

			if (newState != null)
				currentState = newState;

			currentState.Enter(squad);
		}

		public void RevertToPreviousState(Squad squad, bool saveCurrentState)
		{
			ChangeState(squad, previousState, saveCurrentState);
		}
	}

	interface IState
	{
		void Enter(Squad bot);
		void Execute(Squad bot);
		void Exit(Squad bot);
	}
}
