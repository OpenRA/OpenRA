#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using System.Linq;

using SGraphics = System.Drawing.Graphics;

namespace OpenRA.Editor
{
	class WaypointTool : ITool
	{
		WaypointTemplate Waypoint;

		public WaypointTool(WaypointTemplate waypoint) { Waypoint = waypoint; }

		public void Apply(Surface surface)
		{
			var k = surface.Map.Waypoints.FirstOrDefault(a => a.Value == surface.GetBrushLocation());
			if (k.Key != null) surface.Map.Waypoints.Remove(k.Key);

			surface.Map.Waypoints.Add(surface.NextWpid(), surface.GetBrushLocation());
		}

		public void Preview(Surface surface, SGraphics g)
		{
			g.DrawRectangle(Pens.LimeGreen,
					surface.TileSet.TileSize * surface.GetBrushLocation().X * surface.Zoom + surface.GetOffset().X + 4,
					surface.TileSet.TileSize * surface.GetBrushLocation().Y * surface.Zoom + surface.GetOffset().Y + 4,
					(surface.TileSet.TileSize - 8) * surface.Zoom, (surface.TileSet.TileSize - 8) * surface.Zoom);
		}
	}
}
