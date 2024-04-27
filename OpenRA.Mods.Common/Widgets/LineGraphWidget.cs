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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class LineGraphWidget : Widget
	{
		public Func<IEnumerable<LineGraphSeries>> GetSeries;
		public Func<string> GetValueFormat;
		public Func<string> GetXAxisValueFormat;
		public Func<string> GetYAxisValueFormat;
		public Func<int> GetXAxisSize;
		public Func<int> GetYAxisSize;
		public Func<string> GetXAxisLabel;
		public Func<string> GetYAxisLabel;
		public Func<bool> GetDisplayFirstYAxisValue;
		public Func<string> GetLabelFont;
		public Func<string> GetAxisFont;
		public string ValueFormat = "{0}";
		public string XAxisValueFormat = "{0}";
		public string YAxisValueFormat = "{0}";
		public int XAxisSize = 10;
		public int YAxisSize = 10;
		public int XAxisTicksPerLabel = 1;
		public string XAxisLabel = "";
		public string YAxisLabel = "";
		public bool DisplayFirstYAxisValue = false;
		public string LabelFont;
		public string AxisFont;
		public Color BackgroundColorDark = ChromeMetrics.Get<Color>("TextContrastColorDark");
		public Color BackgroundColorLight = ChromeMetrics.Get<Color>("TextContrastColorLight");
		public int Padding = 5;

		public LineGraphWidget()
		{
			GetValueFormat = () => ValueFormat;
			GetXAxisValueFormat = () => XAxisValueFormat;
			GetYAxisValueFormat = () => YAxisValueFormat;
			GetXAxisSize = () => XAxisSize;
			GetYAxisSize = () => YAxisSize;
			GetXAxisLabel = () => XAxisLabel;
			GetYAxisLabel = () => YAxisLabel;
			GetDisplayFirstYAxisValue = () => DisplayFirstYAxisValue;
			GetLabelFont = () => LabelFont;
			GetAxisFont = () => AxisFont;
		}

		protected LineGraphWidget(LineGraphWidget other)
			: base(other)
		{
			GetSeries = other.GetSeries;
			GetValueFormat = other.GetValueFormat;
			GetXAxisValueFormat = other.GetXAxisValueFormat;
			GetYAxisValueFormat = other.GetYAxisValueFormat;
			GetXAxisSize = other.GetXAxisSize;
			GetYAxisSize = other.GetYAxisSize;
			GetXAxisLabel = other.GetXAxisLabel;
			GetYAxisLabel = other.GetYAxisLabel;
			GetDisplayFirstYAxisValue = other.GetDisplayFirstYAxisValue;
			GetLabelFont = other.GetLabelFont;
			GetAxisFont = other.GetAxisFont;
			ValueFormat = other.ValueFormat;
			XAxisValueFormat = other.XAxisValueFormat;
			YAxisValueFormat = other.YAxisValueFormat;
			XAxisSize = other.XAxisSize;
			YAxisSize = other.YAxisSize;
			XAxisTicksPerLabel = other.XAxisTicksPerLabel;
			XAxisLabel = other.XAxisLabel;
			YAxisLabel = other.YAxisLabel;
			DisplayFirstYAxisValue = other.DisplayFirstYAxisValue;
			LabelFont = other.LabelFont;
			AxisFont = other.AxisFont;
			BackgroundColorDark = other.BackgroundColorDark;
			BackgroundColorLight = other.BackgroundColorLight;
			Padding = other.Padding;
		}

		public override void Draw()
		{
			if (GetSeries == null || GetLabelFont == null)
				return;

			var series = GetSeries();
			if (!series.Any())
				return;

			var font = GetLabelFont();
			if (font == null)
				return;

			var cr = Game.Renderer.RgbaColorRenderer;
			var rect = RenderBounds;

			var labelFont = Game.Renderer.Fonts[font];
			var axisFont = Game.Renderer.Fonts[GetAxisFont()];

			var xAxisSize = GetXAxisSize();
			var yAxisSize = GetYAxisSize();

			var xAxisLabel = GetXAxisLabel();
			var xAxisLabelSize = axisFont.Measure(xAxisLabel);

			var xAxisPointLabelHeight = labelFont.Measure("0").Y;

			var graphBottomOffset = Padding * 2 + xAxisLabelSize.Y + xAxisPointLabelHeight;
			var height = rect.Height - (graphBottomOffset + Padding);

			var maxValue = series.Select(p => p.Points).SelectMany(d => d).Concat(new[] { 0f }).Max();
			var longestName = series.Select(s => s.Key).OrderByDescending(s => s.Length).FirstOrDefault() ?? "";

			var scale = 200 / Math.Max(5000, (float)Math.Ceiling(maxValue / 1000) * 1000);

			var widthMaxValue = labelFont.Measure(GetYAxisValueFormat().FormatCurrent(height / scale)).X;
			var widthLongestName = labelFont.Measure(longestName).X;

			// y axis label
			var yAxisLabel = GetYAxisLabel();
			var yAxisLabelSize = axisFont.Measure(yAxisLabel);

			var width = rect.Width - (Padding * 4 + widthMaxValue + widthLongestName + yAxisLabelSize.Y);

			var xStep = width / xAxisSize;
			var yStep = height / yAxisSize;

			var pointCount = series.Max(s => s.Points.Count());
			var pointStart = Math.Max(0, pointCount - xAxisSize);
			var pointEnd = Math.Max(pointCount, xAxisSize);

			var graphOrigin = new float2(rect.Left, rect.Bottom) + new float2(Padding * 2 + widthMaxValue + yAxisLabelSize.Y, -graphBottomOffset);

			var origin = new float2(rect.Left, rect.Bottom);

			var keyOffset = 0;

			// added sorting so that names appear in order of highest value to lowest value
			series = series.OrderByDescending(s => s.Points.LastOrDefault()).ToList();

			foreach (var s in series)
			{
				var key = s.Key;
				var color = s.Color;
				var points = s.Points;
				if (points.Any())
				{
					points = points.Reverse().Take(xAxisSize).Reverse();
					var lastX = 0;
					var lastPoint = 0f;
					cr.DrawLine(
						points.Select((point, x) =>
						{
							lastX = x;
							lastPoint = point;
							return graphOrigin + new float3(x * xStep, -point * scale, 0);
						}), 1, color);

					if (lastPoint != 0f)
						labelFont.DrawTextWithShadow(GetValueFormat().FormatCurrent(lastPoint), graphOrigin + new float2(lastX * xStep, -lastPoint * scale - 2),
							color, BackgroundColorDark, BackgroundColorLight, 1);
				}

				labelFont.DrawTextWithShadow(key, new float2(rect.Right, rect.Top) + new float2(-(widthLongestName + Padding), 10 * keyOffset + 3),
					color, BackgroundColorDark, BackgroundColorLight, 1);
				keyOffset++;
			}

			// Draw x axis
			axisFont.DrawTextWithShadow(xAxisLabel,
				new float2(graphOrigin.X, origin.Y) + new float2(width / 2 - xAxisLabelSize.X / 2, -(xAxisLabelSize.Y + Padding)),
				Color.White, BackgroundColorDark, BackgroundColorLight, 1);

			// TODO: make this stuff not draw outside of the RenderBounds
			for (int n = pointStart, x = 0; n <= pointEnd; n++, x += xStep)
			{
				cr.DrawLine(graphOrigin + new float2(x, 0), graphOrigin + new float2(x, -5), 1, Color.White);
				if (n % XAxisTicksPerLabel != 0)
					continue;

				var xAxisText = GetXAxisValueFormat().FormatCurrent(n / XAxisTicksPerLabel);
				var xAxisTickTextWidth = labelFont.Measure(xAxisText).X;
				var xLocation = x - xAxisTickTextWidth / 2;
				labelFont.DrawTextWithShadow(xAxisText,
					graphOrigin + new float2(xLocation, 2),
					Color.White, BackgroundColorDark, BackgroundColorLight, 1);
			}

			// Draw y axis
			axisFont.DrawTextWithShadow(yAxisLabel,
				new float2(origin.X, graphOrigin.Y) + new float2(5 - axisFont.TopOffset, -(height / 2 - yAxisLabelSize.X / 2)),
				Color.White, BackgroundColorDark, BackgroundColorLight, 1, (float)Math.PI / 2);

			for (var y = GetDisplayFirstYAxisValue() ? 0 : yStep; y <= height; y += yStep)
			{
				var yValue = y / scale;
				cr.DrawLine(graphOrigin + new float2(0, -y), graphOrigin + new float2(5, -y), 1, Color.White);
				var text = GetYAxisValueFormat().FormatCurrent(yValue);

				var textWidth = labelFont.Measure(text);

				var yLocation = y + (textWidth.Y + labelFont.TopOffset) / 2;

				labelFont.DrawTextWithShadow(text,
					graphOrigin + new float2(-(textWidth.X + 3), -yLocation),
					Color.White, BackgroundColorDark, BackgroundColorLight, 1);
			}

			// Bottom line
			cr.DrawLine(graphOrigin, graphOrigin + new float2(width, 0), 1, Color.White);

			// Left line
			cr.DrawLine(graphOrigin, graphOrigin + new float2(0, -height), 1, Color.White);
		}

		public override Widget Clone()
		{
			return new LineGraphWidget(this);
		}
	}

	public class LineGraphSeries
	{
		public string Key;
		public Color Color;
		public IEnumerable<float> Points;

		public LineGraphSeries(string key, Color color, IEnumerable<float> points)
		{
			Key = key;
			Color = color;
			Points = points;
		}
	}
}
