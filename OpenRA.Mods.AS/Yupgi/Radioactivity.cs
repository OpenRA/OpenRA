#region Copyright & License Information
/*
 * Radioactivity class by Boolbada of OP Mod
 * This one I made from scratch xD
 * As an OpenRA module, this module follows GPLv3 license:
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Yupgi_alert.Traits;

/*
Works without base engine modification
*/

namespace OpenRA.Mods.Yupgi_alert.Graphics
{
	class Radioactivity : IRenderable, IFinalizedRenderable, IEffect
	{
		public int Ticks = 0;
		public int Level = 0;
		readonly RadioactivityLayer layer;
		readonly WPos wpos;

		public int ZOffset
		{
			get
			{
				return layer.Info.ZOffset;
			}
		}

		public Radioactivity(RadioactivityLayer layer, WPos wpos)
		{
			this.wpos = wpos;
			this.layer = layer;
		}

		public Radioactivity(Radioactivity src)
		{
			Ticks = src.Ticks;
			Level = src.Level;
			layer = src.layer;
			wpos = src.wpos;
		}

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(WVec vec) { return this; }
		public IRenderable AsDecoration() { return this; }

		public PaletteReference Palette { get { return null; } }
		public bool IsDecoration { get { return false; } }

		public WPos Pos { get { return wpos; } }

		IFinalizedRenderable IRenderable.PrepareRender(WorldRenderer wr)
		{
			return this;
		}

		public void Render(WorldRenderer wr)
		{
			var map = wr.World.Map;
			var tileSet = wr.World.Map.Rules.TileSet;
			var uv = map.CellContaining(wpos).ToMPos(map);

			if (!map.Height.Contains(uv))
				return;

			var height = (int)map.Height[uv];
			var tile = map.Tiles[uv];
			var ti = tileSet.GetTileInfo(tile);
			var ramp = ti != null ? ti.RampType : 0;

			var corners = map.Grid.CellCorners[ramp];
			var pos = map.CenterOfCell(uv.ToCPos(map));
			var screen = corners.Select(c => wr.Screen3DPxPosition(pos + c)).ToArray();

			int level = this.Level.Clamp(0, layer.Info.MaxLevel); // Saturate the visualization to MaxLevel
			if (level == 0)
				return; // don't visualize 0 cells. They show up before cells get removed.

			int alpha = (layer.YIntercept100 + layer.Slope100 * level) / 100; // Linear interpolation
			alpha = alpha.Clamp(0, 255); // Just to be safe.

			/*
			for (var i = 0; i < 4; i++)
			{
				var j = (i + 1) % 4;
				Game.Renderer.WorldRgbaColorRenderer.DrawLine(screen[i], screen[j], 3,
					Color.FromArgb(alpha, layer.Info.Color),
					Color.FromArgb(alpha, layer.Info.Color));
			}
			*/

			Game.Renderer.WorldRgbaColorRenderer.FillTriangle(screen[0], screen[1], screen[2], Color.FromArgb(alpha, layer.Info.Color));
			Game.Renderer.WorldRgbaColorRenderer.FillTriangle(screen[2], screen[3], screen[0], Color.FromArgb(alpha, layer.Info.Color));

			// mix in yellow so that the radion shines brightly, after certain threshold.
			// It is different than tinting the info.color itself and provides nicer look.
			if (alpha > layer.Info.MixThreshold)
			{
				Game.Renderer.WorldRgbaColorRenderer.FillTriangle(screen[0], screen[1], screen[2], Color.FromArgb(16, layer.Info.Color2));
				Game.Renderer.WorldRgbaColorRenderer.FillTriangle(screen[2], screen[3], screen[0], Color.FromArgb(16, layer.Info.Color2));
			}

			/* Let's actually see the level numbers
			var text = "{0}".F(Level);
			var font = Game.Renderer.Fonts["Bold"]; // OPMod: Was TinyBold
			var screenPos = wr.Viewport.Zoom * (wr.ScreenPosition(pos) - wr.Viewport.TopLeft.ToFloat2()) - 0.5f * font.Measure(text).ToFloat2();
			font.DrawTextWithContrast(text, screenPos, color, Color.Black, Color.Black, 1);
			*/
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }

		public void Tick(World world)
		{
		}

		// Returns true when "dirty" (RA value changed)
		public bool Decay(int updateDelay)
		{
			Ticks--; // count half-life.
			if (Ticks > 0)
				return false;

			/* on each half life...
			 * ra.ticks = info.Halflife; // reset ticks
			 * ra.level /= 2; // simple is best haha...
			 * Looks unnatural and induces "flickers"
			 */

			Ticks = updateDelay; // reset ticks
			int dlevel = layer.K1000 * Level / 1000;
			if (dlevel < 1)
				dlevel = 1; // must decrease by at least 1 so that the contamination disappears eventually.
			Level -= dlevel;

			return true;
		}

		public void IncreaseLevel(int updateDelay, int level, int max_level)
		{
			Ticks = updateDelay;

			/// The code below may look odd but consider that each weapon may have a different max_level.

			var new_level = Level + level;
			if (new_level > max_level)
				new_level = max_level;

			if (Level > new_level)
				return; // the given weapon can't make the cell more radio active. (saturate)

			Level = new_level;
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer r)
		{
			yield return this;
		}
	}
}
