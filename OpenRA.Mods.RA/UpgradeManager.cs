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

namespace OpenRA.Mods.RA
{
	public class UpgradeManagerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new UpgradeManager(init); }
	}

	public class UpgradeManager : ITick
	{
		public class TimedUpgrade
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

		readonly List<TimedUpgrade> timedUpgrades = new List<TimedUpgrade>();
		readonly Dictionary<string, List<object>> sources = new Dictionary<string, List<object>>();
		readonly Dictionary<string, List<Action<int, int>>> watchers = new Dictionary<string, List<Action<int, int>>>();
		readonly Lazy<IEnumerable<IUpgradable>> upgradable;

		public UpgradeManager(ActorInitializer init)
		{
			upgradable = Exts.Lazy(() => init.self.TraitsImplementing<IUpgradable>());
		}

		public object GrantTimedUpgrade(Actor self, string upgrade, int duration)
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
			return timed;
		}

		public object GrantUpgrade(Actor self, string upgrade, object source)
		{
			List<object> ss;
			if (!sources.TryGetValue(upgrade, out ss))
			{
				ss = new List<object>();
				sources.Add(upgrade, ss);

				foreach (var up in upgradable.Value)
					if (up.AcceptsUpgrade(upgrade))
						up.UpgradeAvailable(self, upgrade, true);
			}

			// Track the upgrade source so that the upgrade can be removed without conflicts
			ss.Add(source);
			return source;
		}

		public void RevokeUpgrade(Actor self, string upgrade, object source)
		{
			// This upgrade may have been granted by multiple sources
			// We must be careful to only remove the upgrade after all
			// sources have been revoked
			List<object> ss;
			if (!sources.TryGetValue(upgrade, out ss))
				return;

			ss.Remove(source);
			if (!ss.Any())
			{
				foreach (var up in upgradable.Value)
					if (up.AcceptsUpgrade(upgrade))
						up.UpgradeAvailable(self, upgrade, false);

				sources.Remove(upgrade);
			}

			if (source is TimedUpgrade)
			{
				var u = source as TimedUpgrade;
				u.Remaining = 0;
				NotifyWatchers(u);
				timedUpgrades.Remove(u);
			}
		}

		public bool AcceptsUpgrade(Actor self, string upgrade)
		{
			return upgradable.Value.Any(up => up.AcceptsUpgrade(upgrade));
		}

		public void RegisterWatcher(string upgrade, Action<int, int> action)
		{
			if (!watchers.ContainsKey(upgrade))
				watchers.Add(upgrade, new List<Action<int, int>>());

			watchers[upgrade].Add(action);
		}

		void NotifyWatchers(TimedUpgrade u)
		{
			List<Action<int, int>> actions;
			if (watchers.TryGetValue(u.Upgrade, out actions))
				foreach (var a in actions)
					a(u.Duration, u.Remaining);
		}

		public void Tick(Actor self)
		{
			foreach (var u in timedUpgrades)
			{
				u.Tick();
				if (u.Remaining <= 0)
				{
					RevokeUpgrade(self, u.Upgrade, u);
					timedUpgrades.Remove(u);
				}
				else
					NotifyWatchers(u);
			}
		}
	}
}
