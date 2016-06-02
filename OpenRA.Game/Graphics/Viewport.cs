#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
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
			// PERF: Enum.HasFlag is slower and requires allocations.
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
		readonly Size tileSize;

		// Viewport geometry (world-px)
		public int2 CenterLocation { get; private set; }

		public WPos CenterPosition { get { return worldRenderer.ProjectedPosition(CenterLocation); } }

		public int2 TopLeft { get { return CenterLocation - viewportSize / 2; } }
		public int2 BottomRight { get { return CenterLocation + viewportSize / 2; } }
		int2 viewportSize;
		ProjectedCellRegion cells;
		bool cellsDirty = true;

		ProjectedCellRegion allCells;
		bool allCellsDirty = true;
		readonly float[] availableZoomSteps = new[] { 2f, 1f, 0.5f, 0.25f };

		float zoom = 1f;

		public float[] AvailableZoomSteps
		{
			get { return availableZoomSteps; }
		}

		public float Zoom
		{
			get
			{
				return zoom;
			}

			set
			{
				var newValue = ClosestTo(AvailableZoomSteps, value);
				zoom = newValue;
				viewportSize = (1f / zoom * new float2(Game.Renderer.Resolution)).ToInt2();
				cellsDirty = true;
				allCellsDirty = true;
			}
		}

		public static int TicksSinceLastMove = 0;
		public static int2 LastMousePos;

		float ClosestTo(float[] collection, float target)
		{
			var closestValue = collection.First();
			var subtractResult = Math.Abs(closestValue - target);

			foreach (var element in collection)
			{
				if (Math.Abs(element - target) < subtractResult)
				{
					subtractResult = Math.Abs(element - target);
					closestValue = element;
				}
			}

			return closestValue;
		}

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
			var grid = Game.ModData.Manifest.Get<MapGrid>();

			// Calculate map bounds in world-px
			if (wr.World.Type == WorldType.Editor)
			{
				// The full map is visible in the editor
				var width = map.MapSize.X * grid.TileSize.Width;
				var height = map.MapSize.Y * grid.TileSize.Height;
				if (wr.World.Map.Grid.Type == MapGridType.RectangularIsometric)
					height /= 2;

				mapBounds = new Rectangle(0, 0, width, height);
				CenterLocation = new int2(width / 2, height / 2);
			}
			else
			{
				var tl = wr.ScreenPxPosition(map.ProjectedTopLeft);
				var br = wr.ScreenPxPosition(map.ProjectedBottomRight);
				mapBounds = Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);
				CenterLocation = (tl + br) / 2;
			}

			Zoom = Game.Settings.Graphics.PixelDouble ? 2 : 1;
			tileSize = grid.TileSize;
		}

		public CPos ViewToWorld(int2 view)
		{
			var world = worldRenderer.Viewport.ViewToWorldPx(view);
			var map = worldRenderer.World.Map;
			var candidates = CandidateMouseoverCells(world).ToList();
			var tileSet = worldRenderer.World.Map.Rules.TileSet;

			foreach (var uv in candidates)
			{
				// Coarse filter to nearby cells
				var p = map.CenterOfCell(uv.ToCPos(map.Grid.Type));
				var s = worldRenderer.ScreenPxPosition(p);
				if (Math.Abs(s.X - world.X) <= tileSize.Width && Math.Abs(s.Y - world.Y) <= tileSize.Height)
				{
					var ramp = 0;
					if (map.Contains(uv))
					{
						var ti = tileSet.GetTileInfo(map.Tiles[uv]);
						if (ti != null)
							ramp = ti.RampType;
					}

					var corners = map.Grid.CellCorners[ramp];
					var pos = map.CenterOfCell(uv.ToCPos(map));
					var screen = corners.Select(c => worldRenderer.ScreenPxPosition(pos + c)).ToArray();

					if (screen.PolygonContains(world))
						return uv.ToCPos(map);
				}
			}

			// Mouse is not directly over a cell (perhaps on a cliff)
			// Try and find the closest cell
			if (candidates.Count > 0)
			{
				return candidates.OrderBy(uv =>
				{
					var p = map.CenterOfCell(uv.ToCPos(map.Grid.Type));
					var s = worldRenderer.ScreenPxPosition(p);
					var dx = Math.Abs(s.X - world.X);
					var dy = Math.Abs(s.Y - world.Y);

					return dx * dx + dy * dy;
				}).First().ToCPos(map);
			}

			// Something is very wrong, but lets return something that isn't completely bogus and hope the caller can recover
			return worldRenderer.World.Map.CellContaining(worldRenderer.ProjectedPosition(ViewToWorldPx(view)));
		}

		/// <summary> Returns an unfiltered list of all cells that could potentially contain the mouse cursor</summary>
		IEnumerable<MPos> CandidateMouseoverCells(int2 world)
		{
			var map = worldRenderer.World.Map;
			var minPos = worldRenderer.ProjectedPosition(world);

			// Find all the cells that could potentially have been clicked
			var a = map.CellContaining(minPos - new WVec(1024, 0, 0)).ToMPos(map.Grid.Type);
			var b = map.CellContaining(minPos + new WVec(512, 512 * map.Grid.MaximumTerrainHeight, 0)).ToMPos(map.Grid.Type);

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
			allCellsDirty = true;
		}

		public void Scroll(float2 delta, bool ignoreBorders)
		{
			// Convert scroll delta from world-px to viewport-px
			CenterLocation += (1f / Zoom * delta).ToInt2();
			cellsDirty = true;
			allCellsDirty = true;

			if (!ignoreBorders)
				CenterLocation = CenterLocation.Clamp(mapBounds);
		}

		// Rectangle (in viewport coords) that contains things to be drawn
		static readonly Rectangle ScreenClip = Rectangle.FromLTRB(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
		public Rectangle GetScissorBounds(bool insideBounds)
		{
			// Visible rectangle in world coordinates (expanded to the corners of the cells)
			var bounds = insideBounds ? VisibleCellsInsideBounds : AllVisibleCells;
			var map = worldRenderer.World.Map;
			var ctl = map.CenterOfCell(((MPos)bounds.TopLeft).ToCPos(map)) - new WVec(512, 512, 0);
			var cbr = map.CenterOfCell(((MPos)bounds.BottomRight).ToCPos(map)) + new WVec(512, 512, 0);

			// Convert to screen coordinates
			var tl = WorldToViewPx(worldRenderer.ScreenPxPosition(ctl - new WVec(0, 0, ctl.Z))).Clamp(ScreenClip);
			var br = WorldToViewPx(worldRenderer.ScreenPxPosition(cbr - new WVec(0, 0, cbr.Z))).Clamp(ScreenClip);

			// Add an extra one cell fudge in each direction for safety
			return Rectangle.FromLTRB(tl.X - tileSize.Width, tl.Y - tileSize.Height,
				br.X + tileSize.Width, br.Y + tileSize.Height);
		}

		ProjectedCellRegion CalculateVisibleCells(bool insideBounds)
		{
			var map = worldRenderer.World.Map;

			// Calculate the projected cell position at the corners of the visible area
			var tl = (PPos)map.CellContaining(worldRenderer.ProjectedPosition(TopLeft)).ToMPos(map);
			var br = (PPos)map.CellContaining(worldRenderer.ProjectedPosition(BottomRight)).ToMPos(map);

			// RectangularIsometric maps don't have straight edges, and so we need an additional
			// cell margin to include the cells that are half visible on each edge.
			if (map.Grid.Type == MapGridType.RectangularIsometric)
			{
				tl = new PPos(tl.U - 1, tl.V - 1);
				br = new PPos(br.U + 1, br.V + 1);
			}

			// Clamp to the visible map bounds, if requested
			if (insideBounds)
			{
				tl = map.Clamp(tl);
				br = map.Clamp(br);
			}

			return new ProjectedCellRegion(map, tl, br);
		}

		public ProjectedCellRegion VisibleCellsInsideBounds
		{
			get
			{
				if (cellsDirty)
				{
					cells = CalculateVisibleCells(true);
					cellsDirty = false;
				}

				return cells;
			}
		}

		public ProjectedCellRegion AllVisibleCells
		{
			get
			{
				if (allCellsDirty)
				{
					allCells = CalculateVisibleCells(false);
					allCellsDirty = false;
				}

				return allCells;
			}
		}
	}
}