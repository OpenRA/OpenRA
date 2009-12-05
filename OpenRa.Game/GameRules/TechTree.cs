using System.Collections.Generic;
using System.Linq;
using IjwFramework.Collections;

namespace OpenRa.Game.GameRules
{
	class TechTree
	{
		readonly Cache<string, List<UnitInfo>> producesIndex = new Cache<string, List<UnitInfo>>( x => new List<UnitInfo>() );

		public TechTree()
		{
			foreach( var b in Rules.Categories[ "Building" ] )
			{
				var info = (BuildingInfo)Rules.UnitInfo[ b ];
				foreach( var p in info.Produces )
					producesIndex[ p ].Add( info );
			}
		}

		public Cache<string, List<Actor>> GatherBuildings( Player player )
		{
			var ret = new Cache<string, List<Actor>>( x => new List<Actor>() );
			foreach( var b in Game.world.Actors.Where( x => x.Owner == player && x.Info is BuildingInfo ) )
				ret[ b.Info.Name ].Add( b );
			return ret;
		}

		public bool CanBuild( UnitInfo unit, Player player, Cache<string, List<Actor>> playerBuildings )
		{
			if( unit.TechLevel == -1 )
				return false;

			if( !unit.Owner.Any( x => x == player.Race ) )
				return false;

			foreach( var p in unit.Prerequisite )
				if( playerBuildings[ p ].Count == 0 )
					return false;

			if( producesIndex[ Rules.UnitCategory[ unit.Name ] ].All( x => playerBuildings[ x.Name ].Count == 0 ) )
				return false;

			return true;
		}

		public IEnumerable<string> BuildableItems( Player player, params string[] categories )
		{
			var playerBuildings = GatherBuildings( player );
			foreach( var unit in categories.SelectMany( x => Rules.Categories[ x ] ).Select( x => Rules.UnitInfo[ x ] ) )
				if( CanBuild( unit, player, playerBuildings ) )
					yield return unit.Name;
		}

		public IEnumerable<string> AllItems(Player player, params string[] categories)
		{
			return categories.SelectMany(x => Rules.Categories[x]).Select(x => Rules.UnitInfo[x].Name)
				.Where(x => Rules.UnitInfo[x].Owner.Contains(player.Race));	/* todo: fix for dual-race scenarios (captured buildings) */
		}

		public IEnumerable<UnitInfo> UnitBuiltAt( UnitInfo info )
		{
			if( info.BuiltAt.Length != 0 )
				return info.BuiltAt.Select( x => Rules.UnitInfo[ x.ToLowerInvariant() ] );
			else
				return producesIndex[ Rules.UnitCategory[ info.Name ] ];
		}
	}
}
