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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages build limits and pre-requisites.", " Attach this to the player actor.")]
	public class TechTreeInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new TechTree(init); }
	}

	public class TechTree
	{
		readonly List<Watcher> watchers = new List<Watcher>();
		readonly Player player;

		public TechTree(ActorInitializer init)
		{
			player = init.Self.Owner;
			init.World.ActorAdded += ActorChanged;
			init.World.ActorRemoved += ActorChanged;
		}

		public void ActorChanged(Actor a)
		{
			var bi = a.Info.TraitInfoOrDefault<BuildableInfo>();
			if (a.Owner == player && (a.Info.HasTraitInfo<ITechTreePrerequisiteInfo>() || (bi != null && bi.BuildLimit > 0)))
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

		public void Remove(ITechTreeElement tte)
		{
			watchers.RemoveAll(x => x.RegisteredBy == tte);
		}

		public bool HasPrerequisites(IEnumerable<string> prerequisites)
		{
			var ownedPrereqs = GatherOwnedPrerequisites(player);
			return prerequisites.All(p => !(p.Replace("~", "").StartsWith("!", StringComparison.Ordinal)
					^ !ownedPrereqs.ContainsKey(p.Replace("!", "").Replace("~", ""))));
		}

		static Cache<string, List<Actor>> GatherOwnedPrerequisites(Player player)
		{
			var ret = new Cache<string, List<Actor>>(x => new List<Actor>());
			if (player == null)
				return ret;

			// Add all actors that provide prerequisites
			var prerequisites = player.World.ActorsWithTrait<ITechTreePrerequisite>()
				.Where(a => a.Actor.Owner == player && a.Actor.IsInWorld && !a.Actor.IsDead);

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
			var buildables = player.World.ActorsWithTrait<Buildable>()
				.Where(a =>
					a.Actor.Owner == player &&
					a.Actor.IsInWorld &&
					!a.Actor.IsDead &&
					!ret.ContainsKey(a.Actor.Info.Name) &&
					a.Actor.Info.TraitInfo<BuildableInfo>().BuildLimit > 0);

			foreach (var buildable in buildables)
				ret[buildable.Actor.Info.Name].Add(buildable.Actor);

			return ret;
		}

		public Player Owner => player;

		class Watcher
		{
			public readonly string Key;
			public ITechTreeElement RegisteredBy => watcher;

			// Strings may be either actor type, or "alternate name" key
			readonly string[] prerequisites;
			readonly ITechTreeElement watcher;
			bool hasPrerequisites;
			readonly int limit;
			bool hidden;
			bool initialized = false;

			public Watcher(string key, string[] prerequisites, int limit, ITechTreeElement watcher)
			{
				Key = key;
				this.prerequisites = prerequisites;
				this.watcher = watcher;
				hasPrerequisites = false;
				this.limit = limit;
				hidden = false;
			}

			bool HasPrerequisites(Cache<string, List<Actor>> ownedPrerequisites)
			{
				// PERF: Avoid LINQ.
				foreach (var prereq in prerequisites)
				{
					var withoutTilde = prereq.Replace("~", "");
					if (withoutTilde.StartsWith("!", StringComparison.Ordinal) ^ !ownedPrerequisites.ContainsKey(withoutTilde.Replace("!", "")))
						return false;
				}

				return true;
			}

			bool IsHidden(Cache<string, List<Actor>> ownedPrerequisites)
			{
				// PERF: Avoid LINQ.
				foreach (var prereq in prerequisites)
				{
					if (!prereq.StartsWith("~", StringComparison.Ordinal))
						continue;
					var withoutTilde = prereq.Replace("~", "");
					if (withoutTilde.StartsWith("!", StringComparison.Ordinal) ^ !ownedPrerequisites.ContainsKey(withoutTilde.Replace("!", "")))
						return true;
				}

				return false;
			}

			public void Update(Cache<string, List<Actor>> ownedPrerequisites)
			{
				var hasReachedLimit = limit > 0 && ownedPrerequisites.ContainsKey(Key) && ownedPrerequisites[Key].Count >= limit;

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
					watcher.PrerequisitesItemVisible(Key);

				if (nowHasPrerequisites && !hasPrerequisites)
					watcher.PrerequisitesAvailable(Key);

				if (!nowHasPrerequisites && hasPrerequisites)
					watcher.PrerequisitesUnavailable(Key);

				hidden = nowHidden;
				hasPrerequisites = nowHasPrerequisites;
			}
		}
	}
}
