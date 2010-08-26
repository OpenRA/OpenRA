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
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	public class TechTree
	{
		readonly Cache<string, List<ActorInfo>> producesIndex = new Cache<string, List<ActorInfo>>(x => new List<ActorInfo>());

		public TechTree()
		{
			foreach( var info in Rules.Info.Values )
			{
				var pi = info.Traits.GetOrDefault<ProductionInfo>();
				if (pi != null)
					foreach( var p in pi.Produces )
						producesIndex[ p ].Add( info );
			}
		}

		public Cache<string, List<Actor>> GatherBuildings( Player player )
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

		public bool CanBuild( ActorInfo info, Player player, Cache<string, List<Actor>> playerBuildings )
		{
			if (player == null)
				return false;
			
			var bi = info.Traits.GetOrDefault<BuildableInfo>();
			if( bi == null ) return false;

			if( !bi.Owner.Contains( player.Country.Race ) )
				return false;

			foreach( var p in bi.Prerequisites )
				if( playerBuildings[ p ].Count == 0 )
					return false;

			if( producesIndex[ bi.Queue ].All( x => playerBuildings[ x.Name ].Count == 0 ) )
				return false;

			return true;
		}

		public IEnumerable<string> BuildableItems( Player player, params string[] categories )
		{
			if (player == null)
				yield break;
			
			var playerBuildings = GatherBuildings( player );
			foreach (var unit in AllBuildables(categories))
				if( CanBuild( unit, player, playerBuildings ) )
					yield return unit.Name;
		}

		public IEnumerable<ActorInfo> AllBuildables(params string[] categories)
		{
			return Rules.Info.Values
				.Where( x => x.Name[ 0 ] != '^' )
				.Where( x => x.Traits.Contains<BuildableInfo>() )
				.Where( x => categories.Contains(x.Traits.Get<BuildableInfo>().Queue) );
		}

		public IEnumerable<ActorInfo> UnitBuiltAt( ActorInfo info )
		{
			var bi = info.Traits.Get<BuildableInfo>();
			var builtAt = bi.BuiltAt;
			if( builtAt.Length != 0 )
				return builtAt.Select( x => Rules.Info[ x.ToLowerInvariant() ] );
			else
				return producesIndex[ bi.Queue ];
		}
	}
}
