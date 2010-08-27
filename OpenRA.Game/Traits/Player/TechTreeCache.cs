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
	class TechTreeCacheInfo : TraitInfo<TechTreeCache> { }

	class TechTreeCache : ITick
	{
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

			public void Tick( Player owner, Cache<string, List<Actor>> buildings )
			{
				var nowHasPrerequisites = prerequisites.All( a => buildings[ a ].Any( b => !b.Trait<Building>().Disabled ) );

				if( nowHasPrerequisites && !hasPrerequisites )
					watcher.PrerequisitesAvailable(key);

				if( !nowHasPrerequisites && hasPrerequisites )
					watcher.PrerequisitesUnavailable(key);

				hasPrerequisites = nowHasPrerequisites;
			}
		}

		readonly List<Watcher> watchers = new List<Watcher>();

		public void Tick( Actor self )
		{
			var buildings = Rules.TechTree.GatherBuildings( self.Owner );

			foreach( var w in watchers )
				w.Tick( self.Owner, buildings );
		}

		public void Add( string key, List<string> prerequisites, ITechTreeElement tte )
		{
			watchers.Add( new Watcher( key, prerequisites, tte ) );
		}

		public void Remove( string key )
		{
			watchers.RemoveAll( x => x.key == key );
		}
	}

	interface ITechTreeElement
	{
		void PrerequisitesAvailable(string key);
		void PrerequisitesUnavailable(string key);
	}
}
