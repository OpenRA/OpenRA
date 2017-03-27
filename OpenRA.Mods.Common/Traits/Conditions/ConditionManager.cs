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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[RequireExplicitImplementation]
	public interface IConditionTimerWatcher
	{
		string Condition { get; }
		void Update(int duration, int remaining);
	}

	[Desc("Attach this to a unit to enable dynamic conditions by warheads, experience, crates, support powers, etc.")]
	public class ConditionManagerInfo : TraitInfo<ConditionManager>, Requires<IConditionConsumerInfo> { }

	public class ConditionManager : INotifyCreated, ITick
	{
		/// <summary>Value used to represent an invalid token.</summary>
		public static readonly int InvalidConditionToken = -1;

		class ConditionTimer
		{
			public readonly int Token;
			public readonly int Duration;
			public int Remaining;

			public ConditionTimer(int token, int duration)
			{
				Token = token;
				Duration = Remaining = duration;
			}
		}

		class ConditionState
		{
			/// <summary>Traits that have registered to be notified when this condition changes.</summary>
			public readonly List<ConditionConsumer> Consumers = new List<ConditionConsumer>();

			/// <summary>Unique integers identifying granted instances of the condition.</summary>
			public readonly HashSet<int> Tokens = new HashSet<int>();

			/// <summary>External callbacks that are to be executed when a timed condition changes.</summary>
			public readonly List<IConditionTimerWatcher> Watchers = new List<IConditionTimerWatcher>();
		}

		Dictionary<string, ConditionState> state;
		readonly Dictionary<string, List<ConditionTimer>> timers = new Dictionary<string, List<ConditionTimer>>();

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

			var allConsumers = new HashSet<ConditionConsumer>();
			var allWatchers = self.TraitsImplementing<IConditionTimerWatcher>().ToList();

			foreach (var consumerProvider in self.TraitsImplementing<IConditionConsumerProvider>())
			{
				foreach (var consumerWithConditions in consumerProvider.GetConsumersWithTheirConditions())
				{
					allConsumers.Add(consumerWithConditions.First);
					foreach (var condition in consumerWithConditions.Second)
					{
						var cs = state.GetOrAdd(condition);
						cs.Consumers.Add(consumerWithConditions.First);
						foreach (var w in allWatchers)
							if (w.Condition == condition)
								cs.Watchers.Add(w);

						conditionCache[condition] = 0;
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
			foreach (var consumer in allConsumers)
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

			foreach (var t in conditionState.Consumers)
				t(self, readOnlyConditionCache);
		}

		/// <summary>Grants a specified condition.</summary>
		/// <returns>The token that is used to revoke this condition.</returns>
		/// <param name="duration">Automatically revoke condition after this delay if non-zero.</param>
		public int GrantCondition(Actor self, string condition, int duration = 0)
		{
			var token = nextToken++;
			tokens.Add(token, condition);

			if (duration > 0)
				timers.GetOrAdd(condition).Add(new ConditionTimer(token, duration));

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

			// Clean up timers
			List<ConditionTimer> ct;
			if (timers.TryGetValue(condition, out ct))
			{
				ct.RemoveAll(t => t.Token == token);
				if (!ct.Any())
					timers.Remove(condition);
			}

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

		readonly HashSet<int> timersToRemove = new HashSet<int>();
		void ITick.Tick(Actor self)
		{
			// Watchers will be receiving notifications while the condition is enabled.
			// They will also be provided with the number of ticks before the condition is disabled,
			// as well as the duration of the longest active instance.
			foreach (var kv in timers)
			{
				var duration = 0;
				var remaining = 0;
				foreach (var t in kv.Value)
				{
					if (--t.Remaining <= 0)
						timersToRemove.Add(t.Token);

					// Track the duration and remaining time for the longest remaining timer
					if (t.Remaining > remaining)
					{
						duration = t.Duration;
						remaining = t.Remaining;
					}
				}

				foreach (var w in state[kv.Key].Watchers)
					w.Update(duration, remaining);
			}

			foreach (var t in timersToRemove)
				RevokeCondition(self, t);

			timersToRemove.Clear();
		}
	}
}
