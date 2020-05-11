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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the player actor.")]
	public class GrantConditionOnPrerequisiteManagerInfo : TraitInfo, Requires<TechTreeInfo>
	{
		public override object Create(ActorInitializer init) { return new GrantConditionOnPrerequisiteManager(init); }
	}

	public class GrantConditionOnPrerequisiteManager : ITechTreeElement
	{
		readonly Actor self;
		readonly Dictionary<string, List<Pair<Actor, GrantConditionOnPrerequisite>>> upgradables = new Dictionary<string, List<Pair<Actor, GrantConditionOnPrerequisite>>>();
		readonly TechTree techTree;

		public GrantConditionOnPrerequisiteManager(ActorInitializer init)
		{
			self = init.Self;
			techTree = self.Trait<TechTree>();
		}

		static string MakeKey(string[] prerequisites)
		{
			return "condition_" + string.Join("_", prerequisites.OrderBy(a => a));
		}

		public void Register(Actor actor, GrantConditionOnPrerequisite u, string[] prerequisites)
		{
			var key = MakeKey(prerequisites);
			if (!upgradables.ContainsKey(key))
			{
				upgradables.Add(key, new List<Pair<Actor, GrantConditionOnPrerequisite>>());
				techTree.Add(key, prerequisites, 0, this);
			}

			upgradables[key].Add(Pair.New(actor, u));

			// Notify the current state
			u.PrerequisitesUpdated(actor, techTree.HasPrerequisites(prerequisites));
		}

		public void Unregister(Actor actor, GrantConditionOnPrerequisite u, string[] prerequisites)
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
			List<Pair<Actor, GrantConditionOnPrerequisite>> list;
			if (!upgradables.TryGetValue(key, out list))
				return;

			foreach (var u in list)
				u.Second.PrerequisitesUpdated(u.First, true);
		}

		public void PrerequisitesUnavailable(string key)
		{
			List<Pair<Actor, GrantConditionOnPrerequisite>> list;
			if (!upgradables.TryGetValue(key, out list))
				return;

			foreach (var u in list)
				u.Second.PrerequisitesUpdated(u.First, false);
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}
}
