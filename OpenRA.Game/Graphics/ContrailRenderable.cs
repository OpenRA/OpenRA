#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;

namespace OpenRA.Graphics
{
	public struct ContrailRenderable : IRenderable
	{
		public int Length { get { return trail.Length; } }

		readonly World world;

		// Store trail positions in a circular buffer
		readonly WPos[] trail;
		int next;
		int length;
		int skip;

		readonly Color color;
		readonly int zOffset;

		public ContrailRenderable(World world, Color color, int length, int skip, int zOffset)
			: this(world, new WPos[length], 0, 0, skip, color, zOffset) {}

		ContrailRenderable(World world, WPos[] trail, int next, int length, int skip, Color color, int zOffset)
		{
			this.world = world;
			this.trail = trail;
			this.next = next;
			this.length = length;
			this.skip = skip;
			this.color = color;
			this.zOffset = zOffset;
		}

		public WPos Pos { get { return trail[idx(next-1)]; } }
		public float Scale { get { return 1f; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithScale(float newScale) { return new ContrailRenderable(world, (WPos[])trail.Clone(), next, length, skip, color, zOffset); }
		public IRenderable WithPalette(PaletteReference newPalette) { return new ContrailRenderable(world, (WPos[])trail.Clone(), next, length, skip, color, zOffset); }
		public IRenderable WithZOffset(int newOffset) { return new ContrailRenderable(world, (WPos[])trail.Clone(), next, length, skip, color, newOffset); }
		public IRenderable OffsetBy(WVec vec) { return new ContrailRenderable(world, trail.Select(pos => pos + vec).ToArray(), next, length, skip, color, zOffset); }
		public IRenderable AsDecoration() { return this; }

		public void BeforeRender(WorldRenderer wr) {}
		public void Render(WorldRenderer wr)
		{
			// Need at least 4 points to smooth the contrail over
			if (length - skip < 4)
				return;

			var wlr = Game.Renderer.WorldLineRenderer;
			var oldWidth = wlr.LineWidth;
			wlr.LineWidth = wr.Viewport.Zoom;

			// Start of the first line segment is the tail of the list - don't smooth it.
			var curPos = trail[idx(next - skip - 1)];
			var curCell = curPos.ToCPos();
			var curColor = color;
			for (var i = 0; i < length - skip - 4; i++)
			{
				var j = next - skip - i - 2;
				var nextPos = Average(trail[idx(j)], trail[idx(j-1)], trail[idx(j-2)], trail[idx(j-3)]);
				var nextCell = nextPos.ToCPos();
				var nextColor = Exts.ColorLerp(i * 1f / (length - 4), color, Color.Transparent);

				if (!world.FogObscures(curCell) && !world.FogObscures(nextCell))
					wlr.DrawLine(wr.ScreenPosition(curPos), wr.ScreenPosition(nextPos), curColor, nextColor);

				curPos = nextPos;
				curCell = nextCell;
				curColor = nextColor;
			}

			wlr.LineWidth = oldWidth;
		}

		public void RenderDebugGeometry(WorldRenderer wr) {}

		// Array index modulo length
		int idx(int i)
		{
			var j = i % trail.Length;
			return j < 0 ? j + trail.Length : j;
		}

		static WPos Average(params WPos[] list)
		{
			return list.Average();
		}

		public void Update(WPos pos)
		{
			trail[next] = pos;
			next = idx(next+1);

			if (length < trail.Length)
				length++;
		}

		public static Color ChooseColor(Actor self)
		{
			var ownerColor = Color.FromArgb(255, self.Owner.Color.RGB);
			return Exts.ColorLerp(0.5f, ownerColor, Color.White);
		}
	}
}
