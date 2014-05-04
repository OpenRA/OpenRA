#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.RA.Buildings
{
	public static class FootprintUtils
	{
		public static IEnumerable<CPos> Tiles(MapRuleset rules, string name, BuildingInfo buildingInfo, CPos topLeft)
		{
			var dim = (CVec)buildingInfo.Dimensions;

			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x));

			var buildingTraits = rules.Actors[name].Traits;
			if (buildingTraits.Contains<BibInfo>() && !(buildingTraits.Get<BibInfo>().HasMinibib))
			{
				dim += new CVec(0, 1);
				footprint = footprint.Concat(new char[dim.X]);
			}

			return TilesWhere( name, dim, footprint.ToArray(), a => a != '_' ).Select( t => t + topLeft );
		}

		public static IEnumerable<CPos> Tiles(Actor a)
		{
			return Tiles(a.World.Map.Rules, a.Info.Name, a.Info.Traits.Get<BuildingInfo>(), a.Location);
		}

		public static IEnumerable<CPos> UnpathableTiles(string name, BuildingInfo buildingInfo, CPos position)
		{
			var footprint = buildingInfo.Footprint.Where( x => !char.IsWhiteSpace( x ) ).ToArray();
			foreach( var tile in TilesWhere( name, (CVec)buildingInfo.Dimensions, footprint, a => a == 'x' ) )
				yield return tile + position;
		}

		static IEnumerable<CVec> TilesWhere(string name, CVec dim, char[] footprint, Func<char, bool> cond)
		{
			if( footprint.Length != dim.X * dim.Y )
				throw new InvalidOperationException( "Invalid footprint for " + name );
			int index = 0;

			for( int y = 0 ; y < dim.Y ; y++ )
				for( int x = 0 ; x < dim.X ; x++ )
					if( cond( footprint[ index++ ] ) )
						yield return new CVec(x, y);
		}

		public static CVec AdjustForBuildingSize(BuildingInfo buildingInfo)
		{
			var dim = buildingInfo.Dimensions;
			return new CVec(dim.X / 2, dim.Y > 1 ? (dim.Y + 1) / 2 : 0);
		}

		public static WVec CenterOffset(BuildingInfo buildingInfo)
		{
			var dim = buildingInfo.Dimensions;
			// Offset is measured relative to the center of the cell, so need to subtract an additional half cell.
			return new CVec(dim.X, dim.Y).ToWVec() / 2 - new WVec(512, 512, 0);
		}
	}
}
