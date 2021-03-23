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

using System;
using System.Globalization;
using OpenRA.Scripting;

namespace OpenRA.Primitives
{
	public readonly struct Color : IEquatable<Color>, IScriptBindable
	{
		readonly long argb;

		public static Color FromArgb(int red, int green, int blue)
		{
			return FromArgb(byte.MaxValue, red, green, blue);
		}

		public static Color FromArgb(int alpha, int red, int green, int blue)
		{
			return new Color(((byte)alpha << 24) + ((byte)red << 16) + ((byte)green << 8) + (byte)blue);
		}

		public static Color FromAhsl(int alpha, float h, float s, float l)
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
				if (trgb[k] < 1 / 6.0f)
					rgb[k] = p + ((q - p) * 6 * trgb[k]);
				else if (trgb[k] >= 1 / 6.0f && trgb[k] < 0.5)
					rgb[k] = q;
				else if (trgb[k] >= 0.5f && trgb[k] < 2.0f / 3)
					rgb[k] = p + ((q - p) * 6 * (2.0f / 3 - trgb[k]));
				else
					rgb[k] = p;
			}

			return FromArgb(alpha, (int)(rgb[0] * 255), (int)(rgb[1] * 255), (int)(rgb[2] * 255));
		}

		public static Color FromAhsl(int h, int s, int l)
		{
			return FromAhsl(255, h / 255f, s / 255f, l / 255f);
		}

		public static Color FromAhsl(float h, float s, float l)
		{
			return FromAhsl(255, h, s, l);
		}

		public static Color FromAhsv(int alpha, float h, float s, float v)
		{
			var ll = 0.5f * (2 - s) * v;
			var ss = (ll >= 1 || v <= 0) ? 0 : 0.5f * s * v / (ll <= 0.5f ? ll : 1 - ll);
			return FromAhsl(alpha, h, ss, ll);
		}

		public static Color FromAhsv(float h, float s, float v)
		{
			return FromAhsv(255, h, s, v);
		}

		public void ToAhsv(out int a, out float h, out float s, out float v)
		{
			var ll = 2 * GetBrightness();
			var ss = GetSaturation() * ((ll <= 1) ? ll : 2 - ll);

			a = A;
			h = GetHue() / 360f;
			s = (2 * ss) / (ll + ss);
			v = (ll + ss) / 2;
		}

		Color(long argb)
		{
			this.argb = argb;
		}

		public int ToArgb()
		{
			return (int)argb;
		}

		public static Color FromArgb(int alpha, Color baseColor)
		{
			return FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
		}

		public static Color FromArgb(int argb)
		{
			return FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
		}

		public static Color FromArgb(uint argb)
		{
			return FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
		}

		public static bool TryParse(string value, out Color color)
		{
			color = default(Color);
			value = value.Trim();
			if (value.Length != 6 && value.Length != 8)
				return false;

			byte alpha = 255;
			if (!byte.TryParse(value.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red)
			    || !byte.TryParse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green)
			    || !byte.TryParse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
				return false;

			if (value.Length == 8
			    && !byte.TryParse(value.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out alpha))
				return false;

			color = FromArgb(alpha, red, green, blue);
			return true;
		}

		public static bool operator ==(Color left, Color right)
		{
			return left.argb == right.argb;
		}

		public static bool operator !=(Color left, Color right)
		{
			return !(left == right);
		}

		public float GetBrightness()
		{
			var min = Math.Min(R, Math.Min(G, B));
			var max = Math.Max(R, Math.Max(G, B));
			return (max + min) / 510f;
		}

		public float GetSaturation()
		{
			var min = Math.Min(R, Math.Min(G, B));
			var max = Math.Max(R, Math.Max(G, B));
			if (max == min)
				return 0.0f;

			var sum = max + min;
			if (sum > byte.MaxValue)
				sum = 510 - sum;

			return (float)(max - min) / sum;
		}

		public float GetHue()
		{
			var min = Math.Min(R, Math.Min(G, B));
			var max = Math.Max(R, Math.Max(G, B));
			if (max == min)
				return 0.0f;

			var diff = (float)(max - min);
			var rNorm = (max - R) / diff;
			var gNorm = (max - G) / diff;
			var bNorm = (max - B) / diff;

			var hue = 0.0f;
			if (R == max)
				hue = 60.0f * (6.0f + bNorm - gNorm);
			if (G == max)
				hue = 60.0f * (2.0f + rNorm - bNorm);
			if (B == max)
				hue = 60.0f * (4.0f + gNorm - rNorm);

			if (hue > 360.0f)
				hue -= 360f;

			return hue;
		}

		public byte A => (byte)(argb >> 24);
		public byte R => (byte)(argb >> 16);
		public byte G => (byte)(argb >> 8);
		public byte B => (byte)argb;

		public bool Equals(Color other)
		{
			return this == other;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Color))
				return false;

			return this == (Color)obj;
		}

		public override int GetHashCode()
		{
			return (int)(argb ^ argb >> 32);
		}

		public override string ToString()
		{
			if (A == 255)
				return R.ToString("X2") + G.ToString("X2") + B.ToString("X2");

			return R.ToString("X2") + G.ToString("X2") + B.ToString("X2") + A.ToString("X2");
		}

		public static Color Transparent => FromArgb(0x00FFFFFF);
		public static Color AliceBlue => FromArgb(0xFFF0F8FF);
		public static Color AntiqueWhite => FromArgb(0xFFFAEBD7);
		public static Color Aqua => FromArgb(0xFF00FFFF);
		public static Color Aquamarine => FromArgb(0xFF7FFFD4);
		public static Color Azure => FromArgb(0xFFF0FFFF);
		public static Color Beige => FromArgb(0xFFF5F5DC);
		public static Color Bisque => FromArgb(0xFFFFE4C4);
		public static Color Black => FromArgb(0xFF000000);
		public static Color BlanchedAlmond => FromArgb(0xFFFFEBCD);
		public static Color Blue => FromArgb(0xFF0000FF);
		public static Color BlueViolet => FromArgb(0xFF8A2BE2);
		public static Color Brown => FromArgb(0xFFA52A2A);
		public static Color BurlyWood => FromArgb(0xFFDEB887);
		public static Color CadetBlue => FromArgb(0xFF5F9EA0);
		public static Color Chartreuse => FromArgb(0xFF7FFF00);
		public static Color Chocolate => FromArgb(0xFFD2691E);
		public static Color Coral => FromArgb(0xFFFF7F50);
		public static Color CornflowerBlue => FromArgb(0xFF6495ED);
		public static Color Cornsilk => FromArgb(0xFFFFF8DC);
		public static Color Crimson => FromArgb(0xFFDC143C);
		public static Color Cyan => FromArgb(0xFF00FFFF);
		public static Color DarkBlue => FromArgb(0xFF00008B);
		public static Color DarkCyan => FromArgb(0xFF008B8B);
		public static Color DarkGoldenrod => FromArgb(0xFFB8860B);
		public static Color DarkGray => FromArgb(0xFFA9A9A9);
		public static Color DarkGreen => FromArgb(0xFF006400);
		public static Color DarkKhaki => FromArgb(0xFFBDB76B);
		public static Color DarkMagenta => FromArgb(0xFF8B008B);
		public static Color DarkOliveGreen => FromArgb(0xFF556B2F);
		public static Color DarkOrange => FromArgb(0xFFFF8C00);
		public static Color DarkOrchid => FromArgb(0xFF9932CC);
		public static Color DarkRed => FromArgb(0xFF8B0000);
		public static Color DarkSalmon => FromArgb(0xFFE9967A);
		public static Color DarkSeaGreen => FromArgb(0xFF8FBC8B);
		public static Color DarkSlateBlue => FromArgb(0xFF483D8B);
		public static Color DarkSlateGray => FromArgb(0xFF2F4F4F);
		public static Color DarkTurquoise => FromArgb(0xFF00CED1);
		public static Color DarkViolet => FromArgb(0xFF9400D3);
		public static Color DeepPink => FromArgb(0xFFFF1493);
		public static Color DeepSkyBlue => FromArgb(0xFF00BFFF);
		public static Color DimGray => FromArgb(0xFF696969);
		public static Color DodgerBlue => FromArgb(0xFF1E90FF);
		public static Color Firebrick => FromArgb(0xFFB22222);
		public static Color FloralWhite => FromArgb(0xFFFFFAF0);
		public static Color ForestGreen => FromArgb(0xFF228B22);
		public static Color Fuchsia => FromArgb(0xFFFF00FF);
		public static Color Gainsboro => FromArgb(0xFFDCDCDC);
		public static Color GhostWhite => FromArgb(0xFFF8F8FF);
		public static Color Gold => FromArgb(0xFFFFD700);
		public static Color Goldenrod => FromArgb(0xFFDAA520);
		public static Color Gray => FromArgb(0xFF808080);
		public static Color Green => FromArgb(0xFF008000);
		public static Color GreenYellow => FromArgb(0xFFADFF2F);
		public static Color Honeydew => FromArgb(0xFFF0FFF0);
		public static Color HotPink => FromArgb(0xFFFF69B4);
		public static Color IndianRed => FromArgb(0xFFCD5C5C);
		public static Color Indigo => FromArgb(0xFF4B0082);
		public static Color Ivory => FromArgb(0xFFFFFFF0);
		public static Color Khaki => FromArgb(0xFFF0E68C);
		public static Color Lavender => FromArgb(0xFFE6E6FA);
		public static Color LavenderBlush => FromArgb(0xFFFFF0F5);
		public static Color LawnGreen => FromArgb(0xFF7CFC00);
		public static Color LemonChiffon => FromArgb(0xFFFFFACD);
		public static Color LightBlue => FromArgb(0xFFADD8E6);
		public static Color LightCoral => FromArgb(0xFFF08080);
		public static Color LightCyan => FromArgb(0xFFE0FFFF);
		public static Color LightGoldenrodYellow => FromArgb(0xFFFAFAD2);
		public static Color LightGray => FromArgb(0xFFD3D3D3);
		public static Color LightGreen => FromArgb(0xFF90EE90);
		public static Color LightPink => FromArgb(0xFFFFB6C1);
		public static Color LightSalmon => FromArgb(0xFFFFA07A);
		public static Color LightSeaGreen => FromArgb(0xFF20B2AA);
		public static Color LightSkyBlue => FromArgb(0xFF87CEFA);
		public static Color LightSlateGray => FromArgb(0xFF778899);
		public static Color LightSteelBlue => FromArgb(0xFFB0C4DE);
		public static Color LightYellow => FromArgb(0xFFFFFFE0);
		public static Color Lime => FromArgb(0xFF00FF00);
		public static Color LimeGreen => FromArgb(0xFF32CD32);
		public static Color Linen => FromArgb(0xFFFAF0E6);
		public static Color Magenta => FromArgb(0xFFFF00FF);
		public static Color Maroon => FromArgb(0xFF800000);
		public static Color MediumAquamarine => FromArgb(0xFF66CDAA);
		public static Color MediumBlue => FromArgb(0xFF0000CD);
		public static Color MediumOrchid => FromArgb(0xFFBA55D3);
		public static Color MediumPurple => FromArgb(0xFF9370DB);
		public static Color MediumSeaGreen => FromArgb(0xFF3CB371);
		public static Color MediumSlateBlue => FromArgb(0xFF7B68EE);
		public static Color MediumSpringGreen => FromArgb(0xFF00FA9A);
		public static Color MediumTurquoise => FromArgb(0xFF48D1CC);
		public static Color MediumVioletRed => FromArgb(0xFFC71585);
		public static Color MidnightBlue => FromArgb(0xFF191970);
		public static Color MintCream => FromArgb(0xFFF5FFFA);
		public static Color MistyRose => FromArgb(0xFFFFE4E1);
		public static Color Moccasin => FromArgb(0xFFFFE4B5);
		public static Color NavajoWhite => FromArgb(0xFFFFDEAD);
		public static Color Navy => FromArgb(0xFF000080);
		public static Color OldLace => FromArgb(0xFFFDF5E6);
		public static Color Olive => FromArgb(0xFF808000);
		public static Color OliveDrab => FromArgb(0xFF6B8E23);
		public static Color Orange => FromArgb(0xFFFFA500);
		public static Color OrangeRed => FromArgb(0xFFFF4500);
		public static Color Orchid => FromArgb(0xFFDA70D6);
		public static Color PaleGoldenrod => FromArgb(0xFFEEE8AA);
		public static Color PaleGreen => FromArgb(0xFF98FB98);
		public static Color PaleTurquoise => FromArgb(0xFFAFEEEE);
		public static Color PaleVioletRed => FromArgb(0xFFDB7093);
		public static Color PapayaWhip => FromArgb(0xFFFFEFD5);
		public static Color PeachPuff => FromArgb(0xFFFFDAB9);
		public static Color Peru => FromArgb(0xFFCD853F);
		public static Color Pink => FromArgb(0xFFFFC0CB);
		public static Color Plum => FromArgb(0xFFDDA0DD);
		public static Color PowderBlue => FromArgb(0xFFB0E0E6);
		public static Color Purple => FromArgb(0xFF800080);
		public static Color Red => FromArgb(0xFFFF0000);
		public static Color RosyBrown => FromArgb(0xFFBC8F8F);
		public static Color RoyalBlue => FromArgb(0xFF4169E1);
		public static Color SaddleBrown => FromArgb(0xFF8B4513);
		public static Color Salmon => FromArgb(0xFFFA8072);
		public static Color SandyBrown => FromArgb(0xFFF4A460);
		public static Color SeaGreen => FromArgb(0xFF2E8B57);
		public static Color SeaShell => FromArgb(0xFFFFF5EE);
		public static Color Sienna => FromArgb(0xFFA0522D);
		public static Color Silver => FromArgb(0xFFC0C0C0);
		public static Color SkyBlue => FromArgb(0xFF87CEEB);
		public static Color SlateBlue => FromArgb(0xFF6A5ACD);
		public static Color SlateGray => FromArgb(0xFF708090);
		public static Color Snow => FromArgb(0xFFFFFAFA);
		public static Color SpringGreen => FromArgb(0xFF00FF7F);
		public static Color SteelBlue => FromArgb(0xFF4682B4);
		public static Color Tan => FromArgb(0xFFD2B48C);
		public static Color Teal => FromArgb(0xFF008080);
		public static Color Thistle => FromArgb(0xFFD8BFD8);
		public static Color Tomato => FromArgb(0xFFFF6347);
		public static Color Turquoise => FromArgb(0xFF40E0D0);
		public static Color Violet => FromArgb(0xFFEE82EE);
		public static Color Wheat => FromArgb(0xFFF5DEB3);
		public static Color White => FromArgb(0xFFFFFFFF);
		public static Color WhiteSmoke => FromArgb(0xFFF5F5F5);
		public static Color Yellow => FromArgb(0xFFFFFF00);
		public static Color YellowGreen => FromArgb(0xFF9ACD32);
	}
}
