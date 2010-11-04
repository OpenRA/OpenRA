#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.RA.Buildings
{
	public static class FootprintUtils
	{
		public static IEnumerable<int2> Tiles( string name, BuildingInfo buildingInfo, int2 topLeft )
		{
			var dim = buildingInfo.Dimensions;

			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x));
			
			if (Rules.Info[ name ].Traits.Contains<BibInfo>())
			{
				dim.Y += 1;
				footprint = footprint.Concat(new char[dim.X]);
			}

			return TilesWhere( name, dim, footprint.ToArray(), a => a != '_' ).Select( t => t + topLeft );
		}

		public static IEnumerable<int2> Tiles(Actor a)
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
