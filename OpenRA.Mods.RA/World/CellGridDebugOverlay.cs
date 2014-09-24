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
	public class CellGridDebugOverlayInfo : ITraitInfo
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

		public object Create(ActorInitializer init) { return new CellGridDebugOverlay(this); }
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

	public class CellGridDebugOverlay : IRenderOverlay, IPostRender, IResolveOrder
	{
		readonly TileShape tileShape;

		public readonly CellGridDebugOverlayInfo Info;
		public bool RenderFullGrid;
		public bool RenderHalfGrid;
		public GridType GridType;
		public int GridRadius;
		public RenderOrder RenderOrder;
		public Color FullCellColor;
		public Color HalfCellColor;
		public bool Visible;

		public CellGridDebugOverlay(CellGridDebugOverlayInfo info)
		{
			Info = info;
			tileShape = info.TileShape;

			RenderFullGrid = info.RenderFullGrid;
			RenderHalfGrid = info.RenderHalfGrid;
			GridType = info.GridType;
			GridRadius = info.GridRadius;
			RenderOrder = info.RenderOrder;
			FullCellColor = info.FullCellColor;
			HalfCellColor = info.HalfCellColor;
			Visible = false;
		}

		public void SwapRenderOrder()
		{
			if (RenderOrder == RenderOrder.BeforeActors)
				RenderOrder = RenderOrder.AfterActors;
			else
				RenderOrder = RenderOrder.BeforeActors;
		}

		public void SwapGridType()
		{
			if (GridType == GridType.FullMap)
				GridType = GridType.MouseRadius;
			else if (GridType == GridType.MouseRadius)
				GridType = GridType.FullMap;
		}

		public void SetRadius(int radius)
		{
			if (radius < 0)
				radius = Math.Abs(radius);

			// `Map` has a hard-coded limit
			if (radius > 50)
				radius = 50;

			GridRadius = radius;
		}

		public void ResetAll()
		{
			RenderFullGrid = Info.RenderFullGrid;
			RenderHalfGrid = Info.RenderHalfGrid;
			GridType = Info.GridType;
			GridRadius = Info.GridRadius;
			RenderOrder = Info.RenderOrder;
			FullCellColor = Info.FullCellColor;
			HalfCellColor = Info.HalfCellColor;
			Visible = true;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DevCellDebug")
				Visible = !Visible;

			if (order.OrderString == "DevCellDebug.above")
				RenderOrder = RenderOrder.AfterActors;

			if (order.OrderString == "DevCellDebug.below")
				RenderOrder = RenderOrder.BeforeActors;

			if (order.OrderString == "DevCellDebug.full")
				RenderFullGrid = !RenderFullGrid;

			if (order.OrderString == "DevCellDebug.half")
				RenderHalfGrid = !RenderHalfGrid;

			if (order.OrderString == "DevCellDebug.type")
				SwapGridType();

			if (order.OrderString.StartsWith("DevCellDebug.range"))
			{
				var value = order.OrderString.Split(' ')[1];
				var radius = 0;
				if (!int.TryParse(value, out radius))
				{
					Game.Debug("`{0}` is not a valid integer.", value);
					return;
				}

				SetRadius(radius);
			}

			if (order.OrderString == "DevCellDebug.reset")
				ResetAll();

		}

		void DoRender(WorldRenderer wr, IEnumerable<CPos> cells)
		{
			foreach (var cell in cells)
			{
				var lr = Game.Renderer.WorldLineRenderer;
				var pos = wr.world.Map.CenterOfCell(cell);

				if (tileShape == TileShape.Diamond)
				{
					if (RenderFullGrid)
					{
						var top = wr.ScreenPxPosition(pos + new WVec(0, -512, 0)).ToFloat2();
						var right = wr.ScreenPxPosition(pos + new WVec(512, 0, 0)).ToFloat2();
						var bottom = wr.ScreenPxPosition(pos + new WVec(0, 512, 0)).ToFloat2();
						var left = wr.ScreenPxPosition(pos + new WVec(-512, 0, 0)).ToFloat2();

						lr.DrawLine(top, right, FullCellColor, FullCellColor);
						lr.DrawLine(right, bottom, FullCellColor, FullCellColor);
						lr.DrawLine(bottom, left, FullCellColor, FullCellColor);
						lr.DrawLine(left, top, FullCellColor, FullCellColor);
					}

					if (RenderHalfGrid)
					{
						var center = wr.ScreenPxPosition(pos);
						var topRight = wr.ScreenPxPosition(pos + new WVec(256, -256, 0)).ToFloat2();
						var bottomRight = wr.ScreenPxPosition(pos + new WVec(256, 256, 0)).ToFloat2();
						var bottomLeft = wr.ScreenPxPosition(pos + new WVec(-256, 256, 0)).ToFloat2();
						var topLeft = wr.ScreenPxPosition(pos + new WVec(-256, -256, 0)).ToFloat2();

						lr.DrawLine(center, topRight, HalfCellColor, HalfCellColor);
						lr.DrawLine(center, bottomRight, HalfCellColor, HalfCellColor);
						lr.DrawLine(center, bottomLeft, HalfCellColor, HalfCellColor);
						lr.DrawLine(center, topLeft, HalfCellColor, HalfCellColor);
					}
				}
				else if (tileShape == TileShape.Rectangle)
				{
					if (RenderFullGrid)
					{
						var topLeft = wr.ScreenPxPosition(pos + new WVec(-512, -512, 0)).ToFloat2();
						var topRight = wr.ScreenPxPosition(pos + new WVec(512, -512, 0)).ToFloat2();
						var bottomRight = wr.ScreenPxPosition(pos + new WVec(512, 512, 0)).ToFloat2();
						var bottomLeft = wr.ScreenPxPosition(pos + new WVec(-512, 512, 0)).ToFloat2();

						lr.DrawLine(topLeft, topRight, FullCellColor, FullCellColor);
						lr.DrawLine(topRight, bottomRight, FullCellColor, FullCellColor);
						lr.DrawLine(bottomRight, bottomLeft, FullCellColor, FullCellColor);
						lr.DrawLine(bottomLeft, topLeft, FullCellColor, FullCellColor);
					}

					if (RenderHalfGrid)
					{
						var center = wr.ScreenPxPosition(pos);
						var top = wr.ScreenPxPosition(pos + new WVec(0, -512, 0)).ToFloat2();
						var bottom = wr.ScreenPxPosition(pos + new WVec(0, 512, 0)).ToFloat2();
						var left = wr.ScreenPxPosition(pos + new WVec(-512, 0, 0)).ToFloat2();
						var right = wr.ScreenPxPosition(pos + new WVec(512, 0, 0)).ToFloat2();

						lr.DrawLine(center, top, HalfCellColor, HalfCellColor);
						lr.DrawLine(center, bottom, HalfCellColor, HalfCellColor);
						lr.DrawLine(center, left, HalfCellColor, HalfCellColor);
						lr.DrawLine(center, right, HalfCellColor, HalfCellColor);
					}
				}
			}
		}

		public void Render(WorldRenderer wr)
		{
			if (RenderOrder == RenderOrder.BeforeActors)
				RenderDecision(wr);
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (RenderOrder == RenderOrder.AfterActors)
				RenderDecision(wr);
		}

		void RenderDecision(WorldRenderer wr)
		{
			if (!Visible)
				return;

			if (GridType == GridType.MouseRadius)
			{
				var mouseCenter = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var cellsInRange = wr.world.Map.FindTilesInCircle(mouseCenter, GridRadius);
				DoRender(wr, cellsInRange);
			}
			else
				DoRender(wr, wr.Viewport.VisibleCells.ToList());
		}
	}
}
