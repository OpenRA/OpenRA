#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class PerfGraphWidget : Widget
	{
		public override void Draw()
		{
			var rect = RenderBounds;
			var origin = new float2(rect.Right, rect.Bottom);
			var basis = new float2(-rect.Width / 100, -rect.Height / 100);

			Game.Renderer.LineRenderer.DrawLine(origin, origin + new float2(100, 0) * basis, Color.White);
			Game.Renderer.LineRenderer.DrawLine(origin + new float2(100, 0) * basis, origin + new float2(100, 100) * basis, Color.White);

			var k = 0;
			foreach (var item in PerfHistory.Items.Values)
			{
				Game.Renderer.LineRenderer.DrawLineStrip(
					item.Samples().Select((sample, i) => origin + new float2(i, (float)sample) * basis), item.C);

				var u = new float2(rect.Left, rect.Top);

				Game.Renderer.LineRenderer.DrawLine(
					u + new float2(10, 10 * k + 5),
					u + new float2(12, 10 * k + 5),
					item.C);

				Game.Renderer.LineRenderer.DrawLine(
					u + new float2(10, 10 * k + 4),
					u + new float2(12, 10 * k + 4),
					item.C);

				++k;
			}

			k = 0;
			foreach (var item in PerfHistory.Items.Values)
			{
				Game.Renderer.Fonts["Tiny"].DrawText(item.Name, new float2(rect.Left, rect.Top) + new float2(18, 10 * k - 3), Color.White);
				++k;
			}
		}
	}
}