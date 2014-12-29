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
	public enum FootprintType
	{
		Empty,
		Unpathable,
		Pathable
	}

	public static class FootprintUtils
	{
		public static Dictionary<char, FootprintType> CharToFootprint = new Dictionary<char, FootprintType>()
		{
			{ '_', FootprintType.Empty },
			{ 'x', FootprintType.Unpathable },
			{ '=', FootprintType.Pathable }
		};

		public static Dictionary<FootprintType, char> FootprintToChar = CharToFootprint.ReverseKeyValues();

		public static IEnumerable<CPos> TilesOfType(FootprintType type, string buildingName, BuildingInfo buildingInfo, CPos topLeft)
		{
			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x)).ToArray();
			var dim = (CVec)buildingInfo.Dimensions;

			var validTiles = TilesWhere(buildingName, dim, footprint, t => CharToFootprint[t] == type);
			foreach (var tile in validTiles)
				yield return tile + topLeft;
		}

		public static IEnumerable<CPos> Tiles(Ruleset rules, string name, BuildingInfo buildingInfo, CPos topLeft)
		{
			var dim = (CVec)buildingInfo.Dimensions;

			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x));

			var buildingTraits = rules.Actors[name].Traits;
			if (buildingTraits.Contains<BibInfo>() && !buildingTraits.Get<BibInfo>().HasMinibib)
			{
				dim += new CVec(0, 1);

				// Add pathable bib tiles to the footprint
				var bibFootprint = new string(FootprintToChar[FootprintType.Pathable], dim.X).ToCharArray();
				footprint = footprint.Concat(bibFootprint);
			}

			return TilesWhere(name, dim, footprint.ToArray(), t => CharToFootprint[t] != FootprintType.Empty).Select(t => t + topLeft);
		}

		public static IEnumerable<CPos> Tiles(Actor a)
		{
			return Tiles(a.World.Map.Rules, a.Info.Name, a.Info.Traits.Get<BuildingInfo>(), a.Location);
		}

		static IEnumerable<CVec> TilesWhere(string name, CVec dim, char[] footprint, Func<char, bool> cond)
		{
			if (footprint.Length != dim.X * dim.Y)
				throw new InvalidOperationException("Invalid footprint for " + name);
			var index = 0;

			for (var y = 0; y < dim.Y; y++)
				for (var x = 0; x < dim.X; x++)
					if (cond(footprint[index++]))
						yield return new CVec(x, y);
		}

		public static CVec AdjustForBuildingSize(BuildingInfo buildingInfo)
		{
			var dim = buildingInfo.Dimensions;
			return new CVec(dim.X / 2, dim.Y > 1 ? (dim.Y + 1) / 2 : 0);
		}

		public static WVec CenterOffset(World w, BuildingInfo buildingInfo)
		{
			var dim = buildingInfo.Dimensions;
			return (w.Map.CenterOfCell(CPos.Zero + new CVec(dim.X, dim.Y)) - w.Map.CenterOfCell(new CPos(1, 1))) / 2;
		}
	}
}
