#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Attach this to the player actor.")]
	public class GrantConditionOnPrerequisiteManagerInfo : TraitInfo, Requires<TechTreeInfo>
	{
		public override object Create(ActorInitializer init) { return new GrantConditionOnPrerequisiteManager(init); }
	}

	public class GrantConditionOnPrerequisiteManager : ITechTreeElement
	{
		readonly Actor self;
		readonly Dictionary<string, List<(Actor Actor, GrantConditionOnPrerequisite GrantConditionOnPrerequisite)>> upgradables = new Dictionary<string, List<(Actor, GrantConditionOnPrerequisite)>>();
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
				upgradables.Add(key, new List<(Actor, GrantConditionOnPrerequisite)>());
				techTree.Add(key, prerequisites, 0, this);
			}

			upgradables[key].Add((actor, u));

			// Notify the current state
			u.PrerequisitesUpdated(actor, techTree.HasPrerequisites(prerequisites));
		}

		public void Unregister(Actor actor, GrantConditionOnPrerequisite u, string[] prerequisites)
		{
			var key = MakeKey(prerequisites);
			var list = upgradables[key];

			list.RemoveAll(x => x.Actor == actor && x.GrantConditionOnPrerequisite == u);
			if (list.Count == 0)
			{
				upgradables.Remove(key);
				techTree.Remove(key);
			}
		}

		public void PrerequisitesAvailable(string key)
		{
			if (!upgradables.TryGetValue(key, out var list))
				return;

			foreach (var u in list)
				u.GrantConditionOnPrerequisite.PrerequisitesUpdated(u.Actor, true);
		}

		public void PrerequisitesUnavailable(string key)
		{
			if (!upgradables.TryGetValue(key, out var list))
				return;

			foreach (var u in list)
				u.GrantConditionOnPrerequisite.PrerequisitesUpdated(u.Actor, false);
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}
}
