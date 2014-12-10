#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("Attach this to a unit to enable dynamic conditions by warheads, experience, crates, support powers, etc.")]
	public class ConditionManagerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new ConditionManager(init); }
	}

	public class ConditionManager : ITick
	{
		class TimedCondition
		{
			public readonly string Condition;
			public readonly int Duration;
			public int Remaining;

			public TimedCondition(string condition, int duration)
			{
				Condition = condition;
				Duration = duration;
				Remaining = duration;
			}

			public void Tick() { Remaining--; }
		}

		class ConditionState
		{
			public readonly List<IConditional> Traits = new List<IConditional>();
			public readonly List<object> Sources = new List<object>();
			public readonly List<Action<int, int>> Watchers = new List<Action<int, int>>();
		}

		readonly List<TimedCondition> timedConditions = new List<TimedCondition>();
		readonly Lazy<Dictionary<string, ConditionState>> conditions;
		readonly Dictionary<IConditional, int> levels = new Dictionary<IConditional, int>();

		public ConditionManager(ActorInitializer init)
		{
			conditions = Exts.Lazy(() =>
			{
				var ret = new Dictionary<string, ConditionState>();
				foreach (var up in init.self.TraitsImplementing<IConditional>())
					foreach (var t in up.ConditionTypes)
						ret.GetOrAdd(t).Traits.Add(up);

				return ret;
			});
		}

		public void GrantTimedCondition(Actor self, string condition, int duration)
		{
			var timed = timedConditions.FirstOrDefault(u => u.Condition == condition);
			if (timed == null)
			{
				timed = new TimedCondition(condition, duration);
				timedConditions.Add(timed);
				GrantCondition(self, condition, timed);
			}
			else
				timed.Remaining = Math.Max(duration, timed.Remaining);
		}

		// Different conditional traits may define (a) different level ranges for the same condition type,
		// and (b) multiple condition types for the same trait. The unrestricted level for each trait is
		// tracked independently so that we can can correctly revoke levels without adding the burden of
		// tracking both the overall (unclamped) and effective (clamped) levels on each individual trait.
		void NotifyConditionLevelChanged(IEnumerable<IConditional> traits, Actor self, string condition, int levelAdjust)
		{
			foreach (var up in traits)
			{
				var oldLevel = levels.GetOrAdd(up);
				var newLevel = levels[up] = oldLevel + levelAdjust;

				// This will internally clamp the levels to its own restricted range
				up.ConditionLevelChanged(self, condition, oldLevel, newLevel);
			}
		}

		int GetOverallLevel(IConditional conditional)
		{
			int level;
			return levels.TryGetValue(conditional, out level) ? level : 0;
		}

		public void GrantCondition(Actor self, string condition, object source)
		{
			ConditionState s;
			if (!conditions.Value.TryGetValue(condition, out s))
				return;

			// Track the condition source so that the condition level can be removed without conflicts
			s.Sources.Add(source);

			NotifyConditionLevelChanged(s.Traits, self, condition, 1);
		}

		public void RevokeCondition(Actor self, string condition, object source)
		{
			ConditionState s;
			if (!conditions.Value.TryGetValue(condition, out s))
				return;

			if (!s.Sources.Remove(source))
				throw new InvalidOperationException("Object <{0}> revoked more levels of condition {1} than it granted for {2}.".F(source, condition, self));

			NotifyConditionLevelChanged(s.Traits, self, condition, -1);
		}

		public bool AcceptsConditionType(Actor self, string condition)
		{
			ConditionState s;
			if (!conditions.Value.TryGetValue(condition, out s))
				return false;

			return s.Traits.Any(up => up.AcceptsConditionLevel(self, condition, GetOverallLevel(up) + 1));
		}

		public void RegisterWatcher(string condition, Action<int, int> action)
		{
			ConditionState s;
			if (!conditions.Value.TryGetValue(condition, out s))
				return;

			s.Watchers.Add(action);
		}

		public void Tick(Actor self)
		{
			foreach (var u in timedConditions)
			{
				u.Tick();
				if (u.Remaining <= 0)
					RevokeCondition(self, u.Condition, u);

				foreach (var a in conditions.Value[u.Condition].Watchers)
					a(u.Duration, u.Remaining);
			}

			timedConditions.RemoveAll(u => u.Remaining <= 0);
		}
	}
}
