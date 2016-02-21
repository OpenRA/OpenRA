#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using Eluant;
using OpenRA.Graphics;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting.Global
{
	[ScriptGlobal("HSLColor")]
	public class HSLColorGlobal : ScriptGlobal
	{
		public HSLColorGlobal(ScriptContext context)
			: base(context) { }

		[Desc("Create a new HSL color with the specified hue/saturation/luminosity.")]
		public HSLColor New(int hue, int saturation, int luminosity)
		{
			var h = (byte)hue.Clamp(0, 255);
			var s = (byte)saturation.Clamp(0, 255);
			var l = (byte)luminosity.Clamp(0, 255);

			return new HSLColor(h, s, l);
		}

		[Desc("Create a new HSL color with the specified red/green/blue/[alpha] values.")]
		public HSLColor FromRGB(int red, int green, int blue, int alpha = 255)
		{
			return new HSLColor(
				Color.FromArgb(
					alpha.Clamp(0, 255), red.Clamp(0, 255), green.Clamp(0, 255), blue.Clamp(0, 255)));
		}

		[Desc("Create a new HSL color with the specified red/green/blue/[alpha] hex string (rrggbb[aa]).")]
		public HSLColor FromHex(string value)
		{
			Color rgb;
			if (HSLColor.TryParseRGB(value, out rgb))
				return new HSLColor(rgb);

			throw new LuaException("Invalid rrggbb[aa] hex string.");
		}

		public HSLColor Aqua { get { return new HSLColor(Color.Aqua); } }
		public HSLColor Black { get { return new HSLColor(Color.Black); } }
		public HSLColor Blue { get { return new HSLColor(Color.Blue); } }
		public HSLColor Brown { get { return new HSLColor(Color.Brown); } }
		public HSLColor Cyan { get { return new HSLColor(Color.Cyan); } }
		public HSLColor DarkBlue { get { return new HSLColor(Color.DarkBlue); } }
		public HSLColor DarkCyan { get { return new HSLColor(Color.DarkCyan); } }
		public HSLColor DarkGray { get { return new HSLColor(Color.DarkGray); } }
		public HSLColor DarkGreen { get { return new HSLColor(Color.DarkGreen); } }
		public HSLColor DarkOrange { get { return new HSLColor(Color.DarkOrange); } }
		public HSLColor DarkRed { get { return new HSLColor(Color.DarkRed); } }
		public HSLColor Fuchsia { get { return new HSLColor(Color.Fuchsia); } }
		public HSLColor Gold { get { return new HSLColor(Color.Gold); } }
		public HSLColor Gray { get { return new HSLColor(Color.Gray); } }
		public HSLColor Green { get { return new HSLColor(Color.Green); } }
		public HSLColor LawnGreen { get { return new HSLColor(Color.LawnGreen); } }
		public HSLColor LightBlue { get { return new HSLColor(Color.LightBlue); } }
		public HSLColor LightCyan { get { return new HSLColor(Color.LightCyan); } }
		public HSLColor LightGray { get { return new HSLColor(Color.LightGray); } }
		public HSLColor LightGreen { get { return new HSLColor(Color.LightGreen); } }
		public HSLColor LightYellow { get { return new HSLColor(Color.LightYellow); } }
		public HSLColor Lime { get { return new HSLColor(Color.Lime); } }
		public HSLColor LimeGreen { get { return new HSLColor(Color.LimeGreen); } }
		public HSLColor Magenta { get { return new HSLColor(Color.Magenta); } }
		public HSLColor Maroon { get { return new HSLColor(Color.Maroon); } }
		public HSLColor Navy { get { return new HSLColor(Color.Navy); } }
		public HSLColor Olive { get { return new HSLColor(Color.Olive); } }
		public HSLColor Orange { get { return new HSLColor(Color.Orange); } }
		public HSLColor OrangeRed { get { return new HSLColor(Color.OrangeRed); } }
		public HSLColor Purple { get { return new HSLColor(Color.Purple); } }
		public HSLColor Red { get { return new HSLColor(Color.Red); } }
		public HSLColor Salmon { get { return new HSLColor(Color.Salmon); } }
		public HSLColor SkyBlue { get { return new HSLColor(Color.SkyBlue); } }
		public HSLColor Teal { get { return new HSLColor(Color.Teal); } }
		public HSLColor Yellow { get { return new HSLColor(Color.Yellow); } }
		public HSLColor White { get { return new HSLColor(Color.White); } }
	}
}
