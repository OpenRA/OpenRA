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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to a unit to enable dynamic upgrades by warheads, experience, crates, support powers, etc.")]
	public class UpgradeManagerInfo : TraitInfo<UpgradeManager>, Requires<IConditionConsumerInfo> { }

	public class UpgradeManager : INotifyCreated, ITick
	{
		/// <summary>Value used to represent an invalid token.</summary>
		public static readonly int InvalidConditionToken = -1;
		string[] externalConditions = { };

		class TimedCondition
		{
			public class ConditionSource
			{
				public readonly object Source;
				public int Remaining;

				public ConditionSource(int duration, object source)
				{
					Remaining = duration;
					Source = source;
				}
			}

			public readonly string Condition;
			public readonly int Duration;
			public readonly HashSet<ConditionSource> Sources;
			public int Remaining; // Equal to maximum of all Sources.Remaining

			public TimedCondition(string condition, int duration, object source)
			{
				Condition = condition;
				Duration = duration;
				Remaining = duration;
				Sources = new HashSet<ConditionSource> { new ConditionSource(duration, source) };
			}

			public void Tick()
			{
				Remaining--;
				foreach (var source in Sources)
					source.Remaining--;
			}
		}

		class ConditionState
		{
			/// <summary>Traits that have registered to be notified when this condition changes.</summary>
			public readonly List<IConditionConsumer> Consumers = new List<IConditionConsumer>();

			/// <summary>Unique integers identifying granted instances of the condition.</summary>
			public readonly HashSet<int> Tokens = new HashSet<int>();

			/// <summary>External callbacks that are to be executed when a timed condition changes.</summary>
			public readonly List<Action<int, int>> Watchers = new List<Action<int, int>>();
		}

		readonly List<TimedCondition> timedConditions = new List<TimedCondition>();

		Dictionary<string, ConditionState> state;

		/// <summary>Each granted condition receives a unique token that is used when revoking.</summary>
		Dictionary<int, string> tokens = new Dictionary<int, string>();

		int nextToken = 1;

		/// <summary>Temporary shim between the old and new upgrade/condition grant and revoke methods.</summary>
		Dictionary<Pair<object, string>, int> objectTokenShim = new Dictionary<Pair<object, string>, int>();

		/// <summary>Cache of condition -> enabled state for quick evaluation of boolean conditions.</summary>
		Dictionary<string, bool> conditionCache = new Dictionary<string, bool>();

		/// <summary>Read-only version of conditionCache that is passed to IConditionConsumers.</summary>
		IReadOnlyDictionary<string, bool> readOnlyConditionCache;

		void INotifyCreated.Created(Actor self)
		{
			state = new Dictionary<string, ConditionState>();
			readOnlyConditionCache = new ReadOnlyDictionary<string, bool>(conditionCache);

			var allConsumers = new HashSet<IConditionConsumer>();
			foreach (var consumer in self.TraitsImplementing<IConditionConsumer>())
			{
				allConsumers.Add(consumer);
				foreach (var condition in consumer.Conditions)
				{
					state.GetOrAdd(condition).Consumers.Add(consumer);
					conditionCache[condition] = false;
				}
			}

			// Enable any conditions granted during trait setup
			foreach (var kv in tokens)
			{
				ConditionState conditionState;
				if (!state.TryGetValue(kv.Value, out conditionState))
					continue;

				conditionState.Tokens.Add(kv.Key);
				conditionCache[kv.Value] = conditionState.Tokens.Count > 0;
			}

			// Update all traits with their initial condition state
			foreach (var consumer in allConsumers)
				consumer.ConditionsChanged(self, readOnlyConditionCache);

			// Build external condition whitelist
			externalConditions = self.Info.TraitInfos<ExternalConditionsInfo>()
				.SelectMany(t => t.Conditions)
				.Distinct()
				.ToArray();
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

			conditionCache[condition] = conditionState.Tokens.Count > 0;

			foreach (var t in conditionState.Consumers)
				t.ConditionsChanged(self, readOnlyConditionCache);
		}

		/// <summary>Grants a specified condition.</summary>
		/// <returns>The token that is used to revoke this condition.</returns>
		/// <param name="external">Validate against the external condition whitelist.</param>
		public int GrantCondition(Actor self, string condition, bool external = false)
		{
			if (external && !externalConditions.Contains(condition))
				return InvalidConditionToken;

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

		/// <summary>Returns true if the given external condition will have an effect on this actor.</summary>
		public bool AcceptsExternalCondition(Actor self, string condition)
		{
			if (state == null)
				throw new InvalidOperationException("AcceptsExternalCondition cannot be queried before the actor has been fully created.");

			return externalConditions.Contains(condition) && !conditionCache[condition];
		}

		#region Shim methods for legacy upgrade granting code

		void CheckCanManageConditions()
		{
			if (state == null)
				throw new InvalidOperationException("Conditions cannot be managed until the actor has been fully created.");
		}

		/// <summary>Upgrade level increments are limited to dupesAllowed per source, i.e., if a single
		/// source attempts granting more upgrades than dupesAllowed, they will not accumulate. They will
		/// replace each other instead, leaving only the most recently granted upgrade active. Each new
		/// upgrade granting request will increment the upgrade's level until AcceptsUpgrade starts
		/// returning false. Then, when no new levels are accepted, the upgrade source with the shortest
		/// remaining upgrade duration will be replaced by the new source.</summary>
		public void GrantTimedUpgrade(Actor self, string upgrade, int duration, object source = null, int dupesAllowed = 1)
		{
			var timed = timedConditions.FirstOrDefault(u => u.Condition == upgrade);
			if (timed == null)
			{
				timed = new TimedCondition(upgrade, duration, source);
				timedConditions.Add(timed);
				GrantUpgrade(self, upgrade, timed);
				return;
			}

			var srcs = timed.Sources.Where(s => s.Source == source);
			if (srcs.Count() < dupesAllowed)
			{
				timed.Sources.Add(new TimedCondition.ConditionSource(duration, source));
				if (AcceptsUpgrade(self, upgrade))
					GrantUpgrade(self, upgrade, timed);
				else
					timed.Sources.Remove(timed.Sources.MinBy(s => s.Remaining));
			}
			else
				srcs.MinBy(s => s.Remaining).Remaining = duration;

			timed.Remaining = Math.Max(duration, timed.Remaining);
		}

		public void GrantUpgrade(Actor self, string upgrade, object source)
		{
			CheckCanManageConditions();
			objectTokenShim[Pair.New(source, upgrade)] = GrantCondition(self, upgrade);
		}

		public void RevokeUpgrade(Actor self, string upgrade, object source)
		{
			CheckCanManageConditions();
			RevokeCondition(self, objectTokenShim[Pair.New(source, upgrade)]);
		}

		/// <summary>Returns true if the actor uses the given upgrade. Does not check the actual level of the upgrade.</summary>
		public bool AcknowledgesUpgrade(Actor self, string upgrade)
		{
			CheckCanManageConditions();
			return state.ContainsKey(upgrade);
		}

		/// <summary>Returns true only if the actor can accept another level of the upgrade.</summary>
		public bool AcceptsUpgrade(Actor self, string upgrade)
		{
			CheckCanManageConditions();
			bool enabled;
			if (!conditionCache.TryGetValue(upgrade, out enabled))
				return false;

			return !enabled;
		}

		public void RegisterWatcher(string upgrade, Action<int, int> action)
		{
			CheckCanManageConditions();

			ConditionState s;
			if (!state.TryGetValue(upgrade, out s))
				return;

			s.Watchers.Add(action);
		}

		/// <summary>Watchers will be receiving notifications while the condition is enabled.
		/// They will also be provided with the number of ticks before the condition is disabled,
		/// as well as the duration in ticks of the timed upgrade (provided in the first call to
		/// GrantTimedUpgrade).</summary>
		void ITick.Tick(Actor self)
		{
			foreach (var u in timedConditions)
			{
				u.Tick();
				foreach (var source in u.Sources)
					if (source.Remaining <= 0)
						RevokeUpgrade(self, u.Condition, u);

				u.Sources.RemoveWhere(source => source.Remaining <= 0);

				foreach (var a in state[u.Condition].Watchers)
					a(u.Duration, u.Remaining);
			}

			timedConditions.RemoveAll(u => u.Remaining <= 0);
		}

		#endregion
	}
}
