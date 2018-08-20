#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;

namespace OpenRA.Mods.AS.Graphics
{
	public class TintedCell : IRenderable, IFinalizedRenderable, IEffect
	{
		public int Ticks = 0;
		readonly TintedCellsLayer layer;
		readonly CPos cpos;
		readonly WPos wpos;

		public int Level { get; private set; }
		public int ZOffset { get { return layer.Info.ZOffset; } }

		public TintedCell(TintedCellsLayer layer, CPos cpos, WPos wpos)
		{
			this.layer = layer;
			this.cpos = cpos;
			this.wpos = wpos;
		}

		public TintedCell(TintedCell src)
		{
			Ticks = src.Ticks;
			Level = src.Level;
			layer = src.layer;
			cpos = src.cpos;
			wpos = src.wpos;
		}

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(WVec vec) { return this; }
		public IRenderable AsDecoration() { return this; }

		public PaletteReference Palette { get { return null; } }
		public bool IsDecoration { get { return false; } }

		WPos IRenderable.Pos { get { return wpos; } }

		IFinalizedRenderable IRenderable.PrepareRender(WorldRenderer wr) { return this; }

		bool firstTime = true;
		float3[] screen;
		int alpha;
		public void Render(WorldRenderer wr)
		{
			if (firstTime)
			{
				var map = wr.World.Map;
				var tileSet = wr.World.Map.Rules.TileSet;
				var uv = cpos.ToMPos(map);

				if (!map.Height.Contains(uv))
					return;

				var tile = map.Tiles[uv];
				var ti = tileSet.GetTileInfo(tile);
				var ramp = ti != null ? ti.RampType : 0;

				var corners = map.Grid.CellCorners[ramp];
				screen = corners.Select(c => wr.Screen3DPxPosition(wpos + c + new WVec(0, 0, ZOffset))).ToArray();
				SetLevel(Level);
				firstTime = false;
			}

			if (Level == 0)
				return;

			Game.Renderer.WorldRgbaColorRenderer.FillRect(screen[0], screen[1], screen[2], screen[3], Color.FromArgb(alpha, layer.Info.Color));
		}

		public void SetLevel(int value)
		{
			Level = value;

			if (layer == null)
				return;

			// Saturate the visualization to MaxLevel
			int level = this.Level.Clamp(0, layer.Info.MaxLevel);

			// Linear interpolation
			alpha = layer.Info.Darkest + (layer.TintLevel * level) / 255;
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
		public void Tick(World world) { }
		IEnumerable<IRenderable> IEffect.Render(WorldRenderer r) { yield return this; }
	}
}
