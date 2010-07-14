#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	class PerfGraphWidget : Widget
	{
		public PerfGraphWidget() : base() { }

		public override void DrawInner(World world)
		{
			var rect = RenderBounds;
			float2 origin = Game.viewport.Location + new float2(rect.Right, rect.Bottom);
			float2 basis = new float2(-rect.Width / 100, -rect.Height / 100);

			Game.chrome.lineRenderer.DrawLine(origin, origin + new float2(100, 0) * basis, Color.White, Color.White);
			Game.chrome.lineRenderer.DrawLine(origin + new float2(100, 0) * basis, origin + new float2(100, 100) * basis, Color.White, Color.White);

			foreach (var item in PerfHistory.items.Values)
			{
				int n = 0;
				item.Samples().Aggregate((a, b) =>
				{
					Game.chrome.lineRenderer.DrawLine(
						origin + new float2(n, (float)a) * basis,
						origin + new float2(n + 1, (float)b) * basis,
						item.c, item.c);
					++n;
					return b;
				});
			}

			Game.chrome.lineRenderer.Flush();
		}
	}
}