using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.GameRules
{
	static class Footprint
	{
		public static IEnumerable<int2> Tiles( BuildingInfo buildingInfo, int2 position )
		{
			return Tiles(buildingInfo, position, true);
		}

		public static IEnumerable<int2> Tiles( BuildingInfo buildingInfo, int2 position, bool adjustForPlacement )
		{
			var dim = buildingInfo.Dimensions;

			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x));
			if (buildingInfo.Bib)
			{
				dim.Y += 1;
				footprint = footprint.Concat(new char[dim.X]);
			}

			var adjustment = adjustForPlacement ? AdjustForBuildingSize(buildingInfo) : int2.Zero;

			var tiles = TilesWhere(buildingInfo.Name, dim, footprint.ToArray(), a => a != '_');
			return tiles.Select(t => t + position - adjustment);
		}

		public static IEnumerable<int2> Tiles(Actor a, Traits.Building building)
		{
			return Tiles( building.unitInfo, a.Location, false );
		}

		public static IEnumerable<int2> UnpathableTiles( BuildingInfo buildingInfo, int2 position )
		{
			var footprint = buildingInfo.Footprint.Where( x => !char.IsWhiteSpace( x ) ).ToArray();
			foreach( var tile in TilesWhere( buildingInfo.Name, buildingInfo.Dimensions, footprint, a => a == 'x' ) )
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

		public static int2 AdjustForBuildingSize( BuildingInfo unitInfo )
		{
			var dim = unitInfo.Dimensions;
			return new int2( dim.X / 2, dim.Y > 1 ? ( dim.Y + 1 ) / 2 : 0 );
		}
	}
}
