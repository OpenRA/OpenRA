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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	static class EditorOverlayCellGrid
	{
		static readonly Color GridColor = Color.FromArgb(192, 160, 160, 160);

		public static IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var map = wr.World.Map;

			foreach (var uv in wr.Viewport.AllVisibleCells.CandidateMapCoords)
			{
				if (!map.Contains(uv) || wr.World.ShroudObscures(uv))
					continue;

				var cell = uv.ToCPos(map);
				var ramp = map.Grid.Ramps[map.Ramp[cell]];
				var pos = map.CenterOfCell(cell) - new WVec(0, 0, ramp.CenterHeightOffset);

				foreach (var polygon in ramp.Polygons)
				{
					for (var i = 0; i < polygon.Length; i++)
					{
						var j = (i + 1) % polygon.Length;
						yield return new LineAnnotationRenderable(pos + polygon[i], pos + polygon[j], 1, GridColor);
					}
				}
			}
		}
	}
}
