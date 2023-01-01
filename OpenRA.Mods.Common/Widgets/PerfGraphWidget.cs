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

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class PerfGraphWidget : Widget
	{
		public override void Draw()
		{
			var cr = Game.Renderer.RgbaColorRenderer;
			var rect = RenderBounds;
			var origin = new float2(rect.Right, rect.Bottom);
			var basis = new float2(-rect.Width / 100, -rect.Height / 100);

			cr.DrawLine(new[]
			{
				new float3(rect.Left, rect.Top, 0),
				new float3(rect.Left, rect.Bottom, 0),
				new float3(rect.Right, rect.Bottom, 0)
			}, 1, Color.White);

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
			foreach (var item in PerfHistory.Items.Values)
			{
				Game.Renderer.Fonts["Tiny"].DrawText(item.Name, new float2(rect.Left, rect.Top) + new float2(18, 10 * k - 3), Color.White);
				++k;
			}
		}
	}
}
