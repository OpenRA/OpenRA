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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	static class Footprint
	{
		public static IEnumerable<int2> Tiles( string name, BuildingInfo buildingInfo, int2 topLeft )
		{
			var dim = buildingInfo.Dimensions;

			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x));
			if (buildingInfo.Bib)
			{
				dim.Y += 1;
				footprint = footprint.Concat(new char[dim.X]);
			}

			return TilesWhere( name, dim, footprint.ToArray(), a => a != '_' ).Select( t => t + topLeft );
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
