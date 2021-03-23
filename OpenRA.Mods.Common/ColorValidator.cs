#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.Common
{
	public class ColorValidator : IGlobalModData
	{
		// The bigger the color threshold, the less permissive is the algorithm
		public readonly int Threshold = 0x50;
		public readonly float[] HsvSaturationRange = new[] { 0.25f, 1f };
		public readonly float[] HsvValueRange = new[] { 0.2f, 1.0f };
		public readonly Color[] TeamColorPresets = { };

		double GetColorDelta(Color colorA, Color colorB)
		{
			var rmean = (colorA.R + colorB.R) / 2.0;
			var r = colorA.R - colorB.R;
			var g = colorA.G - colorB.G;
			var b = colorA.B - colorB.B;
			var weightR = 2.0 + rmean / 256;
			var weightG = 4.0;
			var weightB = 2.0 + (255 - rmean) / 256;
			return Math.Sqrt(weightR * r * r + weightG * g * g + weightB * b * b);
		}

		bool IsValid(Color askedColor, IEnumerable<Color> forbiddenColors, out Color forbiddenColor)
		{
			var blockingColors = forbiddenColors
				.Where(playerColor => GetColorDelta(askedColor, playerColor) < Threshold)
				.Select(playerColor => new { Delta = GetColorDelta(askedColor, playerColor), Color = playerColor });

			// Return the player that holds with the lowest difference
			if (blockingColors.Any())
			{
				forbiddenColor = blockingColors.MinBy(aa => aa.Delta).Color;
				return false;
			}

			forbiddenColor = default(Color);
			return true;
		}

		public bool IsValid(Color askedColor, out Color forbiddenColor, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors, HashSet<string> errorMessages = null)
		{
			// Validate color against HSV
			askedColor.ToAhsv(out _, out _, out var s, out var v);
			if (s < HsvSaturationRange[0] || s > HsvSaturationRange[1] || v < HsvValueRange[0] || v > HsvValueRange[1])
			{
				errorMessages?.Add("Color was adjusted to be inside the allowed range.");
				forbiddenColor = askedColor;
				return false;
			}

			// Validate color against the current map tileset
			if (!IsValid(askedColor, terrainColors, out forbiddenColor))
			{
				errorMessages?.Add("Color was adjusted to be less similar to the terrain.");
				return false;
			}

			// Validate color against other clients
			if (!IsValid(askedColor, playerColors, out forbiddenColor))
			{
				errorMessages?.Add("Color was adjusted to be less similar to another player.");
				return false;
			}

			// Color is valid
			forbiddenColor = default(Color);

			return true;
		}

		public Color RandomPresetColor(MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors)
		{
			if (TeamColorPresets.Any())
			{
				foreach (var c in TeamColorPresets.Shuffle(random))
					if (IsValid(c, out _, terrainColors, playerColors))
						return c;
			}

			return RandomValidColor(random, terrainColors, playerColors);
		}

		public Color RandomValidColor(MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors)
		{
			Color color;
			do
			{
				var h = random.Next(255) / 255f;
				var s = float2.Lerp(HsvSaturationRange[0], HsvSaturationRange[1], random.NextFloat());
				var v = float2.Lerp(HsvValueRange[0], HsvValueRange[1], random.NextFloat());
				color = Color.FromAhsv(h, s, v);
			}
			while (!IsValid(color, out _, terrainColors, playerColors));

			return color;
		}

		public Color MakeValid(Color askedColor, MersenneTwister random, IEnumerable<Color> terrainColors, IEnumerable<Color> playerColors, Action<string> onError)
		{
			var errorMessages = new HashSet<string>();

			if (IsValid(askedColor, out var forbiddenColor, terrainColors, playerColors, errorMessages))
				return askedColor;

			// Vector between the 2 colors
			var vector = new double[]
			{
				askedColor.R - forbiddenColor.R,
				askedColor.G - forbiddenColor.G,
				askedColor.B - forbiddenColor.B
			};

			// Reduce vector by it's biggest value (more calculations, but more accuracy too)
			var vectorMax = vector.Max(vv => Math.Abs(vv));
			if (vectorMax == 0)
			{
				vectorMax = 1;  // Avoid division by 0

				// Create a tiny vector to make the while loop maths work
				vector[0] = 1;
				vector[1] = 1;
				vector[2] = 1;
			}

			vector[0] /= vectorMax;
			vector[1] /= vectorMax;
			vector[2] /= vectorMax;

			// Color weights
			var rmean = (double)(askedColor.R + forbiddenColor.R) / 2;
			var weightVector = new[]
			{
				2.0 + rmean / 256,
				4.0,
				2.0 + (255 - rmean) / 256,
			};

			var attempt = 1;
			Color color;
			do
			{
				// If we reached the limit (The ii >= 255 prevents too much calculations)
				if (attempt >= 255)
				{
					color = RandomPresetColor(random, terrainColors, playerColors);
					errorMessages.Add("Color could not be adjusted enough, a new color has been picked.");
					break;
				}

				// Apply vector to forbidden color
				var r = (forbiddenColor.R + (int)(vector[0] * weightVector[0] * attempt)).Clamp(0, 255);
				var g = (forbiddenColor.G + (int)(vector[1] * weightVector[1] * attempt)).Clamp(0, 255);
				var b = (forbiddenColor.B + (int)(vector[2] * weightVector[2] * attempt)).Clamp(0, 255);

				// Get the alternative color attempt
				color = Color.FromArgb(r, g, b);

				attempt++;
			}
			while (!IsValid(color, out forbiddenColor, terrainColors, playerColors, errorMessages));

			// Coalesce the error messages to only print one of each type despite up to 255 iterations
			errorMessages.Do(onError);

			return color;
		}
	}
}
