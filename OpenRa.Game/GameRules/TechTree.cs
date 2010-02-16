#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Traits;

namespace OpenRa.GameRules
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
			foreach( var b in player.World.Queries.OwnedBy[player].Where( x=>x.Info.Traits.Contains<BuildingInfo>() ) )
			{
				ret[ b.Info.Name ].Add( b );
				var buildable = b.Info.Traits.GetOrDefault<BuildableInfo>();
				if( buildable != null )
					foreach( var alt in buildable.AlternateName )
						ret[ alt ].Add( b );
			}
			return ret;
		}

		public bool CanBuild( ActorInfo info, Player player, Cache<string, List<Actor>> playerBuildings )
		{
			var bi = info.Traits.GetOrDefault<BuildableInfo>();
			if( bi == null ) return false;

			if( !bi.Owner.Contains( player.Country.Race ) )
				return false;

			foreach( var p in bi.Prerequisites )
				if( playerBuildings[ p ].Count == 0 )
					return false;

			if( producesIndex[ info.Category ].All( x => playerBuildings[ x.Name ].Count == 0 ) )
				return false;

			return true;
		}

		public IEnumerable<string> BuildableItems( Player player, params string[] categories )
		{
			var playerBuildings = GatherBuildings( player );
			foreach( var unit in AllBuildables( player, categories ) )
				if( CanBuild( unit, player, playerBuildings ) )
					yield return unit.Name;
		}

		public IEnumerable<ActorInfo> AllBuildables(Player player, params string[] categories)
		{
			return Rules.Info.Values
				.Where( x => x.Name[ 0 ] != '^' )
				.Where( x => categories.Contains( x.Category ) )
				.Where( x => x.Traits.Contains<BuildableInfo>() );
		}

		public IEnumerable<ActorInfo> UnitBuiltAt( ActorInfo info )
		{
			var builtAt = info.Traits.Get<BuildableInfo>().BuiltAt;
			if( builtAt.Length != 0 )
				return builtAt.Select( x => Rules.Info[ x.ToLowerInvariant() ] );
			else
				return producesIndex[ info.Category ];
		}
	}
}
