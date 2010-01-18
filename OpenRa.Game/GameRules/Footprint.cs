using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Traits;

namespace OpenRa.GameRules
{
	static class Footprint
	{
		public static IEnumerable<int2> Tiles( string name, BuildingInfo buildingInfo, int2 position )
		{
			var dim = buildingInfo.Dimensions;

			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x));
			if (buildingInfo.Bib)
			{
				dim.Y += 1;
				footprint = footprint.Concat(new char[dim.X]);
			}

			return TilesWhere( name, dim, footprint.ToArray(), a => a != '_' ).Select( t => t + position );
		}

		public static IEnumerable<int2> Tiles(Actor a, Traits.Building building)
		{
			return Tiles( a.Info.Name, a.Info.Traits.Get<BuildingInfo>(), a.Location );
		}

		public static IEnumerable<int2> UnpathableTiles( string name, BuildingInfo buildingInfo, int2 position )
		{
			var footprint = buildingInfo.Footprint.Where( x => !char.IsWhiteSpace( x ) ).ToArray();
			foreach( var tile in TilesWhere( name, buildingInfo.Dimensions, footprint, a => a == 'x' ) )
				yield return tile + position;
		}

		static IEnumerable<int2> TilesWhere( string name, int2 dim, char[] footprint, Func<char, bool> cond )
		{
			if( footprint.Length != dim.X * dim.Y )
				throw new InvalidOperationException( "Invalid footprint for " + name );
			int index = 0;

			for( int y = 0 ; y < dim.Y ; y++ )
				for( int x = 0 ; x < dim.X ; x++ )
					if( cond( footprint[ index++ ] ) )
						yield return new int2( x, y );
		}

		public static int2 AdjustForBuildingSize( BuildingInfo buildingInfo )
		{
			var dim = buildingInfo.Dimensions;
			return new int2( dim.X / 2, dim.Y > 1 ? ( dim.Y + 1 ) / 2 : 0 );
		}
	}
}
