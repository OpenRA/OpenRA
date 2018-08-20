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
using OpenRA.Graphics;
using OpenRA.Mods.AS.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public enum FadeoutType
	{
		Linear,
		Logarithmic
	}

	[Desc("Has to be attached to world actor. ")]
	public class TintedCellsLayerInfo : ITraitInfo
	{
		[Desc("Color of cells")]
		public readonly Color Color = Color.FromArgb(0, 255, 0);

		[Desc("Maximum radiation allowable in a cell.The cell can actually have more radiation but it will only damage as if it had the maximum level.")]
		public readonly int MaxLevel = 500;

		[Desc("Delay in ticks between level decrements. The level updates this often, although the whole lifetime will be as defined by half-life.")]
		public readonly int UpdateDelay = 15;

		[Desc("The alpha value for displaying level for cells with level == 1")]
		public readonly int Darkest = 4;

		[Desc("The alpha value for displaying level for cells with level == MaxLevel")]
		public readonly int Brightest = 64;

		[Desc("Delay of half life, in ticks")]
		public readonly int FadeoutDelay = 150;

		[Desc("Z offset of the visualization.")]
		public readonly int ZOffset = 512;

		[Desc("Name of the layer.")]
		public readonly string Name = "radioactivity";

		[Desc("How shall level decay, can be Linear or Logarithmic.")]
		public readonly FadeoutType FadeoutType = FadeoutType.Logarithmic;

		public object Create(ActorInitializer init) { return new TintedCellsLayer(init.Self, this); }
	}

	public class TintedCellsLayer : INotifyActorDisposing, ITick, ITickRender
	{
		readonly World world;
		public readonly TintedCellsLayerInfo Info;

		// In the following, I think dictionary is better than array, as radioactivity has similar affecting area as smudges.

		// Tiles without considering fog of war.
		readonly Dictionary<CPos, TintedCell> tiles = new Dictionary<CPos, TintedCell>();

		// What's visible to the player.
		readonly Dictionary<CPos, TintedCell> renderedTiles = new Dictionary<CPos, TintedCell>();

		// Dirty, as in cache dirty bits.
		readonly HashSet<CPos> dirty = new HashSet<CPos>();

		// There's LERP function but the problem is, it is better to reuse these constants than computing
		// related constants (in LERP) every time.
		// half life constant, to be computed at init.
		public readonly int FalloutScale;
		public readonly int TintLevel;

		public TintedCellsLayer(Actor self, TintedCellsLayerInfo info)
		{
			world = self.World;
			Info = info;

			switch (info.FadeoutType)
			{
				case FadeoutType.Linear:
					FalloutScale = info.UpdateDelay * 500 / info.FadeoutDelay;
					break;
				case FadeoutType.Logarithmic:
					// (693 is 1000*ln(2) so we must divide by 1000 later on.)
					FalloutScale = info.UpdateDelay * 693 / info.FadeoutDelay;
					break;
			}

			TintLevel = 255 * (info.Brightest - info.Darkest) / (info.MaxLevel - 1);
		}

		void ITick.Tick(Actor self)
		{
			var remove = new List<CPos>();

			// Apply half life to each cell.
			foreach (var kv in tiles)
			{
				if (!Decay(kv.Value, Info.UpdateDelay))
					continue;

				if (kv.Value.Level <= 0)
					remove.Add(kv.Key);

				dirty.Add(kv.Key);
			}

			foreach (var r in remove)
				tiles.Remove(r);
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var c in dirty)
			{
				if (self.World.FogObscures(c))
					continue;

				if (renderedTiles.ContainsKey(c))
				{
					world.Remove(renderedTiles[c]);
					renderedTiles.Remove(c);
				}

				// synchronize observations with true value.
				if (tiles.ContainsKey(c))
				{
					renderedTiles[c] = new TintedCell(tiles[c]);
					world.Add(renderedTiles[c]);
				}

				remove.Add(c);
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		public int GetLevel(CPos cell)
		{
			if (!tiles.ContainsKey(cell))
				return 0;

			// The damage is constrained by MaxLevel
			var level = tiles[cell].Level;
			if (level > Info.MaxLevel)
				return Info.MaxLevel;
			else
				return level;
		}

		public void IncreaseLevel(CPos cell, int level, int max_level)
		{
			// Initialize, on fresh impact.
			if (!tiles.ContainsKey(cell))
				tiles[cell] = new TintedCell(this, cell, world.Map.CenterOfCell(cell));

			tiles[cell].Ticks = Info.UpdateDelay;
			var new_level = tiles[cell].Level + level;
			if (new_level > max_level)
				new_level = max_level;

			// the given weapon can't saturate the cell anymore.
			if (tiles[cell].Level > new_level)
				return;

			tiles[cell].SetLevel(new_level);

			dirty.Add(cell);
		}

		// Returns true when it decays.
		public bool Decay(TintedCell tc, int updateDelay)
		{
			tc.Ticks--;
			if (tc.Ticks > 0)
				return false;

			tc.Ticks = updateDelay;

			int dlevel = FalloutScale * tc.Level / 1000;

			// has to be decreased by at least 1 so that it disappears eventually.
			if (dlevel < 1)
				dlevel = 1;

			tc.SetLevel(tc.Level - dlevel);
			return true;
		}

		bool disposed = false;
		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			disposed = true;
		}
	}
}
