#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	class TechTreeCacheInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new TechTreeCache(init);}
	}

	class TechTreeCache
	{
		readonly List<Watcher> watchers = new List<Watcher>();
		readonly Player player;
		public TechTreeCache(ActorInitializer init)
		{
			player = init.self.Owner;
			init.world.ActorAdded += ActorChanged;
			init.world.ActorRemoved += ActorChanged;
		}
		
		public void ActorChanged(Actor a)
		{
			if (a.Owner == player && a.HasTrait<Building>())
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
		
		public static Cache<string, List<Actor>> GatherBuildings( Player player )
		{
			var ret = new Cache<string, List<Actor>>( x => new List<Actor>() );
			if (player == null)
				return ret;

			foreach( var b in player.World.Queries.OwnedBy[player].Where( x=>x.Info.Traits.Contains<BuildingInfo>() ) )
			{
				ret[ b.Info.Name ].Add( b );
				var tt = b.Info.Traits.GetOrDefault<TooltipInfo>();
				if( tt != null )
					foreach( var alt in tt.AlternateName )
						ret[ alt ].Add( b );
			}
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

			public void Update(Cache<string, List<Actor>> buildings)
			{
				var nowHasPrerequisites = true;
				foreach (var p in prerequisites)
					if (!buildings.Keys.Contains(p))
					{
						nowHasPrerequisites = false;
						break;
					}
								
				if( nowHasPrerequisites && !hasPrerequisites )
					watcher.PrerequisitesAvailable(key);

				if( !nowHasPrerequisites && hasPrerequisites )
					watcher.PrerequisitesUnavailable(key);

				hasPrerequisites = nowHasPrerequisites;
			}
		}
	}

	interface ITechTreeElement
	{
		void PrerequisitesAvailable(string key);
		void PrerequisitesUnavailable(string key);
	}
}
