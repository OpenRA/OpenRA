#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRA.Graphics
{
	[Flags]
	public enum ScrollDirection { None = 0, Up = 1, Left = 2, Down = 4, Right = 8 }

	public static class ViewportExts
	{
		public static bool Includes(this ScrollDirection d, ScrollDirection s)
		{
			return (d & s) == s;
		}

		public static ScrollDirection Set(this ScrollDirection d, ScrollDirection s, bool val)
		{
			return (d.Includes(s) != val) ? d ^ s : d;
		}
	}

	public class Viewport
	{
		readonly WorldRenderer worldRenderer;

		// Map bounds (world-px)
		readonly Rectangle mapBounds;

		readonly int maxGroundHeight;

		// Viewport geometry (world-px)
		public int2 CenterLocation { get; private set; }

		public WPos CenterPosition { get { return worldRenderer.Position(CenterLocation); } }

		public int2 TopLeft { get { return CenterLocation - viewportSize / 2; } }
		public int2 BottomRight { get { return CenterLocation + viewportSize / 2; } }
		int2 viewportSize;
		CellRegion cells;
		bool cellsDirty = true;

		float zoom = 1f;
		public float Zoom
		{
			get
			{
				return zoom;
			}

			set
			{
				zoom = value;
				viewportSize = (1f / zoom * new float2(Game.Renderer.Resolution)).ToInt2();
				cellsDirty = true;
			}
		}

		public static int TicksSinceLastMove = 0;
		public static int2 LastMousePos;

		public ScrollDirection GetBlockedDirections()
		{
			var ret = ScrollDirection.None;
			if (CenterLocation.Y <= mapBounds.Top)
				ret |= ScrollDirection.Up;
			if (CenterLocation.X <= mapBounds.Left)
				ret |= ScrollDirection.Left;
			if (CenterLocation.Y >= mapBounds.Bottom)
				ret |= ScrollDirection.Down;
			if (CenterLocation.X >= mapBounds.Right)
				ret |= ScrollDirection.Right;

			return ret;
		}

		public Viewport(WorldRenderer wr, Map map)
		{
			worldRenderer = wr;

			// Calculate map bounds in world-px
			var b = map.Bounds;

			// Expand to corners of cells
			var tl = wr.ScreenPxPosition(map.CenterOfCell(new MPos(b.Left, b.Top).ToCPos(map)) - new WVec(512, 512, 0));
			var br = wr.ScreenPxPosition(map.CenterOfCell(new MPos(b.Right, b.Bottom).ToCPos(map)) + new WVec(511, 511, 0));
			mapBounds = Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);

			maxGroundHeight = wr.World.TileSet.MaxGroundHeight;
			CenterLocation = (tl + br) / 2;
			Zoom = Game.Settings.Graphics.PixelDouble ? 2 : 1;
		}

		public CPos ViewToWorld(int2 view)
		{
			var world = worldRenderer.Viewport.ViewToWorldPx(view);
			var map = worldRenderer.World.Map;
			var ts = Game.ModData.Manifest.TileSize;
			var candidates = CandidateMouseoverCells(world);
			var tileSet = worldRenderer.World.TileSet;

			foreach (var uv in candidates)
			{
				// Coarse filter to nearby cells
				var p = map.CenterOfCell(uv.ToCPos(map.TileShape));
				var s = worldRenderer.ScreenPxPosition(p);
				if (Math.Abs(s.X - world.X) <= ts.Width && Math.Abs(s.Y - world.Y) <= ts.Height)
				{
					var tile = map.MapTiles.Value[uv];
					var ti = tileSet.GetTileInfo(tile);
					var ramp = ti != null ? ti.RampType : 0;

					var corners = map.CellCorners[ramp];
					var pos = map.CenterOfCell(uv.ToCPos(map));
					var screen = corners.Select(c => worldRenderer.ScreenPxPosition(pos + c)).ToArray();

					if (screen.PolygonContains(world))
						return uv.ToCPos(map);
				}
			}

			// Mouse is not directly over a cell (perhaps on a cliff)
			// Try and find the closest cell
			if (candidates.Any())
			{
				return candidates.OrderBy(uv =>
				{
					var p = map.CenterOfCell(uv.ToCPos(map.TileShape));
					var s = worldRenderer.ScreenPxPosition(p);
					var dx = Math.Abs(s.X - world.X);
					var dy = Math.Abs(s.Y - world.Y);

					return dx * dx + dy * dy;
				}).First().ToCPos(map);
			}

			// Something is very wrong, but lets return something that isn't completely bogus and hope the caller can recover
			return worldRenderer.World.Map.CellContaining(worldRenderer.Position(ViewToWorldPx(view)));
		}

		/// <summary> Returns an unfiltered list of all cells that could potentially contain the mouse cursor</summary>
		IEnumerable<MPos> CandidateMouseoverCells(int2 world)
		{
			var map = worldRenderer.World.Map;
			var minPos = worldRenderer.Position(world);

			// Find all the cells that could potentially have been clicked
			var a = map.CellContaining(minPos - new WVec(1024, 0, 0)).ToMPos(map.TileShape);
			var b = map.CellContaining(minPos + new WVec(512, 512 * maxGroundHeight, 0)).ToMPos(map.TileShape);

			for (var v = b.V; v >= a.V; v--)
				for (var u = b.U; u >= a.U; u--)
					yield return new MPos(u, v);
		}

		public int2 ViewToWorldPx(int2 view) { return (1f / Zoom * view.ToFloat2()).ToInt2() + TopLeft; }
		public int2 WorldToViewPx(int2 world) { return (Zoom * (world - TopLeft).ToFloat2()).ToInt2(); }

		public void Center(IEnumerable<Actor> actors)
		{
			if (!actors.Any())
				return;

			Center(actors.Select(a => a.CenterPosition).Average());
		}

		public void Center(WPos pos)
		{
			CenterLocation = worldRenderer.ScreenPxPosition(pos).Clamp(mapBounds);
			cellsDirty = true;
		}

		public void Scroll(float2 delta, bool ignoreBorders)
		{
			// Convert scroll delta from world-px to viewport-px
			CenterLocation += (1f / Zoom * delta).ToInt2();
			cellsDirty = true;

			if (!ignoreBorders)
				CenterLocation = CenterLocation.Clamp(mapBounds);
		}

		// Rectangle (in viewport coords) that contains things to be drawn
		static readonly Rectangle ScreenClip = Rectangle.FromLTRB(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
		public Rectangle ScissorBounds
		{
			get
			{
				// Visible rectangle in world coordinates (expanded to the corners of the cells)
				var map = worldRenderer.World.Map;
				var ctl = map.CenterOfCell(VisibleCells.TopLeft) - new WVec(512, 512, 0);
				var cbr = map.CenterOfCell(VisibleCells.BottomRight) + new WVec(512, 512, 0);

				// Convert to screen coordinates
				var tl = WorldToViewPx(worldRenderer.ScreenPxPosition(ctl - new WVec(0, 0, ctl.Z))).Clamp(ScreenClip);
				var br = WorldToViewPx(worldRenderer.ScreenPxPosition(cbr - new WVec(0, 0, cbr.Z))).Clamp(ScreenClip);
				return Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);
			}
		}

		public CellRegion VisibleCells
		{
			get
			{
				if (cellsDirty)
				{
					var map = worldRenderer.World.Map;
					var wtl = worldRenderer.Position(TopLeft);
					var wbr = worldRenderer.Position(BottomRight);

					// Due to diamond tile staggering, we need to adjust the top-left bounds outwards by half a cell.
					if (map.TileShape == TileShape.Diamond)
						wtl -= new WVec(512, 512, 0);

					// Visible rectangle in map coordinates.
					var dy = map.TileShape == TileShape.Diamond ? 512 : 1024;
					var ctl = new MPos(wtl.X / 1024, wtl.Y / dy);
					var cbr = new MPos(wbr.X / 1024, wbr.Y / dy);

					var tl = map.Clamp(ctl.ToCPos(map));

					// Also need to account for height of cells in rows below the bottom.
					var heightPadding = map.TileShape == TileShape.Diamond ? 2 : 0;
					var br = map.Clamp(new MPos(cbr.U, cbr.V + heightPadding + maxGroundHeight / 2).ToCPos(map));

					cells = new CellRegion(map.TileShape, tl, br);
					cellsDirty = false;
				}

				return cells;
			}
		}
	}
}