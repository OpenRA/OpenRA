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
		public byte H,S,L,R;

		public ColorRamp(byte h, byte s, byte l, byte r)
		{
			H = h; S = s; L = l; R = r;
		}

		/* returns a color along the Lum ramp */
		public Color GetColor( float t )
		{
			return ColorFromHSL( H / 255f, S / 255f, float2.Lerp( L / 255f, L*R / (255f * 255f), t ) );
		}

		public override string ToString()
		{
			return "{0},{1},{2},{3}".F(H, S, L, R);
		}

		// hk is hue in the range [0,1] instead of [0,360]
		static Color ColorFromHSL(float hk, float s, float l)
		{
			// Convert from HSL to RGB
			var q = (l < 0.5f) ? l * (1 + s) : l + s - (l * s);
			var p = 2 * l - q;

			float[] trgb = { hk + 1 / 3.0f, hk, hk - 1/3.0f };
			float[] rgb = { 0, 0, 0 };

			for (int k = 0; k < 3; k++)
			{
				while (trgb[k] < 0) trgb[k] += 1.0f;
				while (trgb[k] > 1) trgb[k] -= 1.0f;
			}

			for (int k = 0; k < 3; k++)
			{
				if (trgb[k] < 1 / 6.0f) { rgb[k] = (p + ((q - p) * 6 * trgb[k])); }
				else if (trgb[k] >= 1 / 6.0f && trgb[k] < 0.5) { rgb[k] = q; }
				else if (trgb[k] >= 0.5f && trgb[k] < 2.0f / 3) { rgb[k] = (p + ((q - p) * 6 * (2.0f / 3 - trgb[k]))); }
				else { rgb[k] = p; }
			}

			return Color.FromArgb((int)(rgb[0] * 255), (int)(rgb[1] * 255), (int)(rgb[2] * 255));
		}

		public static bool operator ==(ColorRamp me, ColorRamp other)
		{
			return (me.H == other.H && me.S == other.S && me.L == other.L && me.R == other.R );
		}

		public static bool operator !=(ColorRamp me, ColorRamp other) { return !(me == other); }

		public override int GetHashCode() { return H.GetHashCode() ^ S.GetHashCode() ^ L.GetHashCode() ^ R.GetHashCode(); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			ColorRamp o = (ColorRamp)obj;
			return o == this;
		}
	}
}
