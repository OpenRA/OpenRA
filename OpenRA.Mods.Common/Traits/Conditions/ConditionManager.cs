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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[RequireExplicitImplementation]
	public interface IConditionTimerWatcher
	{
		string Condition { get; }
		void Update(int duration, int remaining);
	}

	[Flags] public enum ConditionProgressProperties { Duration = 1, Remaining = 2, Progress = 4 }

	[Desc("Attach this to a unit to enable dynamic conditions by warheads, experience, crates, support powers, etc.")]
	public class ConditionManagerInfo : ITraitInfo, Requires<IConditionConsumerInfo>
	{
		[Desc("The key is the timer condition name and the value is one or more of the following property names: duration, remaining, or progress.")]
		public readonly Dictionary<string, ConditionProgressProperties> Timers = new Dictionary<string, ConditionProgressProperties>();

		[ConsumedConditionReference]
		public IEnumerable<string> ConsumedTimerConditions { get { return Timers.Keys; } }

		[GrantedConditionReference]
		public IEnumerable<string> GrantedTimerConditionProperties
		{
			get
			{
				foreach (var timer in Timers)
				{
					if (timer.Value.HasFlag(ConditionProgressProperties.Duration))
						yield return timer.Key + ".duration";
					if (timer.Value.HasFlag(ConditionProgressProperties.Remaining))
						yield return timer.Key + ".remaining";
					if (timer.Value.HasFlag(ConditionProgressProperties.Progress))
						yield return timer.Key + ".progress";
				}
			}
		}

		public object Create(ActorInitializer init) { return new ConditionManager(this); }
	}

	public class ConditionManager : INotifyCreated, ITick
	{
		/// <summary>Value used to represent an invalid token.</summary>
		public static readonly int InvalidConditionToken = -1;

		class ConditionTimer
		{
			class TokenTimer
			{
				public static readonly TokenTimer Zero = new TokenTimer(InvalidConditionToken, 0);

				public readonly int Token;
				public readonly int Duration;
				public int Remaining;

				public TokenTimer(int token, int duration)
				{
					Token = token;
					Duration = Remaining = duration;
				}
			}

			/// <summary>External callbacks that are to be executed when a timed condition changes.</summary>
			readonly List<IConditionTimerWatcher> watchers = new List<IConditionTimerWatcher>();

			readonly int durationToken = InvalidConditionToken;
			readonly int remainingToken = InvalidConditionToken;
			readonly int progressToken = InvalidConditionToken;
			readonly List<TokenTimer> timers = new List<TokenTimer>();
			TokenTimer longestTimer = TokenTimer.Zero;
			public int Count { get { return timers.Count; } }

			public ConditionTimer(Actor self, ConditionManager manager, string condition)
			{
				ConditionProgressProperties properties;
				manager.Info.Timers.TryGetValue(condition, out properties);
				if (properties.HasFlag(ConditionProgressProperties.Duration))
					durationToken = manager.GrantCondition(self, condition + ".duration", 0);

				if (properties.HasFlag(ConditionProgressProperties.Remaining))
					remainingToken = manager.GrantCondition(self, condition + ".remaining", 0);

				if (properties.HasFlag(ConditionProgressProperties.Progress))
					progressToken = manager.GrantCondition(self, condition + ".progress", 0);
			}

			public void Remove(int token, Actor self, ConditionManager manager)
			{
				if (!RemoveByIndex(timers.FindIndex(t => t.Token == token), self, manager))
					return;

				NewLongestTimer(self, manager);
				if (timers.Count == 0)
					manager.timers.Remove(this);
			}

			public void Add(IConditionTimerWatcher watcher) { watchers.Add(watcher); }
			public void Add(int token, int duration, Actor self, ConditionManager manager)
			{
				timers.Add(new TokenTimer(token, duration));
				NewLongestTimer(self, manager);
				if (timers.Count == 1)
					manager.timers.Add(this);
			}

			public void Tick(Actor self, ConditionManager manager)
			{
				if (timers.Count == 0)
					return;

				var expiredTimerIndexes = new BitArray(timers.Count);
				for (var i = 0; i < timers.Count; i++)
					if (--timers[i].Remaining <= 0)
						expiredTimerIndexes.Set(i, true);

				for (var i = timers.Count - 1; i >= 0; i--)
					if (expiredTimerIndexes.Get(i))
						RemoveByIndex(i, self, manager);

				if (!NewLongestTimer(self, manager))
				{
					if (remainingToken != InvalidConditionToken)
						manager.UpdateConditionValue(self, remainingToken, longestTimer.Remaining);

					if (progressToken != InvalidConditionToken)
						manager.UpdateConditionValue(self, progressToken, longestTimer.Duration - longestTimer.Remaining);
				}

				foreach (var w in watchers)
					w.Update(longestTimer.Duration, longestTimer.Remaining);
			}

			bool RemoveByIndex(int index, Actor self, ConditionManager manager)
			{
				if (index < 0)
					return false;

				TokenTimer removedTimer = timers[index];
				timers.RemoveAt(index);
				manager.RevokeCondition(self, removedTimer.Token);
				if (longestTimer != removedTimer)
					return false;

				longestTimer = TokenTimer.Zero;
				return true;
			}

			bool NewLongestTimer(Actor self, ConditionManager manager)
			{
				var newLongestTimer = longestTimer;
				foreach (var timer in timers)
					if (timer.Remaining > newLongestTimer.Remaining)
						newLongestTimer = timer;

				if (newLongestTimer == longestTimer)
					return false;

				longestTimer = newLongestTimer;
				if (durationToken != InvalidConditionToken)
					manager.UpdateConditionValue(self, durationToken, longestTimer.Duration);

				if (remainingToken != InvalidConditionToken)
					manager.UpdateConditionValue(self, remainingToken, longestTimer.Remaining);

				if (progressToken != InvalidConditionToken)
					manager.UpdateConditionValue(self, progressToken, longestTimer.Duration - longestTimer.Remaining);

				return true;
			}
		}

		class ConditionState
		{
			/// <summary>Traits that have registered to be notified when this condition changes.</summary>
			public readonly List<ConditionConsumer> Consumers = new List<ConditionConsumer>();

			public ConditionTimer Timers = null;

			/// <summary>Unique integers identifying granted instances of the condition.</summary>
			readonly HashSet<int> tokens = new HashSet<int>();

			public void Add(int token) { tokens.Add(token); }
			public ConditionTimer GetTimer(Actor self, ConditionManager manager, string condition)
			{
				if (Timers != null)
					return Timers;

				Timers = new ConditionTimer(self, manager, condition);
				return Timers;
			}

			public void Add(int token, int duration, Actor self, ConditionManager manager, string condition)
			{
				GetTimer(self, manager, condition).Add(token, duration, self, manager);
			}

			public void Remove(int token, Actor self, ConditionManager manager)
			{
				if (tokens.Remove(token) && Timers != null)
					Timers.Remove(token, self, manager);
			}
		}

		class TokenState
		{
			public readonly string Condition;
			public int Value;

			public TokenState(string condition, int value)
			{
				Condition = condition;
				Value = value;
			}
		}

		enum UpdateType { Grant, Change, Revoke }

		public readonly ConditionManagerInfo Info;
		Dictionary<string, ConditionState> state;
		readonly HashSet<ConditionTimer> timers = new HashSet<ConditionTimer>();
		readonly Dictionary<string, ConditionTimer> pendingTimers = new Dictionary<string, ConditionTimer>();

		/// <summary>Each granted condition receives a unique token that is used when revoking and has an integer value.</summary>
		Dictionary<int, TokenState> tokens = new Dictionary<int, TokenState>();

		int nextToken = 1;

		/// <summary>Cache of condition -> enabled state for quick evaluation of token value sum conditions.</summary>
		readonly Dictionary<string, int> conditionCache = new Dictionary<string, int>();

		/// <summary>Read-only version of conditionCache that is passed to IConditionConsumers.</summary>
		IReadOnlyDictionary<string, int> readOnlyConditionCache;

		public ConditionManager(ConditionManagerInfo info) { Info = info; }

		void INotifyCreated.Created(Actor self)
		{
			state = new Dictionary<string, ConditionState>();
			readOnlyConditionCache = new ReadOnlyDictionary<string, int>(conditionCache);

			var allConsumers = new HashSet<ConditionConsumer>();
			var allWatchers = self.TraitsImplementing<IConditionTimerWatcher>().ToList();

			foreach (var conditionTimersPair in pendingTimers)
				state.GetOrAdd(conditionTimersPair.Key).Timers = conditionTimersPair.Value;

			foreach (var consumerProvider in self.TraitsImplementing<IConditionConsumerProvider>())
			{
				foreach (var consumerWithConditions in consumerProvider.GetConsumersWithTheirConditions())
				{
					allConsumers.Add(consumerWithConditions.First);
					foreach (var condition in consumerWithConditions.Second)
					{
						var cs = state.GetOrAdd(condition);
						cs.Consumers.Add(consumerWithConditions.First);
						if (allWatchers.Count > 0)
						{
							var ts = cs.GetTimer(self, this, condition);
							foreach (var w in allWatchers)
								if (w.Condition == condition)
									ts.Add(w);
						}

						conditionCache[condition] = 0;
					}
				}
			}

			// Enable any conditions granted during trait setup
			foreach (var kv in tokens)
			{
				ConditionState cs;
				if (!state.TryGetValue(kv.Value.Condition, out cs))
					continue;

				cs.Add(kv.Key);
				if (conditionCache.ContainsKey(kv.Value.Condition))
					conditionCache[kv.Value.Condition] += kv.Value.Value;
				else
					conditionCache.Add(kv.Value.Condition, kv.Value.Value);
			}

			// Update all traits with their initial condition state
			foreach (var consumer in allConsumers)
				consumer(self, readOnlyConditionCache);
		}

		void UpdateConditionState(Actor self, string condition, int token, UpdateType type, int delta)
		{
			// Conditions may be granted before the state is initialized.
			// These conditions will be processed in INotifyCreated.Created.
			if (state == null)
				return;

			ConditionState cs;
			if (!state.TryGetValue(condition, out cs))
				return;

			switch (type)
			{
				case UpdateType.Grant:
					cs.Add(token);
					break;

				case UpdateType.Revoke:
					cs.Remove(token, self, this);
					break;
			}

			if (delta == 0)
				return;

			conditionCache[condition] += delta;

			foreach (var t in cs.Consumers)
				t(self, readOnlyConditionCache);
		}

		/// <summary>Grants a specified condition token.</summary>
		/// <returns>The token that is used to revoke this condition.</returns>
		/// <param name="value">Integer value for condition token condition.</param>
		public int GrantCondition(Actor self, string condition, int value = 1)
		{
			var token = nextToken++;
			tokens.Add(token, new TokenState(condition, value));
			UpdateConditionState(self, condition, token, UpdateType.Grant, value);
			return token;
		}

		/// <summary>Grants a timed token for a specified condition.</summary>
		/// <returns>The token that is used to revoke this condition.</returns>
		/// <param name="duration">Automatically revoke condition after this delay if non-zero.</param>
		/// <param name="value">Integer value for condition token condition.</param>
		public int GrantTimedCondition(Actor self, string condition, int duration, int value = 1)
		{
			var token = GrantCondition(self, condition, value);
			if (duration > 0)
			{
				if (state == null)
					pendingTimers.GetOrAdd(condition, c => new ConditionTimer(self, this, condition)).Add(token, duration, self, this);
				else
					state[condition].Add(token, duration, self, this, condition);
			}

			return token;
		}

		/// <summary>Re-values a specified condition token.</summary>
		/// <returns>The token that is used to revoke this condition.</returns>
		/// <param name="value">Integer value for condition token condition.</param>
		public int UpdateConditionValue(Actor self, int token, int value)
		{
			TokenState tokenState;
			if (!tokens.TryGetValue(token, out tokenState))
				throw new InvalidOperationException("Attempting to re-value condition with invalid token {0} for {1}.".F(token, self));

			if (value == tokenState.Value)
				return token;

			UpdateConditionState(self, tokenState.Condition, token, UpdateType.Change, value - tokenState.Value);
			tokenState.Value = value;
			return token;
		}

		/// <summary>Revokes a previously granted condition token.</summary>
		/// <returns>The invalid token ID.</returns>
		/// <param name="token">The token ID returned by GrantCondition.</param>
		public int RevokeCondition(Actor self, int token)
		{
			TokenState tokenState;
			if (!tokens.TryGetValue(token, out tokenState))
				throw new InvalidOperationException("Attempting to revoke condition with invalid token {0} for {1}.".F(token, self));

			tokens.Remove(token);
			UpdateConditionState(self, tokenState.Condition, token, UpdateType.Revoke, -tokenState.Value);
			return InvalidConditionToken;
		}

		/// <summary>Returns whether the specified token is valid for RevokeCondition</summary>
		public bool TokenValid(Actor self, int token)
		{
			return tokens.ContainsKey(token);
		}

		void ITick.Tick(Actor self)
		{
			// Watchers will be receiving notifications while the condition is enabled.
			// They will also be provided with the number of ticks before the condition is disabled,
			// as well as the duration of the longest active instance.
			foreach (var timer in timers)
				timer.Tick(self, this);
		}
	}
}
