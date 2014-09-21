 #region Copyright & License Information
 /*
  * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
  * This file is part of OpenRA, which is free software. It is made
  * available to you under the terms of the GNU General Public License
  * as published by the Free Software Foundation. For more information,
  * see COPYING.
  */
 #endregion

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA
{
	[Desc("Used to show cell size and location. Attach this to the world actor.")]
	public class CellDebugOverlayInfo : ITraitInfo
	{
		[Desc("TileShape of cell artwork. RA/D2k/CNC use `Rectangle`, TS uses `Diamond`.")]
		public readonly TileShape TileShape = TileShape.Rectangle;

		[Desc("When to render cell grids. BeforeWorld = Under actors, AfterWorld = Above actors.", "Cycle with `/grid above` and `/grid below`.")]
		public readonly RenderOrder RenderOrder = RenderOrder.BeforeActors;

		[Desc("Initially render fullcell grid?")]
		public readonly bool RenderFullGrid = true;

		[Desc("Initially render halfcell grid?")]
		public readonly bool RenderHalfGrid = true;

		[Desc("Color for LineRenderer to render for fullcell grid.")]
		public readonly Color FullCellColor = Color.Red;

		[Desc("Color for LineRenderer to render for halfcell grid.")]
		public readonly Color HalfCellColor = Color.DarkCyan;

		[Desc("Render the grid on the entire map or surrounding the mouse?", "`FullMap` or `MouseRadius`")]
		public readonly GridType GridType = GridType.FullMap;

		[Desc("If GridType is `SurroundingMouse` the grid will be rendered in a radius of this many cells.")]
		public readonly int GridRadius = 5;

		public object Create(ActorInitializer init) { return new CellDebugOverlay(this); }
	}

	public enum RenderOrder
	{
		BeforeActors,
		AfterActors
	}

	public enum GridType
	{
		MouseRadius,
		FullMap
	}

	public class CellDebugOverlay : IRenderOverlay, IPostRender, IResolveOrder
	{
		readonly CellDebugOverlayInfo info;
		readonly TileShape tileShape;

		bool renderFullGrid;
		bool renderHalfGrid;
		RenderOrder renderOrder;
		GridType gridType;
		int gridRadius;

		public bool Visible;

		public CellDebugOverlay(CellDebugOverlayInfo info)
		{
			this.info = info;
			tileShape = info.TileShape;

			renderFullGrid = info.RenderFullGrid;
			renderHalfGrid = info.RenderHalfGrid;
			renderOrder = info.RenderOrder;
			gridType = info.GridType;
			gridRadius = info.GridRadius;

			Visible = false;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DevCellDebug")
				Visible = !Visible;

			if (order.OrderString == "DevCellDebug.above")
				renderOrder = RenderOrder.AfterActors;

			if (order.OrderString == "DevCellDebug.below")
				renderOrder = RenderOrder.BeforeActors;

			if (order.OrderString == "DevCellDebug.full")
				renderFullGrid = !renderFullGrid;

			if (order.OrderString == "DevCellDebug.half")
				renderHalfGrid = !renderHalfGrid;

			if (order.OrderString == "DevCellDebug.type")
			{
				if (gridType == GridType.FullMap)
					gridType = GridType.MouseRadius;
				else if (gridType == GridType.MouseRadius)
					gridType = GridType.FullMap;
			}

			if (order.OrderString.StartsWith("DevCellDebug.range"))
			{
				var value = order.OrderString.Split(' ')[1];
				if (!int.TryParse(value, out gridRadius))
				{
					gridRadius = info.GridRadius;
					Game.Debug("{0} is not a valid integer.", value);
				}

				if (gridRadius < 0)
					gridRadius = Math.Abs(gridRadius);
			}

			if (order.OrderString == "DevCellDebug.reset")
			{
				renderOrder = info.RenderOrder;
				renderFullGrid = info.RenderFullGrid;
				renderHalfGrid = info.RenderHalfGrid;
				renderOrder = info.RenderOrder;
				gridType = info.GridType;
				gridRadius = info.GridRadius;
				Visible = true;
			}

		}

		void DoRender(WorldRenderer wr, IEnumerable<CPos> cells)
		{
			foreach (var cell in cells)
			{
				var lr = Game.Renderer.WorldLineRenderer;
				var pos = wr.world.Map.CenterOfCell(cell);
				var fullColor = info.FullCellColor;
				var halfColor = info.HalfCellColor;

				if (tileShape == TileShape.Diamond)
				{
					if (renderFullGrid)
					{
						var top = wr.ScreenPxPosition(pos + new WVec(0, -512, 0)).ToFloat2();
						var right = wr.ScreenPxPosition(pos + new WVec(512, 0, 0)).ToFloat2();
						var bottom = wr.ScreenPxPosition(pos + new WVec(0, 512, 0)).ToFloat2();
						var left = wr.ScreenPxPosition(pos + new WVec(-512, 0, 0)).ToFloat2();

						lr.DrawLine(top, right, fullColor, fullColor);
						lr.DrawLine(right, bottom, fullColor, fullColor);
						lr.DrawLine(bottom, left, fullColor, fullColor);
						lr.DrawLine(left, top, fullColor, fullColor);
					}

					if (renderHalfGrid)
					{
						var center = wr.ScreenPxPosition(pos);
						var topRight = wr.ScreenPxPosition(pos + new WVec(256, -256, 0)).ToFloat2();
						var bottomRight = wr.ScreenPxPosition(pos + new WVec(256, 256, 0)).ToFloat2();
						var bottomLeft = wr.ScreenPxPosition(pos + new WVec(-256, 256, 0)).ToFloat2();
						var topLeft = wr.ScreenPxPosition(pos + new WVec(-256, -256, 0)).ToFloat2();

						lr.DrawLine(center, topRight, halfColor, halfColor);
						lr.DrawLine(center, bottomRight, halfColor, halfColor);
						lr.DrawLine(center, bottomLeft, halfColor, halfColor);
						lr.DrawLine(center, topLeft, halfColor, halfColor);
					}
				}
				else if (tileShape == TileShape.Rectangle)
				{
					if (renderFullGrid)
					{
						var topLeft = wr.ScreenPxPosition(pos + new WVec(-512, -512, 0)).ToFloat2();
						var topRight = wr.ScreenPxPosition(pos + new WVec(512, -512, 0)).ToFloat2();
						var bottomRight = wr.ScreenPxPosition(pos + new WVec(512, 512, 0)).ToFloat2();
						var bottomLeft = wr.ScreenPxPosition(pos + new WVec(-512, 512, 0)).ToFloat2();

						lr.DrawLine(topLeft, topRight, fullColor, fullColor);
						lr.DrawLine(topRight, bottomRight, fullColor, fullColor);
						lr.DrawLine(bottomRight, bottomLeft, fullColor, fullColor);
						lr.DrawLine(bottomLeft, topLeft, fullColor, fullColor);
					}

					if (renderHalfGrid)
					{
						var center = wr.ScreenPxPosition(pos);
						var top = wr.ScreenPxPosition(pos + new WVec(0, -512, 0)).ToFloat2();
						var bottom = wr.ScreenPxPosition(pos + new WVec(0, 512, 0)).ToFloat2();
						var left = wr.ScreenPxPosition(pos + new WVec(-512, 0, 0)).ToFloat2();
						var right = wr.ScreenPxPosition(pos + new WVec(512, 0, 0)).ToFloat2();

						lr.DrawLine(center, top, halfColor, halfColor);
						lr.DrawLine(center, bottom, halfColor, halfColor);
						lr.DrawLine(center, left, halfColor, halfColor);
						lr.DrawLine(center, right, halfColor, halfColor);
					}
				}
			}
		}

		public void Render(WorldRenderer wr)
		{
			if (renderOrder == RenderOrder.BeforeActors)
				RenderDecision(wr);
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (renderOrder == RenderOrder.AfterActors)
				RenderDecision(wr);
		}

		void RenderDecision(WorldRenderer wr)
		{
			if (!Visible)
				return;

			if (gridType == GridType.MouseRadius)
			{
				var mouseCenter = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var cellsInRange = wr.world.Map.FindTilesInCircle(mouseCenter, gridRadius);
				DoRender(wr, cellsInRange);
			}
			else
				DoRender(wr, wr.Viewport.VisibleCells.ToList());
		}
	}
}
