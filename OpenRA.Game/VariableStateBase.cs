#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA
{
	interface IAcceptsTokens<TokenValueType>
	{
		void AddOrUpdateToken(Actor self, int token);
		void AddOrUpdateToken(Actor self, int token, TokenValueType tokenValue);
		void RemoveToken(Actor self, int token);
	}

	public abstract class VariableStateBase
	{
		/// <summary>Delegates that have registered to be notified when this condition changes.</summary>
		private readonly List<VariableObserverNotifier> notifiers = new List<VariableObserverNotifier>();

		public IEnumerable<VariableObserverNotifier> Notifiers { get { return notifiers; } }

		public void AddNotifier(VariableObserverNotifier notifier)
		{
			notifiers.Add(notifier);
		}

		public void NotifyAll(Actor self, IReadOnlyDictionary<string, int> variables)
		{
			foreach (var notify in notifiers)
				notify(self, variables);
		}
	}

	public abstract class IntVariableStateBase : VariableStateBase
	{
		protected readonly string variable;

		protected IntVariableStateBase(string variable)
		{
			this.variable = variable;
		}

		protected int GetValue(Actor self)
		{
			// TODO: replace with initialization of all variables
			int currentValue;
			if (self.VariableCache.TryGetValue(variable, out currentValue))
				return currentValue;

			currentValue = Init();
			self.VariableCache[variable] = currentValue;
			return currentValue;
		}

		protected void SetValue(Actor self, int value)
		{
			self.VariableCache[variable] = value;

			// Conditions may be granted or revoked before the state is initialized.
			// These notifications will be processed after INotifyCreated.Created.
			if (self.Created)
				NotifyAll(self, self.ReadOnlyVariableCache);
		}

		public virtual int Init() { return default(int); }
	}
}
