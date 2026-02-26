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
using System.Globalization;

namespace OpenRA.Mods.Common.GeoMapGenerator
{
	/// <summary>
	/// Parses MGRS (Military Grid Reference System) strings into lat/lon
	/// using NGA 100km grid letter lookup tables.
	/// Format: zone-number + band-letter + 100km-col + 100km-row + easting + northing
	/// Example: "18STJ8690017000" → zone 18, band S, col T, row J, easting 86900, northing 17000
	/// </summary>
	public static class MgrsConverter
	{
		// The 100km column letters cycle through these characters (I and O excluded)
		// Set 1: zone % 6 == 1 → A-H, Set 2: zone % 6 == 2 → J-R, Set 3: zone % 6 == 0 → S-Z
		// (repeating pattern every 3 sets for the 6-zone groups)
		static readonly string ColumnLetterSets = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // 24 letters (no I, O)

		// Row letters cycle A-V (20 letters, no I, O) with offset depending on zone set
		static readonly string RowLetters = "ABCDEFGHJKLMNPQRSTUV"; // 20 letters (no I, O)

		/// <summary>
		/// Parse an MGRS string and return the corresponding lat/lon (WGS84, degrees).
		/// </summary>
		public static (double Lat, double Lon) ToLatLon(string mgrsString)
		{
			if (string.IsNullOrWhiteSpace(mgrsString))
				throw new ArgumentException("MGRS string is null or empty.", nameof(mgrsString));

			var s = mgrsString.Trim().ToUpperInvariant().Replace(" ", "");

			// Parse zone number (1 or 2 digits)
			var i = 0;
			while (i < s.Length && char.IsDigit(s[i]))
				i++;

			if (i == 0 || i > 2)
				throw new FormatException($"Invalid MGRS zone number in '{mgrsString}'.");

			var zoneNumber = int.Parse(s[..i], CultureInfo.InvariantCulture);
			if (zoneNumber < 1 || zoneNumber > 60)
				throw new FormatException($"MGRS zone number {zoneNumber} out of range 1-60.");

			// Parse band letter
			if (i >= s.Length || !char.IsLetter(s[i]))
				throw new FormatException($"Missing MGRS band letter in '{mgrsString}'.");

			var bandLetter = s[i++];

			// Parse 100km grid square (2 letters)
			if (i + 1 >= s.Length || !char.IsLetter(s[i]) || !char.IsLetter(s[i + 1]))
				throw new FormatException($"Missing 100km grid square letters in '{mgrsString}'.");

			var colLetter = s[i++];
			var rowLetter = s[i++];

			// Remaining digits: split evenly into easting and northing
			var digits = s[i..];
			if (digits.Length % 2 != 0)
				throw new FormatException($"MGRS coordinate digits must be even count, got {digits.Length}.");

			var half = digits.Length / 2;
			var eastingStr = digits[..half];
			var northingStr = digits[half..];

			// Scale to meters (5-digit precision = 1m)
			var precision = half;
			var easting = double.Parse(eastingStr, CultureInfo.InvariantCulture) * Math.Pow(10, 5 - precision);
			var northing = double.Parse(northingStr, CultureInfo.InvariantCulture) * Math.Pow(10, 5 - precision);

			// Add 100km grid square offsets
			easting += Get100kmEasting(colLetter, zoneNumber);
			northing += Get100kmNorthing(rowLetter, zoneNumber);

			// Adjust northing for the band letter to disambiguate the 2,000km row cycle
			northing = AdjustNorthingForBand(northing, bandLetter);

			return UtmConverter.ToLatLon(easting, northing, zoneNumber, bandLetter);
		}

		/// <summary>
		/// Get the 100km easting offset for a column letter and zone number.
		/// </summary>
		static double Get100kmEasting(char colLetter, int zoneNumber)
		{
			// Column letters cycle in sets of 8 depending on zone number
			// Zone set: (zoneNumber - 1) % 3 determines the starting column letter
			var setIndex = (zoneNumber - 1) % 3;
			var colIndex = ColumnLetterSets.IndexOf(colLetter);
			if (colIndex < 0)
				throw new FormatException($"Invalid 100km column letter '{colLetter}'.");

			// Each set starts at a different offset in the column letter sequence
			// Set 0 (zones 1,4,7,...): starts at A (index 0)
			// Set 1 (zones 2,5,8,...): starts at J (index 8)
			// Set 2 (zones 3,6,9,...): starts at S (index 16)
			var setStart = setIndex * 8;
			var relativeIndex = colIndex - setStart;
			if (relativeIndex < 0)
				relativeIndex += 24;

			// Column index 0 = 100000m, index 1 = 200000m, etc.
			return (relativeIndex + 1) * 100000.0;
		}

		/// <summary>
		/// Get the 100km northing offset for a row letter and zone number.
		/// </summary>
		static double Get100kmNorthing(char rowLetter, int zoneNumber)
		{
			var rowIndex = RowLetters.IndexOf(rowLetter);
			if (rowIndex < 0)
				throw new FormatException($"Invalid 100km row letter '{rowLetter}'.");

			// Row letters cycle every 2,000km (20 letters × 100km = 2000km)
			// Odd zones start at A, even zones start at F (index 5)
			var setOffset = zoneNumber % 2 == 0 ? 5 : 0;
			var adjustedIndex = (rowIndex - setOffset + 20) % 20;

			return adjustedIndex * 100000.0;
		}

		/// <summary>
		/// Adjust the northing value based on the band letter to resolve the 2,000km ambiguity.
		/// The row letters repeat every 2,000km, so we need to determine which
		/// 2,000km band the coordinate falls in based on the latitude band letter.
		/// </summary>
		static double AdjustNorthingForBand(double northing, char bandLetter)
		{
			// Approximate minimum northing for each band letter
			var minNorthing = GetMinNorthingForBand(bandLetter);

			// northing is currently 0-based within a 2,000km cycle
			// We need to add the right multiple of 2,000,000 to place it in the correct band
			const double cycle = 2000000.0;

			while (northing < minNorthing)
				northing += cycle;

			// Safety: don't overshoot (shouldn't happen with valid inputs)
			while (northing > minNorthing + cycle)
				northing -= cycle;

			return northing;
		}

		/// <summary>
		/// Get the approximate minimum northing for a given UTM band letter.
		/// Based on the latitude boundaries of each band.
		/// </summary>
		static double GetMinNorthingForBand(char band)
		{
			// Each band is 8° of latitude (except X which is 12°)
			// C starts at -80°, going up
			return band switch
			{
				'C' => 1100000.0,
				'D' => 2000000.0,
				'E' => 2800000.0,
				'F' => 3700000.0,
				'G' => 4600000.0,
				'H' => 5500000.0,
				'J' => 6400000.0,
				'K' => 7300000.0,
				'L' => 8200000.0,
				'M' => 9100000.0,
				'N' => 0.0,
				'P' => 800000.0,
				'Q' => 1700000.0,
				'R' => 2600000.0,
				'S' => 3500000.0,
				'T' => 4400000.0,
				'U' => 5300000.0,
				'V' => 6200000.0,
				'W' => 7000000.0,
				'X' => 7900000.0,
				_ => throw new FormatException($"Invalid UTM band letter '{band}'."),
			};
		}
	}
}
