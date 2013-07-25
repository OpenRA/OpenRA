#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	class BibLayerInfo : ITraitInfo
	{
		public readonly string[] BibTypes = { "bib3", "bib2", "bib1" };
		public readonly int[] BibWidths = { 2, 3, 4 };
		public readonly bool FrozenUnderFog = false;
		public object Create(ActorInitializer init) { return new BibLayer(init.self, this); }
	}

	struct CachedBib
	{
		public Dictionary<CPos, TileReference<byte, byte>> Tiles;
		public IEnumerable<CPos> Footprint;
		public bool Visible;
	}

	class BibLayer : IRenderOverlay, IWorldLoaded, ITickRender
	{
		World world;
		BibLayerInfo info;
		Dictionary<Actor, CachedBib> visible;
		Dictionary<Actor, CachedBib> dirty;
		Sprite[][] bibSprites;

		public BibLayer(Actor self, BibLayerInfo info)
		{
			this.info = info;
			bibSprites = info.BibTypes.Select(x => Game.modData.SpriteLoader.LoadAllSprites(x)).ToArray();

			self.World.ActorAdded += a => DoBib(a, true);
			self.World.ActorRemoved += a => DoBib(a, false);
		}

		public void WorldLoaded(World w)
		{
			world = w;
			visible = new Dictionary<Actor, CachedBib>();
			dirty = new Dictionary<Actor, CachedBib>();
		}

		public void DoBib(Actor b, bool isAdd)
		{
			if (!b.HasTrait<Bib>())
				return;

			var buildingInfo = b.Info.Traits.Get<BuildingInfo>();
			var size = buildingInfo.Dimensions.X;
			var bibOffset = buildingInfo.Dimensions.Y - 1;

			var bib = Array.IndexOf(info.BibWidths, size);
			if (bib < 0)
			{
				Log.Write("debug", "Cannot bib {0}-wide building {1}", size, b.Info.Name);
				return;
			}

			dirty[b] = new CachedBib()
			{
				Footprint = FootprintUtils.Tiles(b),
				Tiles = new Dictionary<CPos, TileReference<byte, byte>>(),
				Visible = isAdd
			};

			for (var i = 0; i < 2 * size; i++)
			{
				var cell = b.Location + new CVec(i % size, i / size + bibOffset);
				var tile = new TileReference<byte, byte>((byte)(bib + 1), (byte) i);
				dirty[b].Tiles.Add(cell, tile);
			}
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<Actor>();
			foreach (var kv in dirty)
			{
				if (!info.FrozenUnderFog || kv.Value.Footprint.Any(c => !self.World.FogObscures(c)))
				{
					if (kv.Value.Visible)
						visible[kv.Key] = kv.Value;
					else
						visible.Remove(kv.Key);

					remove.Add(kv.Key);
				}
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		public void Render(WorldRenderer wr)
		{
			var pal = wr.Palette("terrain");
			var cliprect = Game.viewport.WorldBounds(world);

			foreach (var bib in visible.Values)
			{
				foreach (var kv in bib.Tiles)
				{
					if (!cliprect.Contains(kv.Key.X, kv.Key.Y))
						continue;
					if (world.ShroudObscures(kv.Key))
						continue;

					var tile = bibSprites[kv.Value.type - 1][kv.Value.index];
					tile.DrawAt(wr.ScreenPxPosition(kv.Key.CenterPosition) - 0.5f * tile.size, pal);
				}
			}
		}
	}

	public class BibInfo : TraitInfo<Bib>, Requires<BuildingInfo> { }
	public class Bib { }
}
