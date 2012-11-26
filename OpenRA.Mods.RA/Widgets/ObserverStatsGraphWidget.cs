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
		public Func<IEnumerable<Pair<Player, IEnumerable<float>>>> GetDataSource = () => null;
		public Func<float> GetDataScale = () => 1.0f;
		public Func<string> GetLastValueFormat = () => "{0}";
		public Func<int> GetNodeCount = () => 20;
		public Func<int> GetNodeStep = () => 5;

		public ObserverStatsGraphWidget() : base() { }

		protected ObserverStatsGraphWidget(ObserverStatsGraphWidget other)
			: base(other)
		{
			GetDataSource = other.GetDataSource;
		}

		public override void Draw()
		{
			var rect = RenderBounds;
			var origin = new float2(rect.Left, rect.Bottom);
			var basis = new float2(rect.Width / 100, rect.Height / 100);

			Game.Renderer.LineRenderer.DrawLine(origin, origin + new float2(100, 0) * basis, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(origin, origin - new float2(0, 100) * basis, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(origin + new float2(100, 0) * basis, origin + new float2(100, -100) * basis, Color.White, Color.White);

			var tinyBold = Game.Renderer.Fonts["TinyBold"];

			var i = 0;
			foreach (var pair in GetDataSource())
			{
				var player = pair.First;
				var data = pair.Second.Reverse().Take(GetNodeCount()).Reverse();
				var color = player.ColorRamp.GetColor(0);
				if (data.Any())
				{
					var scale = GetDataScale();
					var scaledData = data.Select(d => d * scale);
					var n = 0;
					var step = GetNodeStep();
					scaledData.Aggregate((a, b) =>
					{
						Game.Renderer.LineRenderer.DrawLine(
							origin + new float2(n, -a) * basis,
							origin + new float2(n + step, -b) * basis,
							color, color);
						n += step;
						return b;
					});

					var lastValue = data.Last();
					if (lastValue != 0)
					{
						var scaledLastValue = lastValue * scale;
						var lastValueFormat = GetLastValueFormat();
						if (lastValueFormat != null)
						{
							tinyBold.DrawText(lastValueFormat.F(lastValue), origin + new float2(n, -scaledLastValue - 2) * basis, color);
						}
					}
				}

				tinyBold.DrawText(player.PlayerName, new float2(rect.Left, rect.Top) + new float2(5, 10 * i - 3), color);
				i++;
			}
		}

		public override Widget Clone()
		{
			return new ObserverStatsGraphWidget(this);
		}
	}
}
