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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Attach this to the player actor.")]
	public class GlobalConditionManagerInfo : ITraitInfo, Requires<TechTreeInfo>
	{
		public object Create(ActorInitializer init) { return new GlobalConditionManager(init); }
	}

	public class GlobalConditionManager : ITechTreeElement
	{
		readonly Actor self;
		readonly Dictionary<string, List<Pair<Actor, GlobalConditional>>> conditionals = new Dictionary<string, List<Pair<Actor, GlobalConditional>>>();
		readonly TechTree techTree;

		public GlobalConditionManager(ActorInitializer init)
		{
			self = init.self;
			techTree = self.Trait<TechTree>();
		}

		static string MakeKey(string[] prerequisites)
		{
			return "condition_" + string.Join("_", prerequisites.OrderBy(a => a));
		}

		public void Register(Actor actor, GlobalConditional u, string[] prerequisites)
		{
			var key = MakeKey(prerequisites);
			if (!conditionals.ContainsKey(key))
			{
				conditionals.Add(key, new List<Pair<Actor, GlobalConditional>>());
				techTree.Add(key, prerequisites, 0, this);
			}

			conditionals[key].Add(Pair.New(actor, u));

			// Notify the current state
			u.PrerequisitesUpdated(actor, techTree.HasPrerequisites(prerequisites));
		}

		public void Unregister(Actor actor, GlobalConditional u, string[] prerequisites)
		{
			var key = MakeKey(prerequisites);
			var list = conditionals[key];

			list.RemoveAll(x => x.First == actor && x.Second == u);
			if (!list.Any())
			{
				conditionals.Remove(key);
				techTree.Remove(key);
			}
		}

		public void PrerequisitesAvailable(string key)
		{
			List<Pair<Actor, GlobalConditional>> list;
			if (!conditionals.TryGetValue(key, out list))
				return;

			foreach (var u in list)
				u.Second.PrerequisitesUpdated(u.First, true);
		}

		public void PrerequisitesUnavailable(string key)
		{
			List<Pair<Actor, GlobalConditional>> list;
			if (!conditionals.TryGetValue(key, out list))
				return;

			foreach (var u in list)
				u.Second.PrerequisitesUpdated(u.First, false);
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}
}
