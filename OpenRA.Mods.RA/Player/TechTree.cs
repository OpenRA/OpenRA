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
			if (a.Owner == player && a.HasTrait<ITechTreePrerequisite>())
				Update();
		}

		public void Update()
		{
			var buildings = GatherBuildings(player);
			foreach(var w in watchers)
				w.Update(buildings);
		}

		public void Add(string key, List<string> prerequisites, ITechTreeElement tte)
		{
			watchers.Add(new Watcher( key, prerequisites, tte ));
		}

		public void Remove(string key)
		{
			watchers.RemoveAll(x => x.key == key);
		}

		static Cache<string, List<Actor>> GatherBuildings( Player player )
		{
			var ret = new Cache<string, List<Actor>>( x => new List<Actor>() );
			if (player == null)
				return ret;


			foreach (var b in player.World.ActorsWithTrait<ITechTreePrerequisite>()
					.Where(a => a.Actor.IsInWorld && !a.Actor.IsDead() && a.Actor.Owner == player))
				foreach (var p in b.Trait.ProvidesPrerequisites)
					ret[ p ].Add( b.Actor );

			return ret;
		}

		class Watcher
		{
			public readonly string key;
			// strings may be either actor type, or "alternate name" key
			public readonly List<string> prerequisites;
			public readonly ITechTreeElement watcher;
			bool hasPrerequisites;

			public Watcher(string key, List<string> prerequisites, ITechTreeElement watcher)
			{
				this.key = key;
				this.prerequisites = prerequisites;
				this.watcher = watcher;
				this.hasPrerequisites = false;
			}

			bool HasPrerequisites(Cache<string, List<Actor>> buildings)
			{
				foreach (var p in prerequisites)
					if (p.StartsWith("!") ^
						!buildings.Keys.Contains(p.Replace("!","")))
						return false;
				return true;
			}

			public void Update(Cache<string, List<Actor>> buildings)
			{
				var nowHasPrerequisites = HasPrerequisites(buildings);

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
