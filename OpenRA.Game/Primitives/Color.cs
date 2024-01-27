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
			// Convert HSL to HSV
			var v = l + s * Math.Min(l, 1 - l);
			var sv = v > 0 ? 2 * (1 - l / v) : 0;

			return FromAhsv(alpha, h, sv, v);
		}

		public static Color FromAhsv(int alpha, float h, float s, float v)
		{
			var (r, g, b) = HsvToRgb(h, s, v);
			return FromArgb(alpha, (byte)Math.Round(255 * r), (byte)Math.Round(255 * g), (byte)Math.Round(255 * b));
		}

		public static Color FromAhsv(float h, float s, float v)
		{
			return FromAhsv(255, h, s, v);
		}

		public (int A, float H, float S, float V) ToAhsv()
		{
			var (h, s, v) = RgbToHsv(R, G, B);
			return (A, h, s, v);
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

		static float SrgbToLinear(float c)
		{
			// Standard gamma conversion equation: see e.g. http://entropymine.com/imageworsener/srgbformula/
			return c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);
		}

		static float LinearToSrgb(float c)
		{
			// Standard gamma conversion equation: see e.g. http://entropymine.com/imageworsener/srgbformula/
			return c <= 0.0031308f ? c * 12.92f : 1.055f * (float)Math.Pow(c, 1.0f / 2.4f) - 0.055f;
		}

		public (float R, float G, float B) ToLinear()
		{
			// Undo pre-multiplied alpha and gamma correction
			var r = SrgbToLinear((float)R / A);
			var g = SrgbToLinear((float)G / A);
			var b = SrgbToLinear((float)B / A);

			return (r, g, b);
		}

		public static Color FromLinear(byte a, float r, float g, float b)
		{
			// Apply gamma correction and pre-multiplied alpha
			return FromArgb(a,
				(byte)Math.Round(LinearToSrgb(r) * a),
				(byte)Math.Round(LinearToSrgb(g) * a),
				(byte)Math.Round(LinearToSrgb(b) * a));
		}

		public static (float R, float G, float B) HsvToRgb(float h, float s, float v)
		{
			// Based on maths explained in http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
			var px = Math.Abs(h * 6f - 3);
			var py = Math.Abs((h + 2f / 3) % 1 * 6f - 3);
			var pz = Math.Abs((h + 1f / 3) % 1 * 6f - 3);

			var r = v * float2.Lerp(1f, (px - 1).Clamp(0, 1), s);
			var g = v * float2.Lerp(1f, (py - 1).Clamp(0, 1), s);
			var b = v * float2.Lerp(1f, (pz - 1).Clamp(0, 1), s);

			return (r, g, b);
		}

		public static (float H, float S, float V) RgbToHsv(byte r, byte g, byte b)
		{
			return RgbToHsv(r / 255f, g / 255f, b / 255f);
		}

		public static (float H, float S, float V) RgbToHsv(float r, float g, float b)
		{
			// Based on maths explained in http://lolengine.net/blog/2013/01/13/fast-rgb-to-hsv
			var rgbMax = Math.Max(r, Math.Max(g, b));
			var rgbMin = Math.Min(r, Math.Min(g, b));
			var delta = rgbMax - rgbMin;
			var v = rgbMax;

			// Greyscale colors are defined to have hue and saturation 0
			if (delta == 0.0f)
				return (0, 0, v);

			float hue;
			if (r == rgbMax)
				hue = (g - b) / (6 * delta);
			else if (g == rgbMax)
				hue = (b - r) / (6 * delta) + 1 / 3f;
			else
				hue = (r - g) / (6 * delta) + 2 / 3f;

			var h = hue - (int)hue;

			// Wrap negative values into [0-1)
			if (h < 0)
				h++;

			var s = delta / rgbMax;
			return (h, s, v);
		}

		public static bool TryParse(string value, out Color color)
		{
			color = default;
			value = value.Trim();
			if (value.Length != 6 && value.Length != 8)
				return false;

			byte alpha = 255;
			if (!byte.TryParse(value.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red)
				|| !byte.TryParse(value.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green)
				|| !byte.TryParse(value.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
				return false;

			if (value.Length == 8
				&& !byte.TryParse(value.AsSpan(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out alpha))
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
			if (obj is not Color)
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
				return CryptoUtil.ToHex(stackalloc byte[3] { R, G, B });

			return CryptoUtil.ToHex(stackalloc byte[4] { R, G, B, A });
		}

		public static Color Transparent => FromArgb(0x00FFFFFF);

		[TranslationReference("color-F0F8FF")]
		public static Color AliceBlue => FromArgb(0xFFF0F8FF);

		[TranslationReference("color-FAEBD7")]
		public static Color AntiqueWhite => FromArgb(0xFFFAEBD7);

		[TranslationReference("color-00FFFF")]
		public static Color Aqua => FromArgb(0xFF00FFFF);

		[TranslationReference("color-7FFFD4")]
		public static Color Aquamarine => FromArgb(0xFF7FFFD4);

		[TranslationReference("color-F0FFFF")]
		public static Color Azure => FromArgb(0xFFF0FFFF);

		[TranslationReference("color-F5F5DC")]
		public static Color Beige => FromArgb(0xFFF5F5DC);

		[TranslationReference("color-FFE4C4")]
		public static Color Bisque => FromArgb(0xFFFFE4C4);

		[TranslationReference("color-000000")]
		public static Color Black => FromArgb(0xFF000000);

		[TranslationReference("color-FFEBCD")]
		public static Color BlanchedAlmond => FromArgb(0xFFFFEBCD);

		[TranslationReference("color-0000FF")]
		public static Color Blue => FromArgb(0xFF0000FF);

		[TranslationReference("color-8A2BE2")]
		public static Color BlueViolet => FromArgb(0xFF8A2BE2);

		[TranslationReference("color-A52A2A")]
		public static Color Brown => FromArgb(0xFFA52A2A);

		[TranslationReference("color-DEB887")]
		public static Color BurlyWood => FromArgb(0xFFDEB887);

		[TranslationReference("color-5F9EA0")]
		public static Color CadetBlue => FromArgb(0xFF5F9EA0);

		[TranslationReference("color-7FFF00")]
		public static Color Chartreuse => FromArgb(0xFF7FFF00);

		[TranslationReference("color-D2691E")]
		public static Color Chocolate => FromArgb(0xFFD2691E);

		[TranslationReference("color-FF7F50")]
		public static Color Coral => FromArgb(0xFFFF7F50);

		[TranslationReference("color-6495ED")]
		public static Color CornflowerBlue => FromArgb(0xFF6495ED);

		[TranslationReference("color-FFF8DC")]
		public static Color Cornsilk => FromArgb(0xFFFFF8DC);

		[TranslationReference("color-DC143C")]
		public static Color Crimson => FromArgb(0xFFDC143C);

		[TranslationReference("color-00FFFF")]
		public static Color Cyan => FromArgb(0xFF00FFFF);

		[TranslationReference("color-00008B")]
		public static Color DarkBlue => FromArgb(0xFF00008B);

		[TranslationReference("color-008B8B")]
		public static Color DarkCyan => FromArgb(0xFF008B8B);

		[TranslationReference("color-B8860B")]
		public static Color DarkGoldenrod => FromArgb(0xFFB8860B);

		[TranslationReference("color-A9A9A9")]
		public static Color DarkGray => FromArgb(0xFFA9A9A9);

		[TranslationReference("color-006400")]
		public static Color DarkGreen => FromArgb(0xFF006400);

		[TranslationReference("color-BDB76B")]
		public static Color DarkKhaki => FromArgb(0xFFBDB76B);

		[TranslationReference("color-8B008B")]
		public static Color DarkMagenta => FromArgb(0xFF8B008B);

		[TranslationReference("color-556B2F")]
		public static Color DarkOliveGreen => FromArgb(0xFF556B2F);

		[TranslationReference("color-FF8C00")]
		public static Color DarkOrange => FromArgb(0xFFFF8C00);

		[TranslationReference("color-9932CC")]
		public static Color DarkOrchid => FromArgb(0xFF9932CC);

		[TranslationReference("color-8B0000")]
		public static Color DarkRed => FromArgb(0xFF8B0000);

		[TranslationReference("color-E9967A")]
		public static Color DarkSalmon => FromArgb(0xFFE9967A);

		[TranslationReference("color-8FBC8B")]
		public static Color DarkSeaGreen => FromArgb(0xFF8FBC8B);

		[TranslationReference("color-483D8B")]
		public static Color DarkSlateBlue => FromArgb(0xFF483D8B);

		[TranslationReference("color-2F4F4F")]
		public static Color DarkSlateGray => FromArgb(0xFF2F4F4F);

		[TranslationReference("color-00CED1")]
		public static Color DarkTurquoise => FromArgb(0xFF00CED1);

		[TranslationReference("color-9400D3")]
		public static Color DarkViolet => FromArgb(0xFF9400D3);

		[TranslationReference("color-FF1493")]
		public static Color DeepPink => FromArgb(0xFFFF1493);

		[TranslationReference("color-00BFFF")]
		public static Color DeepSkyBlue => FromArgb(0xFF00BFFF);

		[TranslationReference("color-696969")]
		public static Color DimGray => FromArgb(0xFF696969);

		[TranslationReference("color-1E90FF")]
		public static Color DodgerBlue => FromArgb(0xFF1E90FF);

		[TranslationReference("color-B22222")]
		public static Color Firebrick => FromArgb(0xFFB22222);

		[TranslationReference("color-FFFAF0")]
		public static Color FloralWhite => FromArgb(0xFFFFFAF0);

		[TranslationReference("color-228B22")]
		public static Color ForestGreen => FromArgb(0xFF228B22);

		[TranslationReference("color-FF00FF")]
		public static Color Fuchsia => FromArgb(0xFFFF00FF);

		[TranslationReference("color-DCDCDC")]
		public static Color Gainsboro => FromArgb(0xFFDCDCDC);

		[TranslationReference("color-F8F8FF")]
		public static Color GhostWhite => FromArgb(0xFFF8F8FF);

		[TranslationReference("color-FFD700")]
		public static Color Gold => FromArgb(0xFFFFD700);

		[TranslationReference("color-DAA520")]
		public static Color Goldenrod => FromArgb(0xFFDAA520);

		[TranslationReference("color-808080")]
		public static Color Gray => FromArgb(0xFF808080);

		[TranslationReference("color-008000")]
		public static Color Green => FromArgb(0xFF008000);

		[TranslationReference("color-ADFF2F")]
		public static Color GreenYellow => FromArgb(0xFFADFF2F);

		[TranslationReference("color-F0FFF0")]
		public static Color Honeydew => FromArgb(0xFFF0FFF0);

		[TranslationReference("color-FF69B4")]
		public static Color HotPink => FromArgb(0xFFFF69B4);

		[TranslationReference("color-CD5C5C")]
		public static Color IndianRed => FromArgb(0xFFCD5C5C);

		[TranslationReference("color-4B0082")]
		public static Color Indigo => FromArgb(0xFF4B0082);

		[TranslationReference("color-FFFFF0")]
		public static Color Ivory => FromArgb(0xFFFFFFF0);

		[TranslationReference("color-F0E68C")]
		public static Color Khaki => FromArgb(0xFFF0E68C);

		[TranslationReference("color-E6E6FA")]
		public static Color Lavender => FromArgb(0xFFE6E6FA);

		[TranslationReference("color-FFF0F5")]
		public static Color LavenderBlush => FromArgb(0xFFFFF0F5);

		[TranslationReference("color-7CFC00")]
		public static Color LawnGreen => FromArgb(0xFF7CFC00);

		[TranslationReference("color-FFFACD")]
		public static Color LemonChiffon => FromArgb(0xFFFFFACD);

		[TranslationReference("color-ADD8E6")]
		public static Color LightBlue => FromArgb(0xFFADD8E6);

		[TranslationReference("color-F08080")]
		public static Color LightCoral => FromArgb(0xFFF08080);

		[TranslationReference("color-E0FFFF")]
		public static Color LightCyan => FromArgb(0xFFE0FFFF);

		[TranslationReference("color-FAFAD2")]
		public static Color LightGoldenrodYellow => FromArgb(0xFFFAFAD2);

		[TranslationReference("color-D3D3D3")]
		public static Color LightGray => FromArgb(0xFFD3D3D3);

		[TranslationReference("color-90EE90")]
		public static Color LightGreen => FromArgb(0xFF90EE90);

		[TranslationReference("color-FFB6C1")]
		public static Color LightPink => FromArgb(0xFFFFB6C1);

		[TranslationReference("color-FFA07A")]
		public static Color LightSalmon => FromArgb(0xFFFFA07A);

		[TranslationReference("color-20B2AA")]
		public static Color LightSeaGreen => FromArgb(0xFF20B2AA);

		[TranslationReference("color-87CEFA")]
		public static Color LightSkyBlue => FromArgb(0xFF87CEFA);

		[TranslationReference("color-778899")]
		public static Color LightSlateGray => FromArgb(0xFF778899);

		[TranslationReference("color-B0C4DE")]
		public static Color LightSteelBlue => FromArgb(0xFFB0C4DE);

		[TranslationReference("color-FFFFE0")]
		public static Color LightYellow => FromArgb(0xFFFFFFE0);

		[TranslationReference("color-00FF00")]
		public static Color Lime => FromArgb(0xFF00FF00);

		[TranslationReference("color-32CD32")]
		public static Color LimeGreen => FromArgb(0xFF32CD32);

		[TranslationReference("color-FAF0E6")]
		public static Color Linen => FromArgb(0xFFFAF0E6);

		[TranslationReference("color-FF00FF")]
		public static Color Magenta => FromArgb(0xFFFF00FF);

		[TranslationReference("color-800000")]
		public static Color Maroon => FromArgb(0xFF800000);

		[TranslationReference("color-66CDAA")]
		public static Color MediumAquamarine => FromArgb(0xFF66CDAA);

		[TranslationReference("color-0000CD")]
		public static Color MediumBlue => FromArgb(0xFF0000CD);

		[TranslationReference("color-BA55D3")]
		public static Color MediumOrchid => FromArgb(0xFFBA55D3);

		[TranslationReference("color-9370DB")]
		public static Color MediumPurple => FromArgb(0xFF9370DB);

		[TranslationReference("color-3CB371")]
		public static Color MediumSeaGreen => FromArgb(0xFF3CB371);

		[TranslationReference("color-7B68EE")]
		public static Color MediumSlateBlue => FromArgb(0xFF7B68EE);

		[TranslationReference("color-00FA9A")]
		public static Color MediumSpringGreen => FromArgb(0xFF00FA9A);

		[TranslationReference("color-48D1CC")]
		public static Color MediumTurquoise => FromArgb(0xFF48D1CC);

		[TranslationReference("color-C71585")]
		public static Color MediumVioletRed => FromArgb(0xFFC71585);

		[TranslationReference("color-191970")]
		public static Color MidnightBlue => FromArgb(0xFF191970);

		[TranslationReference("color-F5FFFA")]
		public static Color MintCream => FromArgb(0xFFF5FFFA);

		[TranslationReference("color-FFE4E1")]
		public static Color MistyRose => FromArgb(0xFFFFE4E1);

		[TranslationReference("color-FFE4B5")]
		public static Color Moccasin => FromArgb(0xFFFFE4B5);

		[TranslationReference("color-FFDEAD")]
		public static Color NavajoWhite => FromArgb(0xFFFFDEAD);

		[TranslationReference("color-000080")]
		public static Color Navy => FromArgb(0xFF000080);

		[TranslationReference("color-FDF5E6")]
		public static Color OldLace => FromArgb(0xFFFDF5E6);

		[TranslationReference("color-808000")]
		public static Color Olive => FromArgb(0xFF808000);

		[TranslationReference("color-6B8E23")]
		public static Color OliveDrab => FromArgb(0xFF6B8E23);

		[TranslationReference("color-FFA500")]
		public static Color Orange => FromArgb(0xFFFFA500);

		[TranslationReference("color-FF4500")]
		public static Color OrangeRed => FromArgb(0xFFFF4500);

		[TranslationReference("color-DA70D6")]
		public static Color Orchid => FromArgb(0xFFDA70D6);

		[TranslationReference("color-EEE8AA")]
		public static Color PaleGoldenrod => FromArgb(0xFFEEE8AA);

		[TranslationReference("color-98FB98")]
		public static Color PaleGreen => FromArgb(0xFF98FB98);

		[TranslationReference("color-AFEEEE")]
		public static Color PaleTurquoise => FromArgb(0xFFAFEEEE);

		[TranslationReference("color-DB7093")]
		public static Color PaleVioletRed => FromArgb(0xFFDB7093);

		[TranslationReference("color-FFEFD5")]
		public static Color PapayaWhip => FromArgb(0xFFFFEFD5);

		[TranslationReference("color-FFDAB9")]
		public static Color PeachPuff => FromArgb(0xFFFFDAB9);

		[TranslationReference("color-CD853F")]
		public static Color Peru => FromArgb(0xFFCD853F);

		[TranslationReference("color-FFC0CB")]
		public static Color Pink => FromArgb(0xFFFFC0CB);

		[TranslationReference("color-DDA0DD")]
		public static Color Plum => FromArgb(0xFFDDA0DD);

		[TranslationReference("color-B0E0E6")]
		public static Color PowderBlue => FromArgb(0xFFB0E0E6);

		[TranslationReference("color-800080")]
		public static Color Purple => FromArgb(0xFF800080);

		[TranslationReference("color-FF0000")]
		public static Color Red => FromArgb(0xFFFF0000);

		[TranslationReference("color-BC8F8F")]
		public static Color RosyBrown => FromArgb(0xFFBC8F8F);

		[TranslationReference("color-4169E1")]
		public static Color RoyalBlue => FromArgb(0xFF4169E1);

		[TranslationReference("color-8B4513")]
		public static Color SaddleBrown => FromArgb(0xFF8B4513);

		[TranslationReference("color-FA8072")]
		public static Color Salmon => FromArgb(0xFFFA8072);

		[TranslationReference("color-F4A460")]
		public static Color SandyBrown => FromArgb(0xFFF4A460);

		[TranslationReference("color-2E8B57")]
		public static Color SeaGreen => FromArgb(0xFF2E8B57);

		[TranslationReference("color-FFF5EE")]
		public static Color SeaShell => FromArgb(0xFFFFF5EE);

		[TranslationReference("color-A0522D")]
		public static Color Sienna => FromArgb(0xFFA0522D);

		[TranslationReference("color-C0C0C0")]
		public static Color Silver => FromArgb(0xFFC0C0C0);

		[TranslationReference("color-87CEEB")]
		public static Color SkyBlue => FromArgb(0xFF87CEEB);

		[TranslationReference("color-6A5ACD")]
		public static Color SlateBlue => FromArgb(0xFF6A5ACD);

		[TranslationReference("color-708090")]
		public static Color SlateGray => FromArgb(0xFF708090);

		[TranslationReference("color-FFFAFA")]
		public static Color Snow => FromArgb(0xFFFFFAFA);

		[TranslationReference("color-00FF7F")]
		public static Color SpringGreen => FromArgb(0xFF00FF7F);

		[TranslationReference("color-4682B4")]
		public static Color SteelBlue => FromArgb(0xFF4682B4);

		[TranslationReference("color-D2B48C")]
		public static Color Tan => FromArgb(0xFFD2B48C);

		[TranslationReference("color-008080")]
		public static Color Teal => FromArgb(0xFF008080);

		[TranslationReference("color-D8BFD8")]
		public static Color Thistle => FromArgb(0xFFD8BFD8);

		[TranslationReference("color-FF6347")]
		public static Color Tomato => FromArgb(0xFFFF6347);

		[TranslationReference("color-40E0D0")]
		public static Color Turquoise => FromArgb(0xFF40E0D0);

		[TranslationReference("color-EE82EE")]
		public static Color Violet => FromArgb(0xFFEE82EE);

		[TranslationReference("color-F5DEB3")]
		public static Color Wheat => FromArgb(0xFFF5DEB3);

		[TranslationReference("color-FFFFFF")]
		public static Color White => FromArgb(0xFFFFFFFF);

		[TranslationReference("color-F5F5F5")]
		public static Color WhiteSmoke => FromArgb(0xFFF5F5F5);

		[TranslationReference("color-FFFF00")]
		public static Color Yellow => FromArgb(0xFFFFFF00);

		[TranslationReference("color-9ACD32")]
		public static Color YellowGreen => FromArgb(0xFF9ACD32);

		public static int GetDistance(Color left, Color right)
		{
			return Math.Abs(left.R - right.R) + Math.Abs(left.G - right.G) + Math.Abs(left.B - right.B);
		}
	}
}
