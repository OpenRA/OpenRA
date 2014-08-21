#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
			if (a.Owner == player && (a.HasTrait<ITechTreePrerequisite>()))
				Update();
		}

		public void Update()
		{
			var ownedPrerequisites = GatherOwnedPrerequisites(player);
			foreach (var w in watchers)
				w.Update(ownedPrerequisites);
		}

		public void Add(IEnumerable<string> prerequisites, ITechTreeElement tte)
		{
			var prereqsOrdered = prerequisites.OrderBy(a => a);
			var w = watchers.Where(watcher => prereqsOrdered.Count() == watcher.Prerequisites.Count()
									&& prereqsOrdered.All(p => watcher.Prerequisites.Contains(p)));
			if (!w.Any())
			{
				var watcher = new Watcher(prereqsOrdered);
				watcher.AddElement(tte);
				watchers.Add(watcher);
				return;
			}
			w.First().AddElement(tte);
		}

		public void Remove(IEnumerable<string> prerequisites)
		{
			var prereqsOrdered = prerequisites.OrderBy(a => a);
			watchers.RemoveAll(watcher => prereqsOrdered.All(p => watcher.Prerequisites.Contains(p)));
		}

		public void Remove(ITechTreeElement tte)
		{
			watchers.Where(w => w.HasElement(tte)).Do(w => w.RemoveElement(tte));
		}

		public bool HasPrerequisites(IEnumerable<string> prerequisites)
		{
			var ownedPrereqs = TechTree.GatherOwnedPrerequisites(player);
			return prerequisites.All(p => !(p.Replace("~", "").StartsWith("!")
					^ !ownedPrereqs.ContainsKey(p.Replace("!", "").Replace("~", ""))));
		}

		static Cache<string, List<Actor>> GatherOwnedPrerequisites(Player player)
		{
			var ret = new Cache<string, List<Actor>>(x => new List<Actor>());
			if (player == null)
				return ret;

			// Add all actors that provide prerequisites
			var prerequisites = player.World.ActorsWithTrait<ITechTreePrerequisite>()
				.Where(a => a.Actor.Owner == player && a.Actor.IsInWorld && !a.Actor.IsDead());

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

			return ret;
		}

		class Watcher
		{
			public IEnumerable<string> Prerequisites { get { return prerequisites; } }

			IEnumerable<string> prerequisites;
			List<ITechTreeElement> watched = new List<ITechTreeElement>();
			bool hasPrerequisites = false;
			bool hidden = false;
			bool initialized = false;

			public Watcher(IEnumerable<string> prerequisites)
			{
				this.prerequisites = prerequisites;
			}

			public void AddElement(ITechTreeElement element)
			{
				watched.Add(element);
			}

			public void RemoveElement(ITechTreeElement element)
			{
				watched.Remove(element);
			}

			public bool HasElement(ITechTreeElement element)
			{
				return watched.Contains(element);
			}

			bool HasPrerequisites(Cache<string, List<Actor>> ownedPrerequisites)
			{
				return prerequisites.All(p => !(p.Replace("~", "").StartsWith("!") ^ !ownedPrerequisites.ContainsKey(p.Replace("!", "").Replace("~", ""))));
			}

			bool IsHidden(Cache<string, List<Actor>> ownedPrerequisites)
			{
				return prerequisites.Any(prereq => prereq.StartsWith("~") && (prereq.Replace("~", "").StartsWith("!") ^ !ownedPrerequisites.ContainsKey(prereq.Replace("~", "").Replace("!", ""))));
			}

			public void Update(Cache<string, List<Actor>> ownedPrerequisites)
			{
				// The '!' annotation inverts prerequisites: "I'm buildable if this prerequisite *isn't* met"
				var nowHasPrerequisites = HasPrerequisites(ownedPrerequisites);
				var nowHidden = IsHidden(ownedPrerequisites);

				if (initialized == false)
				{
					initialized = true;
					hasPrerequisites = !nowHasPrerequisites;
					hidden = !nowHidden;
				}

				// Hide the item from the UI if a prereq annotated with '~' is not met.
				if (nowHidden && !hidden)
					watched.Do(w => w.PrerequisitesItemHidden());

				if (!nowHidden && hidden)
					watched.Do(w => w.PrerequisitesItemVisible());

				if (nowHasPrerequisites && !hasPrerequisites)
					watched.Do(w => w.PrerequisitesAvailable());

				if (!nowHasPrerequisites && hasPrerequisites)
					watched.Do(w => w.PrerequisitesUnavailable());

				hidden = nowHidden;
				hasPrerequisites = nowHasPrerequisites;
			}
		}
	}
}
