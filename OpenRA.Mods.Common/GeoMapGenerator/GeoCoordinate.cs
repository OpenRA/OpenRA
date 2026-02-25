#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.Common.GeoMapGenerator
{
	public readonly record struct LatLon(double Lat, double Lon);
	public readonly record struct UtmCoord(double Easting, double Northing, int ZoneNumber, char ZoneLetter);

	public readonly record struct MapBounds(
		double MinE, double MaxE,
		double MinN, double MaxN,
		int ZoneNumber, char ZoneLetter);

	/// <summary>
	/// Static coordinate math helpers for the geo map generator.
	/// </summary>
	public static class GeoMath
	{
		/// <summary>
		/// Compute UTM bounding box centered on a UTM coordinate.
		/// </summary>
		public static MapBounds ComputeBounds(UtmCoord center, int cells, double metersPerCell)
		{
			var totalM = cells * metersPerCell;
			var halfM = totalM / 2.0;

			return new MapBounds(
				MinE: center.Easting - halfM,
				MaxE: center.Easting + halfM,
				MinN: center.Northing - halfM,
				MaxN: center.Northing + halfM,
				ZoneNumber: center.ZoneNumber,
				ZoneLetter: center.ZoneLetter);
		}

		/// <summary>
		/// Compute the lat/lon bounding box from UTM map bounds (for Overpass queries).
		/// Returns (South, West, North, East) in degrees.
		/// </summary>
		public static (double South, double West, double North, double East)
			ComputeLatLonBbox(MapBounds bounds)
		{
			var (nwLat, nwLon) = UtmConverter.ToLatLon(bounds.MinE, bounds.MaxN, bounds.ZoneNumber, bounds.ZoneLetter);
			var (neLat, neLon) = UtmConverter.ToLatLon(bounds.MaxE, bounds.MaxN, bounds.ZoneNumber, bounds.ZoneLetter);
			var (seLat, seLon) = UtmConverter.ToLatLon(bounds.MaxE, bounds.MinN, bounds.ZoneNumber, bounds.ZoneLetter);
			var (swLat, swLon) = UtmConverter.ToLatLon(bounds.MinE, bounds.MinN, bounds.ZoneNumber, bounds.ZoneLetter);

			return (
				South: Math.Min(Math.Min(swLat, seLat), Math.Min(nwLat, neLat)),
				West: Math.Min(Math.Min(swLon, seLon), Math.Min(nwLon, neLon)),
				North: Math.Max(Math.Max(swLat, seLat), Math.Max(nwLat, neLat)),
				East: Math.Max(Math.Max(swLon, seLon), Math.Max(nwLon, neLon)));
		}

		/// <summary>
		/// Convert lat/lon to fractional cell coordinates (x, y).
		/// Origin is at (MinE, MaxN) — top-left corner. Y increases downward.
		/// Returns null if UTM zone mismatch.
		/// </summary>
		public static (double X, double Y)? LatLonToCell(double lat, double lon, MapBounds bounds, double mpc)
		{
			var (e, n, zn, _) = UtmConverter.FromLatLon(lat, lon);
			if (zn != bounds.ZoneNumber)
				return null;

			var x = (e - bounds.MinE) / mpc;
			var y = (bounds.MaxN - n) / mpc;
			return (x, y);
		}

		/// <summary>
		/// Convert grid cell index (column i, row j) to lat/lon of the cell center.
		/// </summary>
		public static LatLon CellToLatLon(int i, int j, MapBounds bounds, double mpc)
		{
			var e = bounds.MinE + (i + 0.5) * mpc;
			var n = bounds.MaxN - (j + 0.5) * mpc;
			var (lat, lon) = UtmConverter.ToLatLon(e, n, bounds.ZoneNumber, bounds.ZoneLetter);
			return new LatLon(lat, lon);
		}
	}
}
