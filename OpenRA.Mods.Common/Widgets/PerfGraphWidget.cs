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
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class PerfGraphWidget : Widget
	{
		readonly int dotWidth;
		readonly int nextDotAdvance;
		readonly Cache<string, int> textWidthCache = null;
		readonly SpriteFont font = Game.Renderer?.Fonts["Tiny"];
		StringBuilder builder = null;

		public PerfGraphWidget()
		{
			// Skip if running format unit test
			if (font == null)
				return;

			textWidthCache = new(GetTextWidth);
			dotWidth = textWidthCache["."];
			nextDotAdvance = textWidthCache[".."] - dotWidth;
		}

		int GetTextWidth(string text) => font.Measure(text).X;

		// Try to keep the text at 6 characters long to align columns.
		public void SixCharacterFormatFloat(StringBuilder output, double value)
		{
			if (double.IsNaN(value))
			{
				output.Append("NaN   ");
				return;
			}

			if (double.IsInfinity(value))
			{
				output.Append(double.IsPositiveInfinity(value) ? "+Inf  " : "-Inf  ");
				return;
			}

			if (value < 0)
			{
				output.Append("<0    ");
				return;
			}

			var intValue = (int)value;

			// Do normal rounding instead of floor/trucate; account for numbers >= 100000 being 6 decimal places or "TooBig".
			var frontPlaceMultipler = 1;
			while (frontPlaceMultipler <= intValue && frontPlaceMultipler < 100000)
				frontPlaceMultipler *= 10;
			value += 0.000005 * frontPlaceMultipler;
			intValue = (int)value;

			if (intValue >= 1000000)
			{
				output.Append("TooBig");
				return;
			}

			var partValue = (int)value;
			var digit = partValue / 1000000;
			if (intValue >= 1000000)
				output.Append((char)(digit + '0'));

			// Append the whole part.
			for (var p = 1000000; p >= 10;)
			{
				var n = p / 10;

				// Skip leading zeros.
				if (intValue >= n)
				{
					partValue -= digit * p;
					digit = partValue / n;
					output.Append((char)(digit + '0'));
				}

				p = n;
			}

			// Check for room for the '.'.
			if (intValue >= 1000000)
				return;

			if (intValue < 100000)
				output.Append('.');

			if (intValue >= 10000)
				return;

			// Up to 5 fractional digits may be appended while keeping the total characters at 6.
			var fractionValue = (int)(value * 100000) - 100000 * intValue;
			for (var p = 10000; ;)
			{
				digit = fractionValue / p;
				output.Append((char)(digit + '0'));
				var n = p / 10;

				// Stop if reached 6 characters total.
				if (intValue >= n)
					return;

				fractionValue -= digit * p;
				p = n;
			}
		}

		public override void Draw()
		{
			var cr = Game.Renderer.RgbaColorRenderer;
			var rect = RenderBounds;
			var origin = new float2(rect.Right, rect.Bottom);
			var basis = new float2(-rect.Width / 100, -rect.Height / 100);

			cr.DrawLine(
			[
				new float3(rect.Left, rect.Top, 0),
				new float3(rect.Left, rect.Bottom, 0),
				new float3(rect.Right, rect.Bottom, 0)
			], 1, Color.White);

			cr.DrawLine(origin + new float2(100, 0) * basis, origin + new float2(100, 100) * basis, 1, Color.White);

			var k = 0;
			foreach (var item in PerfHistory.Items.Values)
			{
				cr.DrawLine(item.Samples()
					.Select((sample, i) => origin + new float3(i, (float)sample, 0) * basis),
					1, item.C);

				var u = new float2(rect.Left, rect.Top);

				cr.DrawLine(
					u + new float2(10, 10 * k + 5),
					u + new float2(12, 10 * k + 5),
					1, item.C);
				cr.DrawLine(
					u + new float2(10, 10 * k + 4),
					u + new float2(12, 10 * k + 4),
					1, item.C);

				++k;
			}

			k = 0;
			var maxExtraDotSpace = 0;
			var maxNameLength = 0;
			foreach (var item in PerfHistory.Items.Values)
			{
				maxExtraDotSpace = Math.Max(maxExtraDotSpace, textWidthCache[item.Name]);
				maxNameLength = Math.Max(maxNameLength, item.Name.Length);
			}

			builder ??= new StringBuilder(Math.Max(20, maxExtraDotSpace + 3));
			maxExtraDotSpace = Math.Max(0, maxExtraDotSpace - dotWidth); // First dot is always printed.
			var columnTwoStart = maxExtraDotSpace + 3 * nextDotAdvance + 2;

			foreach (var item in PerfHistory.Items.Values)
			{
				var nameWidth = textWidthCache[item.Name];
				var dotsNeeded = (maxExtraDotSpace - nameWidth) / nextDotAdvance + 3; // first + 2 extra dots for spacing before numbers.
				builder.Append(item.Name);
				builder.Append('.', dotsNeeded);
				font.DrawText(builder.ToString(), new float2(rect.Left, rect.Top) + new float2(18, 10 * k - 3), Color.White);
				builder.Clear();
				SixCharacterFormatFloat(builder, item.Average(Game.Settings.Debug.Samples));
				builder.Append(" : ");
				SixCharacterFormatFloat(builder, item.ActiveGameTotalAverage());
				font.DrawText(builder.ToString(), new float2(rect.Left, rect.Top) + new float2(18 + columnTwoStart, 10 * k - 3), Color.White);
				builder.Clear();
				++k;
			}
		}
	}
}
