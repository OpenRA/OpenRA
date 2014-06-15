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

namespace OpenRA.Graphics
{
	public struct HSLColor
	{
		public readonly byte H;
		public readonly byte S;
		public readonly byte L;
		public readonly Color RGB;

		public static HSLColor FromHSV(float h, float s, float v)
		{
			var ll = 0.5f * (2 - s) * v;
			var ss = (ll >= 1 || v <= 0) ? 0 : 0.5f * s * v / (ll <= 0.5f ? ll : 1 - ll);
			return new HSLColor((byte)(255 * h), (byte)(255 * ss), (byte)(255 * ll));
		}

		public static HSLColor FromRGB(int r, int g, int b)
		{
			var c = Color.FromArgb(r, g, b);
			var h = (byte)((c.GetHue() / 360.0f) * 255);
			var s = (byte)(c.GetSaturation() * 255);
			var l = (byte)(c.GetBrightness() * 255);
			return new HSLColor(h, s, l);
		}

		public static Color RGBFromHSL(float h, float s, float l)
		{
			// Convert from HSL to RGB
			var q = (l < 0.5f) ? l * (1 + s) : l + s - (l * s);
			var p = 2 * l - q;

			float[] trgb = { h + 1 / 3.0f, h, h - 1 / 3.0f };
			float[] rgb = { 0, 0, 0 };

			for (var k = 0; k < 3; k++)
			{
				while (trgb[k] < 0) trgb[k] += 1.0f;
				while (trgb[k] > 1) trgb[k] -= 1.0f;
			}

			for (var k = 0; k < 3; k++)
			{
				if (trgb[k] < 1 / 6.0f) { rgb[k] = p + ((q - p) * 6 * trgb[k]); }
				else if (trgb[k] >= 1 / 6.0f && trgb[k] < 0.5) { rgb[k] = q; }
				else if (trgb[k] >= 0.5f && trgb[k] < 2.0f / 3) { rgb[k] = p + ((q - p) * 6 * (2.0f / 3 - trgb[k])); }
				else { rgb[k] = p; }
			}

			return Color.FromArgb((int)(rgb[0] * 255), (int)(rgb[1] * 255), (int)(rgb[2] * 255));
		}

		public static bool operator ==(HSLColor me, HSLColor other)
		{
			return me.H == other.H && me.S == other.S && me.L == other.L;
		}

		public static bool operator !=(HSLColor me, HSLColor other) { return !(me == other); }

		public HSLColor(byte h, byte s, byte l)
		{
			H = h;
			S = s;
			L = l;
			RGB = RGBFromHSL(H / 255f, S / 255f, L / 255f);
		}

		public void ToHSV(out float h, out float s, out float v)
		{
			var ll = 2 * L / 255f;
			var ss = S / 255f * ((ll <= 1) ? ll : 2 - ll);

			h = H / 255f;
			s = (2 * ss) / (ll + ss);
			v = (ll + ss) / 2;
		}

		public override string ToString()
		{
			return "{0},{1},{2}".F(H, S, L);
		}

		public override int GetHashCode() { return H.GetHashCode() ^ S.GetHashCode() ^ L.GetHashCode(); }

		public override bool Equals(object obj)
		{
			var o = obj as HSLColor?;
			return o != null && o == this;
		}
	}
}
