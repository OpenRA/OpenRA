using System.Collections.Generic;
using System.Linq;
using IjwFramework.Collections;
using OpenRa.Game.Traits;

namespace OpenRa.Game.GameRules
{
	class TechTree
	{
		readonly Cache<string, List<NewUnitInfo>> producesIndex = new Cache<string, List<NewUnitInfo>>(x => new List<NewUnitInfo>());

		public TechTree()
		{
			foreach( var b in Rules.Categories[ "Building" ] )
			{
				var info = Rules.NewUnitInfo[ b ];
				var pi = info.Traits.GetOrDefault<ProductionInfo>();
				if (pi != null)
					foreach( var p in pi.Produces )
						producesIndex[ p ].Add( info );
			}
		}

		public Cache<string, List<Actor>> GatherBuildings( Player player )
		{
			var ret = new Cache<string, List<Actor>>( x => new List<Actor>() );
			foreach( var b in Game.world.Actors.Where( x => x.Owner == player && x.Info != null && x.Info.Traits.Contains<BuildingInfo>() ) )
				ret[ b.Info.Name ].Add( b );
			return ret;
		}

		public bool CanBuild( NewUnitInfo info, Player player, Cache<string, List<Actor>> playerBuildings )
		{
			var bi = info.Traits.GetOrDefault<BuildableInfo>();
			if( bi == null ) return false;

			if( !bi.Owner.Any( x => x == player.Race ) )
				return false;

			foreach( var p in bi.Prerequisites )
				if( playerBuildings[ p ].Count == 0 )
					return false;

			if( producesIndex[ Rules.UnitCategory[ info.Name ] ].All( x => playerBuildings[ x.Name ].Count == 0 ) )
				return false;

			return true;
		} 

		public IEnumerable<string> BuildableItems( Player player, params string[] categories )
		{
			var playerBuildings = GatherBuildings( player );
			foreach( var unit in categories.SelectMany( x => Rules.Categories[ x ] ).Select( x => Rules.NewUnitInfo[ x ] ) )
				if( CanBuild( unit, player, playerBuildings ) )
					yield return unit.Name;
		}

		public IEnumerable<string> AllItems(Player player, params string[] categories)
		{
			return categories.SelectMany(x => Rules.Categories[x]).Select(x => Rules.NewUnitInfo[x].Name);
		}

		public IEnumerable<NewUnitInfo> UnitBuiltAt( NewUnitInfo info )
		{
			var builtAt = info.Traits.Get<BuildableInfo>().BuiltAt;
			if( builtAt.Length != 0 )
				return builtAt.Select( x => Rules.NewUnitInfo[ x.ToLowerInvariant() ] );
			else
				return producesIndex[ Rules.UnitCategory[ info.Name ] ];
		}
	}
}
