#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Widgets
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
		public string XAxisLabel = "";
		public string YAxisLabel = "";
		public bool DisplayFirstYAxisValue = false;
		public string LabelFont;
		public string AxisFont;

		public LineGraphWidget()
			: base()
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
			XAxisLabel = other.XAxisLabel;
			YAxisLabel = other.YAxisLabel;
			DisplayFirstYAxisValue = other.DisplayFirstYAxisValue;
			LabelFont = other.LabelFont;
			AxisFont = other.AxisFont;
		}

		public override void Draw()
		{
			if (GetSeries == null || !GetSeries().Any()
				|| GetLabelFont == null || GetLabelFont() == null
				|| GetAxisFont == null || GetAxisFont() == null)
			{
				return;
			}
			var rect = RenderBounds;
			var origin = new float2(rect.Left, rect.Bottom);

			var width = rect.Width;
			var height = rect.Height;

			var tiny = Game.Renderer.Fonts[GetLabelFont()];
			var bold = Game.Renderer.Fonts[GetAxisFont()];

			var xAxisSize = GetXAxisSize();
			var yAxisSize = GetYAxisSize();

			var maxValue = GetSeries().Select(p => p.Points).SelectMany(d => d).Concat(new[] { 0f }).Max();
			var scale = 200 / Math.Max(5000, (float)Math.Ceiling(maxValue / 1000) * 1000);

			var xStep = width / xAxisSize;
			var yStep = height / yAxisSize;

			var pointCount = GetSeries().First().Points.Count();
			var pointStart = Math.Max(0, pointCount - xAxisSize);
			var pointEnd = Math.Max(pointCount, xAxisSize);

			var keyOffset = 0;
			foreach (var series in GetSeries())
			{
				var key = series.Key;
				var color = series.Color;
				var points = series.Points;
				if (points.Any())
				{
					points = points.Reverse().Take(xAxisSize).Reverse();
					var scaledData = points.Select(d => d * scale);
					var x = 0;
					scaledData.Aggregate((a, b) =>
					{
						Game.Renderer.LineRenderer.DrawLine(
							origin + new float2(x, -a),
							origin + new float2(x + xStep, -b),
							color, color);
						x += xStep;
						return b;
					});

					var value = points.Last();
					if (value != 0)
					{
						tiny.DrawText(GetValueFormat().F(value), origin + new float2(x, -value * scale - 2), color);
					}
				}

				tiny.DrawText(key, new float2(rect.Left, rect.Top) + new float2(5, 10 * keyOffset + 3), color);
				keyOffset++;
			}

			//TODO: make this stuff not draw outside of the RenderBounds
			for (int n = pointStart, x = 0; n <= pointEnd; n++, x += xStep)
			{
				Game.Renderer.LineRenderer.DrawLine(origin + new float2(x, 0), origin + new float2(x, -5), Color.White, Color.White);
				tiny.DrawText(GetXAxisValueFormat().F(n), origin + new float2(x, 2), Color.White);
			}
			bold.DrawText(GetXAxisLabel(), origin + new float2(width / 2, 20), Color.White);

			for (var y = (GetDisplayFirstYAxisValue() ? 0 : yStep); y <= height; y += yStep)
			{
				var yValue = y / scale;
				Game.Renderer.LineRenderer.DrawLine(origin + new float2(width - 5, -y), origin + new float2(width, -y), Color.White, Color.White);
				tiny.DrawText(GetYAxisValueFormat().F(yValue), origin + new float2(width + 2, -y), Color.White);
			}
			bold.DrawText(GetYAxisLabel(), origin + new float2(width + 40, -(height / 2)), Color.White);

			Game.Renderer.LineRenderer.DrawLine(origin, origin + new float2(width, 0), Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(origin, origin + new float2(0, -height), Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(origin + new float2(width, 0), origin + new float2(width, -height), Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(origin + new float2(0, -height), origin + new float2(width, -height), Color.White, Color.White);
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
