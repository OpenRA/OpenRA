#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
		public static Color New(int hue, int saturation, int luminosity)
		{
			var h = (byte)hue.Clamp(0, 255);
			var s = (byte)saturation.Clamp(0, 255);
			var l = (byte)luminosity.Clamp(0, 255);

			return Color.FromAhsl(255, h / 255f, s / 255f, l / 255f);
		}

		[Desc("Create a new color with the specified red/green/blue/[alpha] values.")]
		public static Color FromRGB(int red, int green, int blue, int alpha = 255)
		{
			return Color.FromArgb(
					alpha.Clamp(0, 255),
					red.Clamp(0, 255),
					green.Clamp(0, 255),
					blue.Clamp(0, 255));
		}

		[Desc("Create a new color with the specified red/green/blue/[alpha] hex string (rrggbb[aa]).")]
		public static Color FromHex(string value)
		{
			if (Color.TryParse(value, out var color))
				return color;

			throw new LuaException("Invalid rrggbb[aa] hex string.");
		}

		public static Color Aqua => Color.Aqua;
		public static Color Black => Color.Black;
		public static Color Blue => Color.Blue;
		public static Color Brown => Color.Brown;
		public static Color Cyan => Color.Cyan;
		public static Color DarkBlue => Color.DarkBlue;
		public static  Color DarkCyan => Color.DarkCyan;
		public static Color DarkGray => Color.DarkGray;
		public static Color DarkGreen => Color.DarkGreen;
		public static Color DarkOrange => Color.DarkOrange;
		public static Color DarkRed => Color.DarkRed;
		public static Color Fuchsia => Color.Fuchsia;
		public static Color Gold => Color.Gold;
		public static Color Gray => Color.Gray;
		public static Color Green => Color.Green;
		public static Color LawnGreen => Color.LawnGreen;
		public static Color LightBlue => Color.LightBlue;
		public static Color LightCyan => Color.LightCyan;
		public static Color LightGray => Color.LightGray;
		public static Color LightGreen => Color.LightGreen;
		public static Color LightYellow => Color.LightYellow;
		public static Color Lime => Color.Lime;
		public static Color LimeGreen => Color.LimeGreen;
		public static Color Magenta => Color.Magenta;
		public static Color Maroon => Color.Maroon;
		public static Color Navy => Color.Navy;
		public static Color Olive => Color.Olive;
		public static Color Orange => Color.Orange;
		public static Color OrangeRed => Color.OrangeRed;
		public static Color Purple => Color.Purple;
		public static Color Red => Color.Red;
		public static Color Salmon => Color.Salmon;
		public static Color SkyBlue => Color.SkyBlue;
		public static Color Teal => Color.Teal;
		public static Color Yellow => Color.Yellow;
		public static Color White => Color.White;
	}
}
