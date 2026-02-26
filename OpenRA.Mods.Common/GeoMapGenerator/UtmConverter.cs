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
	/// <summary>
	/// Pure-math UTM ↔ lat/lon conversion using WGS84 ellipsoid constants
	/// and Transverse Mercator projection formulas.
	/// </summary>
	public static class UtmConverter
	{
		// WGS84 ellipsoid constants
		const double A = 6378137.0;             // semi-major axis (meters)
		const double F = 1.0 / 298.257223563;   // flattening
		const double B = A * (1.0 - F);         // semi-minor axis
		const double E2 = (A * A - B * B) / (A * A); // first eccentricity squared
		const double Ep2 = (A * A - B * B) / (B * B); // second eccentricity squared

		const double K0 = 0.9996;               // UTM scale factor
		const double FalseEasting = 500000.0;
		const double FalseNorthingSouth = 10000000.0;

		static readonly double EccSquared = E2;
		static readonly double EccPrimeSquared = Ep2;

		const double DegToRad = Math.PI / 180.0;
		const double RadToDeg = 180.0 / Math.PI;

		static readonly char[] ZoneLetters =
		{
			'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M',
			'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X'
		};

		/// <summary>
		/// Convert lat/lon (WGS84, degrees) to UTM.
		/// </summary>
		public static (double Easting, double Northing, int ZoneNumber, char ZoneLetter)
			FromLatLon(double latitude, double longitude)
		{
			if (latitude < -80.0 || latitude > 84.0)
				throw new ArgumentOutOfRangeException(nameof(latitude),
					"UTM is not defined for latitudes outside -80° to 84°.");

			var zoneNumber = GetZoneNumber(latitude, longitude);
			var zoneLetter = GetZoneLetter(latitude);

			var latRad = latitude * DegToRad;
			var lonRad = longitude * DegToRad;

			var lonOrigin = (zoneNumber - 1) * 6 - 180 + 3; // central meridian
			var lonOriginRad = lonOrigin * DegToRad;

			var n = A / Math.Sqrt(1 - EccSquared * Math.Sin(latRad) * Math.Sin(latRad));
			var t = Math.Tan(latRad) * Math.Tan(latRad);
			var c = EccPrimeSquared * Math.Cos(latRad) * Math.Cos(latRad);
			var a2 = Math.Cos(latRad) * (lonRad - lonOriginRad);

			var m = MeridianArc(latRad);

			var easting = K0 * n * (a2
				+ (1 - t + c) * a2 * a2 * a2 / 6
				+ (5 - 18 * t + t * t + 72 * c - 58 * EccPrimeSquared) * a2 * a2 * a2 * a2 * a2 / 120)
				+ FalseEasting;

			var northing = K0 * (m + n * Math.Tan(latRad) * (
				a2 * a2 / 2
				+ (5 - t + 9 * c + 4 * c * c) * a2 * a2 * a2 * a2 / 24
				+ (61 - 58 * t + t * t + 600 * c - 330 * EccPrimeSquared) * a2 * a2 * a2 * a2 * a2 * a2 / 720));

			if (latitude < 0)
				northing += FalseNorthingSouth;

			return (easting, northing, zoneNumber, zoneLetter);
		}

		/// <summary>
		/// Convert UTM to lat/lon (WGS84, degrees).
		/// </summary>
		public static (double Latitude, double Longitude)
			ToLatLon(double easting, double northing, int zoneNumber, char zoneLetter)
		{
			var northern = char.ToUpperInvariant(zoneLetter) >= 'N';

			var x = easting - FalseEasting;
			var y = northing;
			if (!northern)
				y -= FalseNorthingSouth;

			var lonOrigin = (zoneNumber - 1) * 6 - 180 + 3;

			var m = y / K0;
			var mu = m / (A * (1 - EccSquared / 4 - 3 * EccSquared * EccSquared / 64
				- 5 * EccSquared * EccSquared * EccSquared / 256));

			var e1 = (1 - Math.Sqrt(1 - EccSquared)) / (1 + Math.Sqrt(1 - EccSquared));

			var phi1 = mu
				+ (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
				+ (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
				+ (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu)
				+ (1097 * e1 * e1 * e1 * e1 / 512) * Math.Sin(8 * mu);

			var n1 = A / Math.Sqrt(1 - EccSquared * Math.Sin(phi1) * Math.Sin(phi1));
			var t1 = Math.Tan(phi1) * Math.Tan(phi1);
			var c1 = EccPrimeSquared * Math.Cos(phi1) * Math.Cos(phi1);
			var r1 = A * (1 - EccSquared) /
				Math.Pow(1 - EccSquared * Math.Sin(phi1) * Math.Sin(phi1), 1.5);
			var d = x / (n1 * K0);

			var lat = phi1
				- n1 * Math.Tan(phi1) / r1 * (
					d * d / 2
					- (5 + 3 * t1 + 10 * c1 - 4 * c1 * c1 - 9 * EccPrimeSquared) * d * d * d * d / 24
					+ (61 + 90 * t1 + 298 * c1 + 45 * t1 * t1 - 252 * EccPrimeSquared - 3 * c1 * c1)
						* d * d * d * d * d * d / 720);

			var lon = (d
				- (1 + 2 * t1 + c1) * d * d * d / 6
				+ (5 - 2 * c1 + 28 * t1 - 3 * c1 * c1 + 8 * EccPrimeSquared + 24 * t1 * t1)
					* d * d * d * d * d / 120)
				/ Math.Cos(phi1);

			return (lat * RadToDeg, lon * RadToDeg + lonOrigin);
		}

		/// <summary>
		/// Get the UTM zone number for a given latitude and longitude.
		/// Handles Norway and Svalbard exceptions.
		/// </summary>
		public static int GetZoneNumber(double latitude, double longitude)
		{
			// Normalize longitude to [-180, 180)
			var lon = longitude;
			while (lon >= 180) lon -= 360;
			while (lon < -180) lon += 360;

			var zone = (int)Math.Floor((lon + 180) / 6) + 1;

			// Norway exception
			if (latitude >= 56.0 && latitude < 64.0 && lon >= 3.0 && lon < 12.0)
				zone = 32;

			// Svalbard exceptions
			if (latitude >= 72.0 && latitude < 84.0)
			{
				if (lon >= 0.0 && lon < 9.0) zone = 31;
				else if (lon >= 9.0 && lon < 21.0) zone = 33;
				else if (lon >= 21.0 && lon < 33.0) zone = 35;
				else if (lon >= 33.0 && lon < 42.0) zone = 37;
			}

			return zone;
		}

		/// <summary>
		/// Get the UTM zone letter for a given latitude.
		/// </summary>
		public static char GetZoneLetter(double latitude)
		{
			if (latitude >= 84) return 'X';
			if (latitude < -80) return 'C';

			// Band letters from C (-80) to X (72-84), each 8° except X which is 12°
			var index = (int)Math.Floor((latitude + 80) / 8);
			if (index < 0) index = 0;
			if (index >= ZoneLetters.Length) index = ZoneLetters.Length - 1;
			return ZoneLetters[index];
		}

		static double MeridianArc(double latRad)
		{
			return A * (
				(1 - EccSquared / 4 - 3 * EccSquared * EccSquared / 64
					- 5 * EccSquared * EccSquared * EccSquared / 256) * latRad
				- (3 * EccSquared / 8 + 3 * EccSquared * EccSquared / 32
					+ 45 * EccSquared * EccSquared * EccSquared / 1024) * Math.Sin(2 * latRad)
				+ (15 * EccSquared * EccSquared / 256
					+ 45 * EccSquared * EccSquared * EccSquared / 1024) * Math.Sin(4 * latRad)
				- (35 * EccSquared * EccSquared * EccSquared / 3072) * Math.Sin(6 * latRad));
		}
	}
}
