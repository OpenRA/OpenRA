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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the player actor.")]
	public class GlobalUpgradeManagerInfo : ITraitInfo, Requires<TechTreeInfo>
	{
		public object Create(ActorInitializer init) { return new GlobalUpgradeManager(init); }
	}

	public class GlobalUpgradeManager : ITechTreeElement
	{
		readonly Actor self;
		readonly Dictionary<string, List<Pair<Actor, GlobalUpgradable>>> upgradables = new Dictionary<string, List<Pair<Actor, GlobalUpgradable>>>();
		readonly TechTree techTree;

		public GlobalUpgradeManager(ActorInitializer init)
		{
			self = init.Self;
			techTree = self.Trait<TechTree>();
		}

		static string MakeKey(string[] prerequisites)
		{
			return "upgrade_" + string.Join("_", prerequisites.OrderBy(a => a));
		}

		public void Register(Actor actor, GlobalUpgradable u, string[] prerequisites)
		{
			var key = MakeKey(prerequisites);
			if (!upgradables.ContainsKey(key))
			{
				upgradables.Add(key, new List<Pair<Actor, GlobalUpgradable>>());
				techTree.Add(key, prerequisites, 0, this);
			}

			upgradables[key].Add(Pair.New(actor, u));

			// Notify the current state
			u.PrerequisitesUpdated(actor, techTree.HasPrerequisites(prerequisites));
		}

		public void Unregister(Actor actor, GlobalUpgradable u, string[] prerequisites)
		{
			var key = MakeKey(prerequisites);
			var list = upgradables[key];

			list.RemoveAll(x => x.First == actor && x.Second == u);
			if (!list.Any())
			{
				upgradables.Remove(key);
				techTree.Remove(key);
			}
		}

		public void PrerequisitesAvailable(string key)
		{
			List<Pair<Actor, GlobalUpgradable>> list;
			if (!upgradables.TryGetValue(key, out list))
				return;

			foreach (var u in list)
				u.Second.PrerequisitesUpdated(u.First, true);
		}

		public void PrerequisitesUnavailable(string key)
		{
			List<Pair<Actor, GlobalUpgradable>> list;
			if (!upgradables.TryGetValue(key, out list))
				return;

			foreach (var u in list)
				u.Second.PrerequisitesUpdated(u.First, false);
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}
}
