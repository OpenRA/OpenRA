#region Copyright & License Information
/*
 * Radioactivity class by Boolbada of OP Mod
 * This one I made from scratch xD
 * As an OpenRA module, this module follows GPLv3 license:
 * 
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
		readonly WPos pos;

		public int ZOffset
		{
			get
			{
				return layer.Info.ZOffset;
			}
		}

		public Radioactivity(RadioactivityLayer layer, WPos pos)
		{
			this.pos = pos;
			this.layer = layer;
		}

		public Radioactivity(Radioactivity src)
		{
			Ticks = src.Ticks;
			Level = src.Level;
			layer = src.layer;
			pos = src.pos;
		}

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(WVec vec) { return this; }
		public IRenderable AsDecoration() { return this; }

		public PaletteReference Palette { get { return null; } }
		public bool IsDecoration { get { return false; } }

		public WPos Pos { get { return pos; } }

		IFinalizedRenderable IRenderable.PrepareRender(WorldRenderer wr)
		{
			return this;
		}

		public void Render(WorldRenderer wr)
		{
			var tl = wr.Screen3DPosition(pos - new WVec(512, 512, 0)); // cos 512 is half a cell.
			var br = wr.Screen3DPosition(pos + new WVec(512, 512, 0));

			int level = this.Level > layer.Info.MaxLevel ? layer.Info.MaxLevel : this.Level;
			if (level == 0)
				return; // don't visualize 0 cells. They show up before cells get removed.

			int alpha = (layer.YIntercept100 + layer.Slope100 * level) / 100; // Linear interpolation
			alpha = alpha > 255 ? 255 : alpha; // just to be safe.

			Color color = Color.FromArgb(alpha, layer.Info.Color);
			Game.Renderer.WorldRgbaColorRenderer.FillRect(tl, br, color);

			// mix in yellow so that the radion shines brightly, after certain threshold.
			// It is different than tinting the info.color itself and provides nicer look.
			if (alpha > layer.Info.MixThreshold)
				Game.Renderer.WorldRgbaColorRenderer.FillRect(tl, br, Color.FromArgb(16, layer.Info.Color2));
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

			var new_level = this.Level + level;

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
