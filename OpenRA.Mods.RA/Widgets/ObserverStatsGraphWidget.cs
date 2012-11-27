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
	public class ObserverStatsGraphWidget : Widget
	{
		public Func<IEnumerable<Pair<Player, IEnumerable<float>>>> GetDataSource;
		public Func<float> GetDataScale;
		public Func<string> GetValueFormat;
		public Func<string> GetXAxisValueFormat;
		public Func<string> GetYAxisValueFormat;
		public Func<int> GetXAxisSize;
		public Func<int> GetYAxisSize;
		public Func<string> GetXAxisLabel;
		public Func<string> GetYAxisLabel;
		public Func<bool> GetDisplayYAxisZero;
		public string ValueFormat = "{0}";
		public string XAxisValueFormat = "{0}";
		public string YAxisValueFormat = "{0}";
		public int XAxisSize = 10;
		public int YAxisSize = 10;
		public string XAxisLabel = "";
		public string YAxisLabel = "";
		public bool DisplayYAxisZero = true;

		public ObserverStatsGraphWidget()
			: base()
		{
			GetValueFormat = () => ValueFormat;
			GetXAxisValueFormat = () => XAxisValueFormat;
			GetYAxisValueFormat = () => YAxisValueFormat;
			GetXAxisSize = () => XAxisSize;
			GetYAxisSize = () => YAxisSize;
			GetXAxisLabel = () => XAxisLabel;
			GetYAxisLabel = () => YAxisLabel;
			GetDisplayYAxisZero = () => DisplayYAxisZero;
		}

		protected ObserverStatsGraphWidget(ObserverStatsGraphWidget other)
			: base(other)
		{
			GetDataSource = other.GetDataSource;
			GetValueFormat = other.GetValueFormat;
			GetXAxisValueFormat = other.GetXAxisValueFormat;
			GetYAxisValueFormat = other.GetYAxisValueFormat;
			GetXAxisSize = other.GetXAxisSize;
			GetYAxisSize = other.GetYAxisSize;
			GetXAxisLabel = other.GetXAxisLabel;
			GetYAxisLabel = other.GetYAxisLabel;
			GetDisplayYAxisZero = other.GetDisplayYAxisZero;
			ValueFormat = other.ValueFormat;
			XAxisValueFormat = other.XAxisValueFormat;
			YAxisValueFormat = other.YAxisValueFormat;
			XAxisSize = other.XAxisSize;
			YAxisSize = other.YAxisSize;
			XAxisLabel = other.XAxisLabel;
			YAxisLabel = other.YAxisLabel;
			DisplayYAxisZero = other.DisplayYAxisZero;
		}

		public override void Draw()
		{
			if (GetDataSource == null || !GetDataSource().Any())
			{
				return;
			}
			var rect = RenderBounds;
			var origin = new float2(rect.Left, rect.Bottom);

			var width = rect.Width;
			var height = rect.Height;

			Game.Renderer.LineRenderer.DrawLine(origin, origin + new float2(width, 0), Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(origin, origin + new float2(0, -height), Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(origin + new float2(width, 0), origin + new float2(width, -height), Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(origin + new float2(0, -height), origin + new float2(width, -height), Color.White, Color.White);

			var tinyBold = Game.Renderer.Fonts["TinyBold"];
			var bold = Game.Renderer.Fonts["Bold"];

			var xAxisSize = GetXAxisSize();
			var yAxisSize = GetYAxisSize();

			var xStep = width / xAxisSize;
			var yStep = height / yAxisSize;

			var actualNodeCount = GetDataSource().First().Second.Count();
			var visibleNodeStart = Math.Max(0, actualNodeCount - xAxisSize);
			var visibleNodeEnd = Math.Max(actualNodeCount, xAxisSize);

			float maxValue = GetDataSource().Select(p => p.Second).SelectMany(d => d).Max();
			var scale = (100 / maxValue) * 3;

			//todo: make this stuff not draw outside of the RenderBounds
			for (int n = visibleNodeStart, x = 0; n <= visibleNodeEnd; n++, x += xStep)
			{
				Game.Renderer.LineRenderer.DrawLine(origin + new float2(x, 0), origin + new float2(x, -5), Color.White, Color.White);
				tinyBold.DrawText(GetXAxisValueFormat().F(n), origin + new float2(x, 2), Color.White);
			} 
			bold.DrawText(GetXAxisLabel(), origin + new float2(width / 2, 20), Color.White);

			for (var y = (GetDisplayYAxisZero() ? 0f : yStep); y <= height; y += yStep)
			{
				var value = y / scale;
				Game.Renderer.LineRenderer.DrawLine(origin + new float2(width - 5, -y), origin + new float2(width, -y), Color.White, Color.White);
				tinyBold.DrawText(GetYAxisValueFormat().F(value), origin + new float2(width + 2, -y), Color.White);
			}
			bold.DrawText(GetYAxisLabel(), origin + new float2(width + 40, -(height / 2)), Color.White);

			var playerNameOffset = 0;
			foreach (var playerDataPair in GetDataSource())
			{
				var player = playerDataPair.First;
				var data = playerDataPair.Second;
				var color = player.ColorRamp.GetColor(0);
				if (data.Any())
				{
					data = data.Reverse().Take(xAxisSize).Reverse();
					var scaledData = data.Select(d => d * scale);
					var x = 0f;
					scaledData.Aggregate((a, b) =>
					{
						Game.Renderer.LineRenderer.DrawLine(
							origin + new float2(x, -a),
							origin + new float2(x + xStep, -b),
							color, color);
						x += xStep;
						return b;
					});

					var value = data.Last();
					if (value != 0)
					{
						var scaledValue = value * scale;
						tinyBold.DrawText(GetValueFormat().F(value), origin + new float2(x, -scaledValue - 2), color);
					}
				}

				tinyBold.DrawText(player.PlayerName, new float2(rect.Left, rect.Top) + new float2(5, 10 * playerNameOffset + 3), color);
				playerNameOffset++;
			}
		}

		public override Widget Clone()
		{
			return new ObserverStatsGraphWidget(this);
		}
	}
}
