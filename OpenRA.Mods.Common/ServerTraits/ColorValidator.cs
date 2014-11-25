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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class ColorValidator : ServerTrait, IClientJoined
	{
		// The bigger the color threshold, the less permitive is the algorithm
		const int ColorThreshold = 0x40;
		const byte ColorLowerBound = 0x33;
		const byte ColorHigherBound = 0xFF;

		static bool ValidateColorAgainstForbidden(Color askedColor, IEnumerable<Color> forbiddenColors, out Color forbiddenColor)
		{
			var blockingColors =
				forbiddenColors
					.Where(playerColor => GetColorDelta(askedColor, playerColor) < ColorThreshold)
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

		public static Color? GetColorAlternative(Color askedColor, Color forbiddenColor)
		{
			Color? color = null;

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
				vectorMax = 1;	// Avoid divison by 0

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

			var ii = 1;
			var alternativeColor = new int[3];

			do
			{
				// If we reached the limit (The ii >= 255 prevents too much calculations)
				if ((alternativeColor[0] == ColorLowerBound && alternativeColor[1] == ColorLowerBound && alternativeColor[2] == ColorLowerBound)
					|| (alternativeColor[0] == ColorHigherBound && alternativeColor[1] == ColorHigherBound && alternativeColor[2] == ColorHigherBound)
					|| ii >= 255)
				{
					color = null;
					break;
				}

				// Apply vector to forbidden color
				alternativeColor[0] = forbiddenColor.R + (int)(vector[0] * weightVector[0] * ii);
				alternativeColor[1] = forbiddenColor.G + (int)(vector[1] * weightVector[1] * ii);
				alternativeColor[2] = forbiddenColor.B + (int)(vector[2] * weightVector[2] * ii);

				// Be sure it doesnt go out of bounds (0x33 is the lower limit for HSL picker)
				alternativeColor[0] = alternativeColor[0].Clamp(ColorLowerBound, ColorHigherBound);
				alternativeColor[1] = alternativeColor[1].Clamp(ColorLowerBound, ColorHigherBound);
				alternativeColor[2] = alternativeColor[2].Clamp(ColorLowerBound, ColorHigherBound);

				// Get the alternative color attempt
				color = Color.FromArgb(alternativeColor[0], alternativeColor[1], alternativeColor[2]);

				++ii;
			} while (GetColorDelta(color.Value, forbiddenColor) < ColorThreshold);

			return color;
		}

		public static double GetColorDelta(Color colorA, Color colorB)
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

		public static HSLColor ValidatePlayerColorAndGetAlternative(S server, HSLColor askedColor, int playerIndex, Connection connectionToEcho = null)
		{
			var askColor = askedColor;

			Color invalidColor;
			if (!ValidatePlayerNewColor(server, askColor.RGB, playerIndex, out invalidColor, connectionToEcho))
			{
				var altColor = GetColorAlternative(askColor.RGB, invalidColor);
				if (altColor == null || !ValidatePlayerNewColor(server, altColor.Value, playerIndex))
				{
					// Pick a random color
					do
					{
						var hue = (byte)server.Random.Next(255);
						var sat = (byte)server.Random.Next(255);
						var lum = (byte)server.Random.Next(51, 255);
						askColor = new HSLColor(hue, sat, lum);
					} while (!ValidatePlayerNewColor(server, askColor.RGB, playerIndex));
				}
				else
					askColor = HSLColor.FromRGB(altColor.Value.R, altColor.Value.G, altColor.Value.B);
			}

			return askColor;
		}

		public static bool ValidatePlayerNewColor(S server, Color askedColor, int playerIndex, out Color forbiddenColor, Connection connectionToEcho = null)
		{
			// Validate color against the current map tileset
			var tileset = server.Map.Rules.TileSets[server.Map.Tileset];
			var forbiddenColors = tileset.TerrainInfo.Select(terrainInfo => terrainInfo.Color);

			if (!ValidateColorAgainstForbidden(askedColor, forbiddenColors, out forbiddenColor))
			{
				if (connectionToEcho != null)
					server.SendOrderTo(connectionToEcho, OrderCode.Message, "Color was too similar to the terrain, and has been adjusted.");

				return false;
			}

			// Validate color against other clients
			var playerColors = server.LobbyInfo.Clients
				.Where(c => c.Index != playerIndex)
				.ToDictionary(c => c.Color.RGB, c => c.Name);

			if (!ValidateColorAgainstForbidden(askedColor, playerColors.Keys, out forbiddenColor))
			{
				if (connectionToEcho != null)
				{
					var client = playerColors[forbiddenColor];
					server.SendOrderTo(connectionToEcho, OrderCode.Message, "Color was too similar to {0}, and has been adjusted.".F(client));
				}

				return false;
			}

			var mapPlayerColors = server.Map.Players.Values
				.Select(p => p.ColorRamp.RGB);

			if (!ValidateColorAgainstForbidden(askedColor, mapPlayerColors, out forbiddenColor))
			{
				if (connectionToEcho != null)
					server.SendOrderTo(connectionToEcho, OrderCode.Message, "Color was too similar to a non-combatant player, and has been adjusted.");

				return false;
			}

			// Color is valid
			forbiddenColor = default(Color);

			return true;
		}

		public static bool ValidatePlayerNewColor(S server, Color askedColor, int playerIndex, Connection connectionToEcho = null)
		{
			Color forbiddenColor;
			return ValidatePlayerNewColor(server, askedColor, playerIndex, out forbiddenColor, connectionToEcho);
		}

		#region IClientJoined

		public void ClientJoined(S server, Connection conn)
		{
			var client = server.GetClient(conn);

			// Validate whether color is allowed and get an alternative if it isn't
			if (client.Slot == null ||!server.LobbyInfo.Slots[client.Slot].LockColor)
				client.Color = ColorValidator.ValidatePlayerColorAndGetAlternative(server, client.Color, client.Index);
		}

		#endregion
	}
}
