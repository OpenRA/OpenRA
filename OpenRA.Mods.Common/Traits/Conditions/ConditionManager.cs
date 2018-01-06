#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to a unit to enable dynamic conditions by warheads, experience, crates, support powers, etc.")]
	public class ConditionManagerInfo : TraitInfo<ConditionManager>, Requires<IObservesVariablesInfo> { }

	public class ConditionManager : INotifyCreated
	{
		/// <summary>Value used to represent an invalid token.</summary>
		public static readonly int InvalidConditionToken = -1;

		class ConditionState
		{
			/// <summary>Delegates that have registered to be notified when this condition changes.</summary>
			public readonly List<VariableObserverNotifier> Notifiers = new List<VariableObserverNotifier>();

			/// <summary>Unique integers identifying granted instances of the condition.</summary>
			public readonly HashSet<int> Tokens = new HashSet<int>();
		}

		Dictionary<string, ConditionState> state;

		/// <summary>Each granted condition receives a unique token that is used when revoking.</summary>
		Dictionary<int, string> tokens = new Dictionary<int, string>();

		int nextToken = 1;

		/// <summary>Cache of condition -> enabled state for quick evaluation of token counter conditions.</summary>
		readonly Dictionary<string, int> conditionCache = new Dictionary<string, int>();

		/// <summary>Read-only version of conditionCache that is passed to IConditionConsumers.</summary>
		IReadOnlyDictionary<string, int> readOnlyConditionCache;

		void INotifyCreated.Created(Actor self)
		{
			state = new Dictionary<string, ConditionState>();
			readOnlyConditionCache = new ReadOnlyDictionary<string, int>(conditionCache);

			var allObservers = new HashSet<VariableObserverNotifier>();

			foreach (var provider in self.TraitsImplementing<IObservesVariables>())
			{
				foreach (var variableUser in provider.GetVariableObservers())
				{
					allObservers.Add(variableUser.Notifier);
					foreach (var variable in variableUser.Variables)
					{
						var cs = state.GetOrAdd(variable);
						cs.Notifiers.Add(variableUser.Notifier);
						conditionCache[variable] = 0;
					}
				}
			}

			// Enable any conditions granted during trait setup
			foreach (var kv in tokens)
			{
				ConditionState conditionState;
				if (!state.TryGetValue(kv.Value, out conditionState))
					continue;

				conditionState.Tokens.Add(kv.Key);
				conditionCache[kv.Value] = conditionState.Tokens.Count;
			}

			// Update all traits with their initial condition state
			foreach (var consumer in allObservers)
				consumer(self, readOnlyConditionCache);
		}

		void UpdateConditionState(Actor self, string condition, int token, bool isRevoke)
		{
			ConditionState conditionState;
			if (!state.TryGetValue(condition, out conditionState))
				return;

			if (isRevoke)
				conditionState.Tokens.Remove(token);
			else
				conditionState.Tokens.Add(token);

			conditionCache[condition] = conditionState.Tokens.Count;

			foreach (var notify in conditionState.Notifiers)
				notify(self, readOnlyConditionCache);
		}

		/// <summary>Grants a specified condition.</summary>
		/// <returns>The token that is used to revoke this condition.</returns>
		public int GrantCondition(Actor self, string condition)
		{
			var token = nextToken++;
			tokens.Add(token, condition);

			// Conditions may be granted before the state is initialized.
			// These conditions will be processed in INotifyCreated.Created.
			if (state != null)
				UpdateConditionState(self, condition, token, false);

			return token;
		}

		/// <summary>Revokes a previously granted condition.</summary>
		/// <returns>The invalid token ID.</returns>
		/// <param name="token">The token ID returned by GrantCondition.</param>
		public int RevokeCondition(Actor self, int token)
		{
			string condition;
			if (!tokens.TryGetValue(token, out condition))
				throw new InvalidOperationException("Attempting to revoke condition with invalid token {0} for {1}.".F(token, self));

			tokens.Remove(token);

			// Conditions may be granted and revoked before the state is initialized.
			if (state != null)
				UpdateConditionState(self, condition, token, true);

			return InvalidConditionToken;
		}

		/// <summary>Returns whether the specified token is valid for RevokeCondition</summary>
		public bool TokenValid(Actor self, int token)
		{
			return tokens.ContainsKey(token);
		}
	}
}
