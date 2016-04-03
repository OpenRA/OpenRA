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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to a unit to enable dynamic upgrades by warheads, experience, crates, support powers, etc.")]
	public class UpgradeManagerInfo : TraitInfo<UpgradeManager>, IRulesetLoaded
	{
		public void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (!info.Name.StartsWith("^") && !info.TraitInfos<IUpgradableInfo>().Any())
				throw new YamlException(
					"There are no upgrades to be managed for actor '{0}'. You are either missing some upgradeable traits, or this UpgradeManager trait is not required.".F(
						info.Name));
		}
	}

	public class UpgradeManager : INotifyCreated, ITick
	{
		class TimedUpgrade
		{
			public class UpgradeSource
			{
				public readonly object Source;
				public int Remaining;

				public UpgradeSource(int duration, object source)
				{
					Remaining = duration;
					Source = source;
				}
			}

			public readonly string Upgrade;
			public readonly int Duration;
			public readonly HashSet<UpgradeSource> Sources;
			public int Remaining; // Equal to maximum of all Sources.Remaining

			public TimedUpgrade(string upgrade, int duration, object source)
			{
				Upgrade = upgrade;
				Duration = duration;
				Remaining = duration;
				Sources = new HashSet<UpgradeSource> { new UpgradeSource(duration, source) };
			}

			public void Tick()
			{
				Remaining--;
				foreach (var source in Sources)
					source.Remaining--;
			}
		}

		class UpgradeState
		{
			public readonly List<IUpgradable> Traits = new List<IUpgradable>();
			public readonly List<object> Sources = new List<object>();
			public readonly List<Action<int, int>> Watchers = new List<Action<int, int>>();
		}

		readonly List<TimedUpgrade> timedUpgrades = new List<TimedUpgrade>();
		readonly Dictionary<IUpgradable, int> levels = new Dictionary<IUpgradable, int>();
		Dictionary<string, UpgradeState> upgrades;

		void INotifyCreated.Created(Actor self)
		{
			upgrades = new Dictionary<string, UpgradeState>();
			foreach (var up in self.TraitsImplementing<IUpgradable>())
				foreach (var t in up.UpgradeTypes)
					upgrades.GetOrAdd(t).Traits.Add(up);
		}

		void CheckCanManageUpgrades()
		{
			if (upgrades == null)
				throw new InvalidOperationException("Upgrades cannot be managed until the actor has been fully created.");
		}

		/// <summary>Upgrade level increments are limited to dupesAllowed per source, i.e., if a single
		/// source attempts granting more upgrades than dupesAllowed, they will not accumulate. They will
		/// replace each other instead, leaving only the most recently granted upgrade active. Each new
		/// upgrade granting request will increment the upgrade's level until AcceptsUpgrade starts
		/// returning false. Then, when no new levels are accepted, the upgrade source with the shortest
		/// remaining upgrade duration will be replaced by the new source.</summary>
		public void GrantTimedUpgrade(Actor self, string upgrade, int duration, object source = null, int dupesAllowed = 1)
		{
			var timed = timedUpgrades.FirstOrDefault(u => u.Upgrade == upgrade);
			if (timed == null)
			{
				timed = new TimedUpgrade(upgrade, duration, source);
				timedUpgrades.Add(timed);
				GrantUpgrade(self, upgrade, timed);
				return;
			}

			var srcs = timed.Sources.Where(s => s.Source == source);
			if (srcs.Count() < dupesAllowed)
			{
				timed.Sources.Add(new TimedUpgrade.UpgradeSource(duration, source));
				if (AcceptsUpgrade(self, upgrade))
					GrantUpgrade(self, upgrade, timed);
				else
					timed.Sources.Remove(timed.Sources.MinBy(s => s.Remaining));
			}
			else
				srcs.MinBy(s => s.Remaining).Remaining = duration;

			timed.Remaining = Math.Max(duration, timed.Remaining);
		}

		// Different upgradeable traits may define (a) different level ranges for the same upgrade type,
		// and (b) multiple upgrade types for the same trait. The unrestricted level for each trait is
		// tracked independently so that we can correctly revoke levels without adding the burden of
		// tracking both the overall (unclamped) and effective (clamped) levels on each individual trait.
		void NotifyUpgradeLevelChanged(IEnumerable<IUpgradable> traits, Actor self, string upgrade, int levelAdjust)
		{
			foreach (var up in traits)
			{
				var oldLevel = levels.GetOrAdd(up);
				var newLevel = levels[up] = oldLevel + levelAdjust;

				// This will internally clamp the levels to its own restricted range
				up.UpgradeLevelChanged(self, upgrade, oldLevel, newLevel);
			}
		}

		int GetOverallLevel(IUpgradable upgradable)
		{
			int level;
			return levels.TryGetValue(upgradable, out level) ? level : 0;
		}

		public void GrantUpgrade(Actor self, string upgrade, object source)
		{
			CheckCanManageUpgrades();

			UpgradeState s;
			if (!upgrades.TryGetValue(upgrade, out s))
				return;

			// Track the upgrade source so that the upgrade can be removed without conflicts
			s.Sources.Add(source);

			NotifyUpgradeLevelChanged(s.Traits, self, upgrade, 1);
		}

		public void RevokeUpgrade(Actor self, string upgrade, object source)
		{
			CheckCanManageUpgrades();

			UpgradeState s;
			if (!upgrades.TryGetValue(upgrade, out s))
				return;

			if (!s.Sources.Remove(source))
				throw new InvalidOperationException("Object <{0}> revoked more levels of upgrade {1} than it granted for {2}.".F(source, upgrade, self));

			NotifyUpgradeLevelChanged(s.Traits, self, upgrade, -1);
		}

		/// <summary>Returns true if the actor uses the given upgrade. Does not check the actual level of the upgrade.</summary>
		public bool AcknowledgesUpgrade(Actor self, string upgrade)
		{
			CheckCanManageUpgrades();
			return upgrades.ContainsKey(upgrade);
		}

		/// <summary>Returns true only if the actor can accept another level of the upgrade.</summary>
		public bool AcceptsUpgrade(Actor self, string upgrade)
		{
			CheckCanManageUpgrades();

			UpgradeState s;
			if (!upgrades.TryGetValue(upgrade, out s))
				return false;

			return s.Traits.Any(up => up.AcceptsUpgradeLevel(self, upgrade, GetOverallLevel(up) + 1));
		}

		public void RegisterWatcher(string upgrade, Action<int, int> action)
		{
			CheckCanManageUpgrades();

			UpgradeState s;
			if (!upgrades.TryGetValue(upgrade, out s))
				return;

			s.Watchers.Add(action);
		}

		/// <summary>Watchers will be receiving notifications while the upgrade's level is nonzero.
		/// They will also be provided with the number of ticks before the level returns to zero,
		/// as well as the duration in ticks of the timed upgrade (provided in the first call to
		/// GrantTimedUpgrade).</summary>
		public void Tick(Actor self)
		{
			CheckCanManageUpgrades();

			foreach (var u in timedUpgrades)
			{
				u.Tick();
				foreach (var source in u.Sources)
					if (source.Remaining <= 0)
						RevokeUpgrade(self, u.Upgrade, u);

				u.Sources.RemoveWhere(source => source.Remaining <= 0);

				foreach (var a in upgrades[u.Upgrade].Watchers)
					a(u.Duration, u.Remaining);
			}

			timedUpgrades.RemoveAll(u => u.Remaining <= 0);
		}
	}
}
