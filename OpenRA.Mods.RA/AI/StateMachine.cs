#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
		//a pointer to the agent that owns this instance
		private Squad owner;

		private IState currentState;

		//a record of the last state the agent was in
		private IState previousState;

		public StateMachine(Squad owner)
		{
			this.owner = owner;
		}

		public IState CurrentState
		{
			get { return currentState; }
			set { currentState = value; }
		}

		public IState PreviousState
		{
			get { return previousState; }
			set { previousState = value; }
		}

		//call this to update the FSM
		public void UpdateFsm()
		{
			currentState.Execute(owner);
		}

		//change to a new state
		//boolean variable isSaveCurrentState respons on save or not current state
		public void ChangeState(IState newState, bool saveCurrentState)
		{
			if (saveCurrentState)
				//keep a record of the previous state
				previousState = currentState;

			//call the exit method of the existing state
			if(currentState != null)
				currentState.Exit(owner);

			//change state to the new state
			if (newState != null)
				currentState = newState;

			//call the entry method of the new state
			currentState.Enter(owner);
		}

		//change state back to the previous state
		public void RevertToPreviousState(bool saveCurrentState)
		{
			ChangeState(previousState, saveCurrentState);
		}
	}

	interface IState
	{
		void Enter(Squad bot);
		void Execute(Squad bot);
		void Exit(Squad bot);
	}
}
