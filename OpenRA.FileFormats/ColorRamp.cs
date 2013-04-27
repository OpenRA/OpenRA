#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;

namespace OpenRA.FileFormats
{
	public struct ColorRamp
	{
		public readonly HSLColor Color;
		public byte Ramp;

		public ColorRamp(HSLColor color, byte ramp)
		{
			Color = color;
			Ramp = ramp;
		}

		public ColorRamp(byte h, byte s, byte l, byte r)
		{
			Color = new HSLColor(h, s, l);
			Ramp = r;
		}

		/* returns a color along the Lum ramp */
		public Color GetColor(float t)
		{
			var l = float2.Lerp(Color.L, Color.L*Ramp/255f, t);
			return HSLColor.RGBFromHSL(Color.H/255f, Color.S/255f, l/255f);
		}

		public override string ToString()
		{
			return "{0},{1}".F(Color.ToString(), Ramp);
		}

		public static bool operator ==(ColorRamp me, ColorRamp other)
		{
			return (me.Color == other.Color && me.Ramp == other.Ramp);
		}

		public static bool operator !=(ColorRamp me, ColorRamp other) { return !(me == other); }

		public override int GetHashCode() { return Color.GetHashCode() ^ Ramp.GetHashCode(); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			ColorRamp o = (ColorRamp)obj;
			return o == this;
		}
	}
}
