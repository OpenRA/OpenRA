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

using Eluant;
using OpenRA.Primitives;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting.Global
{
	// Kept as HSLColor for backwards compatibility
	[ScriptGlobal("HSLColor")]
	public class ColorGlobal : ScriptGlobal
	{
		public ColorGlobal(ScriptContext context)
			: base(context) { }

		[Desc("Create a new color with the specified hue/saturation/luminosity.")]
		public Color New(int hue, int saturation, int luminosity)
		{
			var h = (byte)hue.Clamp(0, 255);
			var s = (byte)saturation.Clamp(0, 255);
			var l = (byte)luminosity.Clamp(0, 255);

			return Color.FromAhsl(255, h / 255f, s / 255f, l / 255f);
		}

		[Desc("Create a new color with the specified red/green/blue/[alpha] values.")]
		public Color FromRGB(int red, int green, int blue, int alpha = 255)
		{
			return Color.FromArgb(
					alpha.Clamp(0, 255),
					red.Clamp(0, 255),
					green.Clamp(0, 255),
					blue.Clamp(0, 255));
		}

		[Desc("Create a new color with the specified red/green/blue/[alpha] hex string (rrggbb[aa]).")]
		public Color FromHex(string value)
		{
			if (Color.TryParse(value, out var color))
				return color;

			throw new LuaException("Invalid rrggbb[aa] hex string.");
		}

		[Desc("FromHex(\"00FFFF\")")]
		public Color Aqua => Color.Aqua;
		[Desc("FromHex(\"000000\")")]
		public Color Black => Color.Black;
		[Desc("FromHex(\"0000FF\")")]
		public Color Blue => Color.Blue;
		[Desc("FromHex(\"A52A2A\")")]
		public Color Brown => Color.Brown;
		[Desc("FromHex(\"00FFFF\")")]
		public Color Cyan => Color.Cyan;
		[Desc("FromHex(\"00008B\")")]
		public Color DarkBlue => Color.DarkBlue;
		[Desc("FromHex(\"008B8B\")")]
		public Color DarkCyan => Color.DarkCyan;
		[Desc("FromHex(\"A9A9A9\")")]
		public Color DarkGray => Color.DarkGray;
		[Desc("FromHex(\"006400\")")]
		public Color DarkGreen => Color.DarkGreen;
		[Desc("FromHex(\"FF8C00\")")]
		public Color DarkOrange => Color.DarkOrange;
		[Desc("FromHex(\"8B0000\")")]
		public Color DarkRed => Color.DarkRed;
		[Desc("FromHex(\"FF00FF\")")]
		public Color Fuchsia => Color.Fuchsia;
		[Desc("FromHex(\"FFD700\")")]
		public Color Gold => Color.Gold;
		[Desc("FromHex(\"808080\")")]
		public Color Gray => Color.Gray;
		[Desc("FromHex(\"008000\")")]
		public Color Green => Color.Green;
		[Desc("FromHex(\"7CFC00\")")]
		public Color LawnGreen => Color.LawnGreen;
		[Desc("FromHex(\"ADD8E6\")")]
		public Color LightBlue => Color.LightBlue;
		[Desc("FromHex(\"E0FFFF\")")]
		public Color LightCyan => Color.LightCyan;
		[Desc("FromHex(\"D3D3D3\")")]
		public Color LightGray => Color.LightGray;
		[Desc("FromHex(\"90EE90\")")]
		public Color LightGreen => Color.LightGreen;
		[Desc("FromHex(\"FFFFE0\")")]
		public Color LightYellow => Color.LightYellow;
		[Desc("FromHex(\"00FF00\")")]
		public Color Lime => Color.Lime;
		[Desc("FromHex(\"32CD32\")")]
		public Color LimeGreen => Color.LimeGreen;
		[Desc("FromHex(\"FF00FF\")")]
		public Color Magenta => Color.Magenta;
		[Desc("FromHex(\"800000\")")]
		public Color Maroon => Color.Maroon;
		[Desc("FromHex(\"000080\")")]
		public Color Navy => Color.Navy;
		[Desc("FromHex(\"808000\")")]
		public Color Olive => Color.Olive;
		[Desc("FromHex(\"FFA500\")")]
		public Color Orange => Color.Orange;
		[Desc("FromHex(\"FF4500\")")]
		public Color OrangeRed => Color.OrangeRed;
		[Desc("FromHex(\"800080\")")]
		public Color Purple => Color.Purple;
		[Desc("FromHex(\"FF0000\")")]
		public Color Red => Color.Red;
		[Desc("FromHex(\"FA8072\")")]
		public Color Salmon => Color.Salmon;
		[Desc("FromHex(\"87CEEB\")")]
		public Color SkyBlue => Color.SkyBlue;
		[Desc("FromHex(\"008080\")")]
		public Color Teal => Color.Teal;
		[Desc("FromHex(\"FFFF00\")")]
		public Color Yellow => Color.Yellow;
		[Desc("FromHex(\"FFFFFF\")")]
		public Color White => Color.White;
	}
}
