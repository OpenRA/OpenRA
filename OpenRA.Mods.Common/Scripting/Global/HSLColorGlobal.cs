#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
	}
}
