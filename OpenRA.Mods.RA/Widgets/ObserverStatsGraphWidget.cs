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
		public Func<int> GetVisibleNodeCount;
		public Func<string> GetXAxisLabel;
		public Func<string> GetYAxisLabel;
		public Func<bool> GetDisplayYAxisZero;
		public string ValueFormat = "{0}";
		public string XAxisValueFormat = "{0}";
		public string YAxisValueFormat = "{0}";
		public int VisibleNodeCount = 10;
		public string XAxisLabel = "";
		public string YAxisLabel = "";
		public bool DisplayYAxisZero = true;

		public ObserverStatsGraphWidget()
			: base()
		{
			GetValueFormat = () => ValueFormat;
			GetXAxisValueFormat = () => XAxisValueFormat;
			GetYAxisValueFormat = () => YAxisValueFormat;
			GetVisibleNodeCount = () => VisibleNodeCount;
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
			GetVisibleNodeCount = other.GetVisibleNodeCount;
			GetXAxisLabel = other.GetXAxisLabel;
			GetYAxisLabel = other.GetYAxisLabel;
			GetDisplayYAxisZero = other.GetDisplayYAxisZero;
			ValueFormat = other.ValueFormat;
			XAxisValueFormat = other.XAxisValueFormat;
			YAxisValueFormat = other.YAxisValueFormat;
			VisibleNodeCount = other.VisibleNodeCount;
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

			var visibleNodeCount = GetVisibleNodeCount();
			var visibleNodeStep = width / visibleNodeCount;
			var actualNodeCount = GetDataSource().First().Second.Count();
			var visibleNodeStart = Math.Max(0, actualNodeCount - visibleNodeCount);
			var visibleNodeEnd = Math.Max(actualNodeCount, visibleNodeCount);

			float maxValue = GetDataSource().Select(p => p.Second).SelectMany(d => d).Max();
			var scale = (100 / maxValue) * 3;

			//todo: make this stuff not draw outside of the RenderBounds
			for (int n = visibleNodeStart, i = 0; n <= visibleNodeEnd; n++, i++)
			{
				var x = i * visibleNodeStep;
				Game.Renderer.LineRenderer.DrawLine(origin + new float2(x, 0), origin + new float2(x, -5), Color.White, Color.White);
				tinyBold.DrawText(GetXAxisValueFormat().F(n), origin + new float2(x, 2), Color.White);
			} 
			bold.DrawText(GetXAxisLabel(), origin + new float2(width / 2, 20), Color.White);

			for (var i = (GetDisplayYAxisZero() ? 0f : height / 10); i <= height; i += height / 10)
			{
				var value = i / scale;
				Game.Renderer.LineRenderer.DrawLine(origin + new float2(width - 5, -i), origin + new float2(width, -i), Color.White, Color.White);
				tinyBold.DrawText(GetYAxisValueFormat().F(value), origin + new float2(width + 2, -i), Color.White);
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
					data = data.Reverse().Take(visibleNodeCount).Reverse();
					var scaledData = data.Select(d => d * scale);
					var x = 0f;
					scaledData.Aggregate((a, b) =>
					{
						Game.Renderer.LineRenderer.DrawLine(
							origin + new float2(x, -a),
							origin + new float2(x + visibleNodeStep, -b),
							color, color);
						x += visibleNodeStep;
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
