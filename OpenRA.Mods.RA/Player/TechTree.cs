#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class TechTreeInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new TechTree(init); }
	}

	public class TechTree
	{
		readonly List<Watcher> watchers = new List<Watcher>();
		readonly Player player;

		public TechTree(ActorInitializer init)
		{
			player = init.self.Owner;
			init.world.ActorAdded += ActorChanged;
			init.world.ActorRemoved += ActorChanged;
		}

		public void ActorChanged(Actor a)
		{
			var bi = a.Info.Traits.GetOrDefault<BuildableInfo>();
			if (a.Owner == player && (a.HasTrait<ITechTreePrerequisite>() || (bi != null && bi.BuildLimit > 0)))
				Update();
		}

		public void Update()
		{
			var ownedPrerequisites = GatherOwnedPrerequisites(player);
			foreach (var w in watchers)
				w.Update(ownedPrerequisites);
		}

		public void Add(string key, string[] prerequisites, int limit, ITechTreeElement tte)
		{
			watchers.Add(new Watcher(key, prerequisites, limit, tte));
		}

		public void Remove(string key)
		{
			watchers.RemoveAll(x => x.Key == key);
		}

		static Cache<string, List<Actor>> GatherOwnedPrerequisites(Player player)
		{
			var ret = new Cache<string, List<Actor>>(x => new List<Actor>());
			if (player == null)
				return ret;

			// Add all actors that provide prerequisites
			var prerequisites = player.World.ActorsWithTrait<ITechTreePrerequisite>()
				.Where(a => a.Actor.Owner == player && !a.Actor.IsDead() && a.Actor.IsInWorld);

			foreach (var b in prerequisites)
			{
				foreach (var p in b.Trait.ProvidesPrerequisites)
				{
					// Ignore bogus prerequisites
					if (p == null)
						continue;

					ret[p].Add(b.Actor);
				}
			}

			// Add buildables that have a build limit set and are not already in the list
			player.World.ActorsWithTrait<Buildable>()
				  .Where(a => a.Actor.Info.Traits.Get<BuildableInfo>().BuildLimit > 0 && !a.Actor.IsDead() && a.Actor.IsInWorld && a.Actor.Owner == player && ret.Keys.All(k => k != a.Actor.Info.Name))
				  .Do(b => ret[b.Actor.Info.Name].Add(b.Actor));

			return ret;
		}

		class Watcher
		{
			public readonly string Key;

			// Strings may be either actor type, or "alternate name" key
			readonly string[] prerequisites;
			readonly ITechTreeElement watcher;
			bool hasPrerequisites;
			int limit;
			bool hidden;
			bool initialized = false;

			public Watcher(string key, string[] prerequisites, int limit, ITechTreeElement watcher)
			{
				this.Key = key;
				this.prerequisites = prerequisites;
				this.watcher = watcher;
				this.hasPrerequisites = false;
				this.limit = limit;
				this.hidden = false;
			}

			bool HasPrerequisites(Cache<string, List<Actor>> ownedPrerequisites)
			{
				return prerequisites.All(p => !(p.Replace("~", "").StartsWith("!", StringComparison.Ordinal) ^ !ownedPrerequisites.Keys.Contains(p.Replace("!", "").Replace("~", ""))));
			}

			bool IsHidden(Cache<string, List<Actor>> ownedPrerequisites)
			{
                return prerequisites.Any(prereq => 
                    prereq.StartsWith("~", StringComparison.Ordinal) && 
                    (prereq.Replace("~", "").StartsWith("!", StringComparison.Ordinal) ^ 
                    !ownedPrerequisites.Keys.Contains(prereq.Replace("~", "").Replace("!", ""))));
			}

			public void Update(Cache<string, List<Actor>> ownedPrerequisites)
			{
				var hasReachedLimit = limit > 0 && ownedPrerequisites.Keys.Contains(Key) && ownedPrerequisites[Key].Count >= limit;
				// The '!' annotation inverts prerequisites: "I'm buildable if this prerequisite *isn't* met"
				var nowHasPrerequisites = HasPrerequisites(ownedPrerequisites) && !hasReachedLimit;
				var nowHidden = IsHidden(ownedPrerequisites);

				if (initialized == false)
				{
					initialized = true;
					hasPrerequisites = !nowHasPrerequisites;
					hidden = !nowHidden;
				}

				// Hide the item from the UI if a prereq annotated with '~' is not met.
				if (nowHidden && !hidden)
					watcher.PrerequisitesItemHidden(Key);

				if (!nowHidden && hidden)
					watcher.PrerequisitesItemVisable(Key);

				if (nowHasPrerequisites && !hasPrerequisites)
					watcher.PrerequisitesAvailable(Key);

				if (!nowHasPrerequisites && hasPrerequisites)
					watcher.PrerequisitesUnavailable(Key);

				hidden = nowHidden;
				hasPrerequisites = nowHasPrerequisites;
			}
		}
	}

	public class ProvidesCustomPrerequisiteInfo : ITraitInfo
	{
		public readonly string Prerequisite;

		public object Create(ActorInitializer init) { return new ProvidesCustomPrerequisite(this); }
	}

	public class ProvidesCustomPrerequisite : ITechTreePrerequisite
	{
		ProvidesCustomPrerequisiteInfo info;

		public IEnumerable<string> ProvidesPrerequisites { get { yield return info.Prerequisite; } }

		public ProvidesCustomPrerequisite(ProvidesCustomPrerequisiteInfo info)
		{
			this.info = info;
		}
	}
}
