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
			public readonly List<ActorInfo> prerequisites;
			public readonly ITechTreeElement watcher;
			bool hasPrerequisites;

			public Watcher(List<ActorInfo> prerequisites, ITechTreeElement watcher)
			{
				this.prerequisites = prerequisites;
				this.watcher = watcher;
				this.hasPrerequisites = false;
			}

			public void Tick( Player owner, Cache<string, List<Actor>> buildings )
			{
				var effectivePrereq = prerequisites.Where( a => a.Traits.Get<BuildableInfo>().Owner.Contains( owner.Country.Race ) );
				var nowHasPrerequisites = effectivePrereq.Any() &&
					effectivePrereq.All( a => buildings[ a.Name ].Any( b => !b.traits.Get<Building>().Disabled ) );

				if( nowHasPrerequisites && !hasPrerequisites )
					watcher.Available();

				if( !nowHasPrerequisites && hasPrerequisites )
					watcher.Unavailable();

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

		public void Add( List<ActorInfo> prerequisites, ITechTreeElement tte )
		{
			watchers.Add( new Watcher( prerequisites, tte ) );
		}

		public void Remove( ITechTreeElement tte )
		{
			watchers.RemoveAll( x => x.watcher == tte );
		}
	}

	interface ITechTreeElement
	{
		void Available();
		void Unavailable();
	}
}
