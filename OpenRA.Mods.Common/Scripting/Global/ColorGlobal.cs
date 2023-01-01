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

		public Color Aqua => Color.Aqua;
		public Color Black => Color.Black;
		public Color Blue => Color.Blue;
		public Color Brown => Color.Brown;
		public Color Cyan => Color.Cyan;
		public Color DarkBlue => Color.DarkBlue;
		public Color DarkCyan => Color.DarkCyan;
		public Color DarkGray => Color.DarkGray;
		public Color DarkGreen => Color.DarkGreen;
		public Color DarkOrange => Color.DarkOrange;
		public Color DarkRed => Color.DarkRed;
		public Color Fuchsia => Color.Fuchsia;
		public Color Gold => Color.Gold;
		public Color Gray => Color.Gray;
		public Color Green => Color.Green;
		public Color LawnGreen => Color.LawnGreen;
		public Color LightBlue => Color.LightBlue;
		public Color LightCyan => Color.LightCyan;
		public Color LightGray => Color.LightGray;
		public Color LightGreen => Color.LightGreen;
		public Color LightYellow => Color.LightYellow;
		public Color Lime => Color.Lime;
		public Color LimeGreen => Color.LimeGreen;
		public Color Magenta => Color.Magenta;
		public Color Maroon => Color.Maroon;
		public Color Navy => Color.Navy;
		public Color Olive => Color.Olive;
		public Color Orange => Color.Orange;
		public Color OrangeRed => Color.OrangeRed;
		public Color Purple => Color.Purple;
		public Color Red => Color.Red;
		public Color Salmon => Color.Salmon;
		public Color SkyBlue => Color.SkyBlue;
		public Color Teal => Color.Teal;
		public Color Yellow => Color.Yellow;
		public Color White => Color.White;
	}
}
