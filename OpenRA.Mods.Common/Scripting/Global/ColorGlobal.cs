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

			return Color.FromAhsl(h, s, l);
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

		public Color Aqua { get { return Color.Aqua; } }
		public Color Black { get { return Color.Black; } }
		public Color Blue { get { return Color.Blue; } }
		public Color Brown { get { return Color.Brown; } }
		public Color Cyan { get { return Color.Cyan; } }
		public Color DarkBlue { get { return Color.DarkBlue; } }
		public Color DarkCyan { get { return Color.DarkCyan; } }
		public Color DarkGray { get { return Color.DarkGray; } }
		public Color DarkGreen { get { return Color.DarkGreen; } }
		public Color DarkOrange { get { return Color.DarkOrange; } }
		public Color DarkRed { get { return Color.DarkRed; } }
		public Color Fuchsia { get { return Color.Fuchsia; } }
		public Color Gold { get { return Color.Gold; } }
		public Color Gray { get { return Color.Gray; } }
		public Color Green { get { return Color.Green; } }
		public Color LawnGreen { get { return Color.LawnGreen; } }
		public Color LightBlue { get { return Color.LightBlue; } }
		public Color LightCyan { get { return Color.LightCyan; } }
		public Color LightGray { get { return Color.LightGray; } }
		public Color LightGreen { get { return Color.LightGreen; } }
		public Color LightYellow { get { return Color.LightYellow; } }
		public Color Lime { get { return Color.Lime; } }
		public Color LimeGreen { get { return Color.LimeGreen; } }
		public Color Magenta { get { return Color.Magenta; } }
		public Color Maroon { get { return Color.Maroon; } }
		public Color Navy { get { return Color.Navy; } }
		public Color Olive { get { return Color.Olive; } }
		public Color Orange { get { return Color.Orange; } }
		public Color OrangeRed { get { return Color.OrangeRed; } }
		public Color Purple { get { return Color.Purple; } }
		public Color Red { get { return Color.Red; } }
		public Color Salmon { get { return Color.Salmon; } }
		public Color SkyBlue { get { return Color.SkyBlue; } }
		public Color Teal { get { return Color.Teal; } }
		public Color Yellow { get { return Color.Yellow; } }
		public Color White { get { return Color.White; } }
	}
}
