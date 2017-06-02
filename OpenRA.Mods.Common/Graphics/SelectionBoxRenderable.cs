#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Graphics
{
	public struct SelectionBoxRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly Rectangle visualBounds;
		readonly Color color;
		readonly IEnumerable<CPos> cells;
		readonly float isometricHeight;

		public SelectionBoxRenderable(Actor actor, Color color, float isometricHeight = 0)
		{
			this.pos = actor.CenterPosition;
			this.visualBounds = actor.VisualBounds;
			this.color = color;
			var building = actor.TraitOrDefault<Building>();
			if (building != null)
				this.cells = FootprintUtils.Tiles(actor);
			else
				this.cells = new CPos[1] { actor.Location };

			// if isometricHeight == 0, we won't use isometric mode
			this.isometricHeight = isometricHeight;
		}

		private SelectionBoxRenderable(WPos pos, Rectangle visualBounds, Color color, IEnumerable<CPos> cells, float isometricHeight)
		{
			this.pos = pos;
			this.visualBounds = visualBounds;
			this.color = color;
			this.cells = cells;
			this.isometricHeight = isometricHeight;
		}

		public WPos Pos { get { return pos; } }

		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(WVec vec) { return new SelectionBoxRenderable(pos + vec, visualBounds, color, cells, isometricHeight); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

	    private void RenderRectangular(WorldRenderer wr)
	    {
            var iz = 1 / wr.Viewport.Zoom;
            var screenPos = wr.Screen3DPxPosition(pos);
            var tl = screenPos + new float2(visualBounds.Left, visualBounds.Top);
            var br = screenPos + new float2(visualBounds.Right, visualBounds.Bottom);
            var tr = new float3(br.X, tl.Y, screenPos.Z);
            var bl = new float3(tl.X, br.Y, screenPos.Z);
            var u = new float2(4 * iz, 0);
            var v = new float2(0, 4 * iz);

            var wcr = Game.Renderer.WorldRgbaColorRenderer;
            wcr.DrawLine(new[] { tl + u, tl, tl + v }, iz, color, true);
            wcr.DrawLine(new[] { tr - u, tr, tr + v }, iz, color, true);
            wcr.DrawLine(new[] { br - u, br, br - v }, iz, color, true);
            wcr.DrawLine(new[] { bl + u, bl, bl - v }, iz, color, true);
        }

		private void RenderIsometric(WorldRenderer wr)
		{
			var iz = 1 / wr.Viewport.Zoom;

			var xMin = cells.Min(c => c.X);
			var xMax = cells.Max(c => c.X);
			var tc = cells.Where(c => c.X == xMin).MinBy(c => c.Y);
			var lc = cells.Where(c => c.X == xMin).MaxBy(c => c.Y);
			var rc = cells.Where(c => c.X == xMax).MinBy(c => c.Y);

			var tp = wr.Screen3DPxPosition(wr.World.Map.CenterOfCell(tc) + new WVec(0, -724, 0)).Round();
			var lp = wr.Screen3DPxPosition(wr.World.Map.CenterOfCell(lc) + new WVec(-724, 0, 0)).Round();
			var rp = wr.Screen3DPxPosition(wr.World.Map.CenterOfCell(rc) + new WVec(724, 0, 0)).Round();

			var a0 = new float2(0f, -6f * iz);
			var a2 = new float2(6f * iz, -3f * iz);
			var a4 = new float2(6f * iz, 3f * iz);
			var a6 = new float2(0f, 12f * iz);
			var a8 = new float2(-6f * iz, 3f * iz);
			var a10 = new float2(-6f * iz, -3f * iz);

			var top = new float3(0, -isometricHeight, 0);

			var wcr = Game.Renderer.WorldRgbaColorRenderer;

			// Top lines
			wcr.DrawLine(new[] { tp + a4 + top, tp + top, tp + a6 + top }, iz, color, true);
			wcr.DrawLine(new[] { tp + top, tp + a8 + top }, iz, color, true);
			wcr.DrawLine(new[] { lp + a2 + top, lp + top, lp + a6 + top }, iz, color, true);
			wcr.DrawLine(new[] { rp + a10 + top, rp + top }, iz, color, true);
			wcr.DrawLine(new[] { rp + a8 + top, rp + top, rp + a6 + top }, iz, color, true);

			// Bottom lines
			wcr.DrawLine(new[] { lp + a0, lp, lp + a4 }, iz, color, true);
			wcr.DrawLine(new[] { rp + a0, rp, rp + a8 }, iz, color, true);
		}

		public void Render(WorldRenderer wr)
		{
		    if (wr.World.Map.Grid.Type == MapGridType.RectangularIsometric && isometricHeight > 0)
		        RenderIsometric(wr);
		    else
		        RenderRectangular(wr);
		}

	    public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
