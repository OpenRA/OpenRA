#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class TechTreeInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new TechTree(init);}
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
			var buildables = GatherBuildables(player);
			foreach(var w in watchers)
				w.Update(buildables);
		}

		public void Add(string key, BuildableInfo info, ITechTreeElement tte)
		{
			watchers.Add(new Watcher( key, info, tte ));
		}

		public void Remove(string key)
		{
			watchers.RemoveAll(x => x.key == key);
		}

		static Cache<string, List<Actor>> GatherBuildables( Player player )
		{
			var ret = new Cache<string, List<Actor>>( x => new List<Actor>() );
			if (player == null)
				return ret;

			// Add buildables that provide prerequisites
			foreach (var b in player.World.ActorsWithTrait<ITechTreePrerequisite>()
					.Where(a => a.Actor.IsInWorld && !a.Actor.IsDead() && a.Actor.Owner == player))
				foreach (var p in b.Trait.ProvidesPrerequisites)
					ret[ p ].Add( b.Actor );

			// Add buildables that have a build limit set and are not already in the list
			player.World.ActorsWithTrait<Buildable>()
				  .Where(a => a.Actor.Info.Traits.Get<BuildableInfo>().BuildLimit > 0 && !a.Actor.IsDead() && a.Actor.Owner == player && ret.Keys.All(k => k != a.Actor.Info.Name))
				  .ToList()
				  .ForEach(b => ret[b.Actor.Info.Name].Add(b.Actor));

			return ret;
		}

		class Watcher
		{
			public readonly string key;
			// strings may be either actor type, or "alternate name" key
			public readonly string[] prerequisites;
			public readonly ITechTreeElement watcher;
			bool hasPrerequisites;
			int buildLimit;

			public Watcher(string key, BuildableInfo info, ITechTreeElement watcher)
			{
				this.key = key;
				this.prerequisites = info.Prerequisites;
				this.watcher = watcher;
				this.hasPrerequisites = false;
				this.buildLimit = info.BuildLimit;
			}

			bool HasPrerequisites(Cache<string, List<Actor>> buildables)
			{
				return prerequisites.All(p => !(p.StartsWith("!") ^ !buildables.Keys.Contains(p.Replace("!", ""))));
			}

			public void Update(Cache<string, List<Actor>> buildables)
			{
				var hasReachedBuildLimit = buildLimit > 0 && buildables[key].Count >= buildLimit;

				var nowHasPrerequisites = HasPrerequisites(buildables) && !hasReachedBuildLimit;

				if( nowHasPrerequisites && !hasPrerequisites )
					watcher.PrerequisitesAvailable(key);

				if( !nowHasPrerequisites && hasPrerequisites )
					watcher.PrerequisitesUnavailable(key);

				hasPrerequisites = nowHasPrerequisites;
			}
		}
	}

	public class ProvidesCustomPrerequisiteInfo : ITraitInfo
	{
		public readonly string Prerequisite;

		public object Create(ActorInitializer init) { return new ProvidesCustomPrerequisite(this);}
	}

	public class ProvidesCustomPrerequisite : ITechTreePrerequisite
	{
		ProvidesCustomPrerequisiteInfo Info;

		public IEnumerable<string> ProvidesPrerequisites { get { yield return Info.Prerequisite; } }

		public ProvidesCustomPrerequisite(ProvidesCustomPrerequisiteInfo info)
		{
			Info = info;
		}
	}
}
