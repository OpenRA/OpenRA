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
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public static class DevUiTheme
	{
		public static readonly Color DefaultRed = Color.FromArgb(255, 168, 40, 40);
		public static readonly Color DefaultPurple = Color.FromArgb(255, 94, 42, 110);

		static readonly string[] ThemedImages =
		[
			"dialog.png",
			"loadscreen.png",
			"loadscreen-2x.png",
			"loadscreen-3x.png"
		];

		public static Color TargetColor { get; private set; } = DefaultPurple;

		public static bool IsThemedImage(string image)
		{
			if (string.IsNullOrEmpty(image))
				return false;

			foreach (var themedImage in ThemedImages)
				if (image.EndsWith(themedImage, StringComparison.Ordinal))
					return true;

			return false;
		}

		public static void UpdateTargetColor(Color color)
		{
			TargetColor = color;
		}

		public static void ApplyTheme(Color color)
		{
			TargetColor = color;
			ChromeProvider.InvalidateThemedSheets();
			Game.ModData?.LoadScreen?.InvalidateTheme();
		}

		public static string GetSourcePath(string image, IReadOnlyFileSystem fs)
		{
			var backup = image + ".bak";
			return fs.Exists(backup) ? backup : image;
		}

		public static bool IsLoadScreenImage(string image)
		{
			if (string.IsNullOrEmpty(image))
				return false;

			return image.EndsWith("loadscreen.png", StringComparison.Ordinal)
				|| image.EndsWith("loadscreen-2x.png", StringComparison.Ordinal)
				|| image.EndsWith("loadscreen-3x.png", StringComparison.Ordinal);
		}

		public static void RecolorSheet(Sheet sheet, Color target, string image)
		{
			var flatLogoColor = IsLoadScreenImage(image);
			var data = sheet.GetData();
			var width = sheet.Size.Width;
			var stride = 4 * width;

			for (var y = 0; y < sheet.Size.Height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var i = y * stride + 4 * x;
					var pixel = Color.FromArgb(data[i + 3], data[i + 2], data[i + 1], data[i]);
					var recolored = flatLogoColor
						? TransformLoadScreenThemeColor(pixel, target, x, y)
						: TransformChromeThemeColor(pixel, target);
					data[i] = recolored.B;
					data[i + 1] = recolored.G;
					data[i + 2] = recolored.R;
					data[i + 3] = recolored.A;
				}
			}

			sheet.CommitBufferedData();
		}

		public static bool IsDefaultRed(Color color)
		{
			return ColorsEqual(color, DefaultRed);
		}

		public static bool IsDefaultPurple(Color color)
		{
			return ColorsEqual(color, DefaultPurple);
		}

		static bool ColorsEqual(Color a, Color b)
		{
			return a.R == b.R && a.G == b.G && a.B == b.B;
		}

		static bool ShouldRecolor(byte r, byte g, byte b, byte a)
		{
			if (a < 8)
				return false;
			if (r < 35)
				return false;
			if (r <= g + 12 && r <= b + 12)
				return false;
			if (g > b + 40 && r > g)
				return false;

			return true;
		}

		static bool LooksLikeThemePurple(byte r, byte g, byte b, byte a)
		{
			if (a < 8)
				return false;

			return Math.Abs(r - DefaultPurple.R) < 55
				&& Math.Abs(g - DefaultPurple.G) < 45
				&& Math.Abs(b - DefaultPurple.B) < 55
				&& b > g + 15;
		}

		static Color TransformChromeThemeColor(Color pixel, Color target)
		{
			var usePurpleReference = LooksLikeThemePurple(pixel.R, pixel.G, pixel.B, pixel.A);
			if (!ShouldRecolor(pixel.R, pixel.G, pixel.B, pixel.A) && !usePurpleReference)
				return pixel;

			if (IsDefaultRed(target))
				return pixel;

			var reference = usePurpleReference ? DefaultPurple : DefaultRed;
			var (_, refH, refS, _) = reference.ToAhsv();
			var (_, targetH, targetS, _) = target.ToAhsv();
			var (pa, ph, ps, pv) = pixel.ToAhsv();

			var dh = targetH - refH;
			var newH = ph + dh;
			if (newH < 0)
				newH += 1;
			else if (newH >= 1)
				newH -= 1;

			var satScale = refS > 0.01f ? targetS / refS : 1f;
			var newS = (ps * satScale).Clamp(0, 1);

			return Color.FromAhsv((int)pa, newH, newS, pv);
		}

		static Color TransformLoadScreenThemeColor(Color pixel, Color target, int x, int y)
		{
			var usePurpleReference = LooksLikeThemePurple(pixel.R, pixel.G, pixel.B, pixel.A);
			if (!ShouldRecolor(pixel.R, pixel.G, pixel.B, pixel.A) && !usePurpleReference)
				return pixel;

			if (IsDefaultRed(target))
				return pixel;

			// Logo/star: chosen accent hue with original light top-left / dark bottom-right shading.
			if (x < 256 && y < 256)
			{
				var (_, targetH, targetS, _) = target.ToAhsv();
				var (pa, _, _, pv) = pixel.ToAhsv();
				return Color.FromAhsv((int)pa, targetH, targetS, pv);
			}

			return TransformChromeThemeColor(pixel, target);
		}
	}
}
