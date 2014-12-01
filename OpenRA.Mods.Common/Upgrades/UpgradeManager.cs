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
	[Desc("Attach this to a unit to enable dynamic upgrades by warheads, experience, crates, support powers, etc.")]
	public class UpgradeManagerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new UpgradeManager(init); }
	}

	public class UpgradeManager : ITick
	{
		class TimedUpgrade
		{
			public readonly string Upgrade;
			public readonly int Duration;
			public int Remaining;

			public TimedUpgrade(string upgrade, int duration)
			{
				Upgrade = upgrade;
				Duration = duration;
				Remaining = duration;
			}

			public void Tick() { Remaining--; }
		}

		class UpgradeState
		{
			public readonly List<IUpgradable> Traits = new List<IUpgradable>();
			public readonly List<object> Sources = new List<object>();
			public readonly List<Action<int, int>> Watchers = new List<Action<int, int>>();
		}

		readonly List<TimedUpgrade> timedUpgrades = new List<TimedUpgrade>();
		readonly Lazy<Dictionary<string, UpgradeState>> upgrades;
		readonly Dictionary<IUpgradable, int> levels = new Dictionary<IUpgradable, int>();

		public UpgradeManager(ActorInitializer init)
		{
			upgrades = Exts.Lazy(() =>
			{
				var ret = new Dictionary<string, UpgradeState>();
				foreach (var up in init.self.TraitsImplementing<IUpgradable>())
					foreach (var t in up.UpgradeTypes)
						ret.GetOrAdd(t).Traits.Add(up);

				return ret;
			});
		}

		public void GrantTimedUpgrade(Actor self, string upgrade, int duration)
		{
			var timed = timedUpgrades.FirstOrDefault(u => u.Upgrade == upgrade);
			if (timed == null)
			{
				timed = new TimedUpgrade(upgrade, duration);
				timedUpgrades.Add(timed);
				GrantUpgrade(self, upgrade, timed);
			}
			else
				timed.Remaining = Math.Max(duration, timed.Remaining);
		}

		// Different upgradeable traits may define (a) different level ranges for the same upgrade type,
		// and (b) multiple upgrade types for the same trait. The unrestricted level for each trait is
		// tracked independently so that we can can correctly revoke levels without adding the burden of
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
			UpgradeState s;
			if (!upgrades.Value.TryGetValue(upgrade, out s))
				return;

			// Track the upgrade source so that the upgrade can be removed without conflicts
			s.Sources.Add(source);

			NotifyUpgradeLevelChanged(s.Traits, self, upgrade, 1);
		}

		public void RevokeUpgrade(Actor self, string upgrade, object source)
		{
			UpgradeState s;
			if (!upgrades.Value.TryGetValue(upgrade, out s))
				return;

			if (!s.Sources.Remove(source))
				throw new InvalidOperationException("Object <{0}> revoked more levels of upgrade {1} than it granted for {2}.".F(source, upgrade, self));

			NotifyUpgradeLevelChanged(s.Traits, self, upgrade, -1);
		}

		public bool AcceptsUpgrade(Actor self, string upgrade)
		{
			UpgradeState s;
			if (!upgrades.Value.TryGetValue(upgrade, out s))
				return false;

			return s.Traits.Any(up => up.AcceptsUpgradeLevel(self, upgrade, GetOverallLevel(up) + 1));
		}

		public void RegisterWatcher(string upgrade, Action<int, int> action)
		{
			UpgradeState s;
			if (!upgrades.Value.TryGetValue(upgrade, out s))
				return;

			s.Watchers.Add(action);
		}

		public void Tick(Actor self)
		{
			foreach (var u in timedUpgrades)
			{
				u.Tick();
				if (u.Remaining <= 0)
					RevokeUpgrade(self, u.Upgrade, u);

				foreach (var a in upgrades.Value[u.Upgrade].Watchers)
					a(u.Duration, u.Remaining);
			}

			timedUpgrades.RemoveAll(u => u.Remaining <= 0);
		}
	}
}
