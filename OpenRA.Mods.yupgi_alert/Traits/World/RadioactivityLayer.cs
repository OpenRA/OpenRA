#region Copyright & License Information
/*
 * Radioactivity layer by Boolbada of OP Mod.
 * Started off from Resource layer by OpenRA devs but required intensive rewrite...
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Diagnostics; // assert thingy for temporary debugging
using OpenRA.Graphics;
using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.yupgi_alert.Traits
{
	[Desc("Attach this to the world actor. Radioactivity layer, as in RA2 desolator radioactivity. Order of the layers defines the Z sorting.")]
	// You can attach this layer by editing rules/world.yaml
	// I (boolbada) made this layer by cloning resources layer, as resource amount is quite similar to
	// radio activity. I looked at SmudgeLayer too.

	public class RadioactivityLayerInfo : ITraitInfo
	{
		[Desc("Color of radio activity")]
		public readonly Color Color = Color.FromArgb(0, 255, 0); // tint factor (was in RA2) sucks. Modify tint here statically.
		[Desc("Second color of radio activity, used in conjunction with MixThreshold.")]
		public readonly Color Color2 = Color.Yellow;

		[Desc("Maximum radiation allowable in a cell.The cell can actually have more radiation but it will only damage as if it had the maximum level.")]
		public readonly int MaxLevel = 500;

		[Desc("Delay in ticks between radiation level decrements. The level updates this often, although the whole lifetime will be as defined by half-life.")]
		public readonly int UpdateDelay = 15;

		[Desc("The alpha value for displaying radioactivity level for cells with level == 1")]
		public readonly int Darkest = 4;
		[Desc("The alpha value for displaying radioactivity level for cells with level == MaxLevel")]
		public readonly int Brightest = 64; // level == MaxLevel will get this as alpha
		[Desc("Color mix threshold. If alpha level goes beyond this threshold, Color2 will be mixed in.")]
		public readonly int MixThreshold = 36; 

		[Desc("Delay of half life, in ticks")]
		public readonly int Halflife = 150; // in ticks.

		// Damage dealing is handled by "DamagedByRadioactivity" trait attached at each actor.
		public object Create(ActorInitializer init) { return new RadioactivityLayer(init.Self, this); }
	}

	class Radioactivity
	{
		public int ticks = 0;
		public int level = 0;

		public Radioactivity Clone()
		{
			Radioactivity result = new Radioactivity();
			result.ticks = ticks;
			result.level = level;
			return result;
		}
	}

	public class RadioactivityLayer : IRenderOverlay, IWorldLoaded, ITickRender, INotifyActorDisposing, ITick
	{
		readonly World world;
		readonly RadioactivityLayerInfo info;

		// In the following, I think dictionary is better than array, as radioactivity has similar affecting area as smudges.

		// true radioactivity values, without considering fog of war.
		readonly Dictionary<CPos, Radioactivity> tiles = new Dictionary<CPos, Radioactivity>();

		// what's visible to the player.
		readonly Dictionary<CPos, Radioactivity> rendered_tiles = new Dictionary<CPos, Radioactivity>();

		readonly HashSet<CPos> dirty = new HashSet<CPos>(); // dirty, as in cache dirty bits.

		readonly int k1000; // half life constant, to be computed at init.
		public int slope100;
		public int y_intercept100;

		public RadioactivityLayer(Actor self, RadioactivityLayerInfo info)
		{
			world = self.World;
			this.info = info;
			//k = info.UpdateDelay * ((float) Math.Log(2)) / info.Halflife;
			k1000 = info.UpdateDelay * 693 / info.Halflife; // (693 is 1000*ln(2) so we must divide by 1000 later on.)
			//Debug.Assert(k > 0);
			// half life decay follows differential equation d/dt m(t) = -k m(t).
			// d/dt will be in ticks, ofcourse.

			// rad level visualization constants...
			slope100 = 100 * (info.Brightest - info.Darkest) / (info.MaxLevel - 1);
			y_intercept100 = 100*info.Brightest - (info.MaxLevel * slope100);
		}

		public void Render(WorldRenderer wr)
		{
			//foreach (var kv in spriteLayers.Values)
			//kv.Draw(wr.Viewport);
			foreach (var tile in rendered_tiles)
			{
				var ra = tile.Value;
				var center = wr.World.Map.CenterOfCell(tile.Key);
				var tl = wr.Screen3DPosition(center - new WVec(512, 512, 0)); // cos 512 is half a cell.
				var br = wr.Screen3DPosition(center + new WVec(512, 512, 0));

				int level = ra.level > info.MaxLevel ? info.MaxLevel : ra.level;
				if (level == 0)
					continue; // don't visualize 0 cells. They might show up before cells getting removed.

				int alpha = (y_intercept100 + slope100 * level)/100; // Linear interpolation
				alpha = alpha > 255 ? 255 : alpha; // just to be safe.

				Color color = Color.FromArgb(alpha, info.Color);
				Game.Renderer.WorldRgbaColorRenderer.FillRect(tl, br, color);

				// mix in yellow so that the radion shines brightly, after certain threshold.
				// It is different than tinting the info.color itself and provides nicer look.
				if (alpha > info.MixThreshold)
					Game.Renderer.WorldRgbaColorRenderer.FillRect(tl, br, Color.FromArgb(16, info.Color2));
			}
		}

		public void Tick(Actor self)
		{
			var remove = new List<CPos>();

			// Apply half life to each cell.
			foreach (var kv in tiles)
			{
				var ra = kv.Value;
				ra.ticks--; // count half-life.
				if (ra.ticks > 0)
					continue;

				// on each half life...
				//ra.ticks = info.Halflife; // reset ticks
				//ra.level /= 2; // simple is best haha...
				// Looks unnatural and induces "flickers"

				ra.ticks = info.UpdateDelay; // reset ticks
				int dlevel = k1000 * ra.level / 1000;
				if (dlevel < 1)
					dlevel = 1; // must decrease by at least 1 so that the contamination disappears eventually.
				ra.level -= dlevel;

				if (ra.level <= 0)
				{
					// Not radioactive anymore. Remove from this.tiles.
					remove.Add(kv.Key);
				}

				dirty.Add(kv.Key);
			}

			// Lets actually remove the entry.
			foreach (var r in remove)
				tiles.Remove(r);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			// hmm dunno what to do, at the moment.
		}

		// tick render, regardless of pause state.
		public void TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var c in dirty)
			{
				if (!self.World.FogObscures(c))
				{
					// synchronize observations with true value.
					if (tiles.ContainsKey(c))
						rendered_tiles[c] = tiles[c].Clone();
					else
						rendered_tiles.Remove(c);
					remove.Add(c);
				}
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		// Gets level, for damage calculation!!
		// That is, the level is constrained by MaxLevel!!!!
		public int GetLevel(CPos cell)
		{
			if (!tiles.ContainsKey(cell))
				return 0;

			var level = tiles[cell].level;
			if (level > info.MaxLevel)
				return info.MaxLevel;
			else
				return level;
		}

		public void IncreaseLevel(CPos cell, int level, int max_level)
		{
			// Initialize, on fresh impact.
			if (!tiles.ContainsKey(cell))
				tiles[cell] = new Radioactivity();

			var ra = tiles[cell];
			var new_level = level + ra.level;

			if (new_level > max_level)
				new_level = max_level;

			if (ra.level > new_level)
				return; // the given weapon can't make the cell more radio active. (saturate)

			// apply new level.
			ra.level = new_level;

			ra.ticks = info.UpdateDelay;
			//ra.ticks = info.Halflife;

			dirty.Add(cell);
		}

		public void Destroy(CPos cell)
		{
			// Clear cell
			//Content[cell] = EmptyCell;
			//world.Map.CustomTerrain[cell] = byte.MaxValue;

			//dirty[cell] = 100;
		}
		
		bool disposed = false;
		public void Disposing(Actor self)
		{
			if (disposed)
				return;

			// dispose all stuff

			disposed = true;
		}
	}
}
