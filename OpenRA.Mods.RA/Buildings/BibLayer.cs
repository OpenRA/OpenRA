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
		public object Create(ActorInitializer init) { return new BibLayer(init.self); }
	}

	struct CachedBib
	{
		public Dictionary<CPos, Sprite> Tiles;
		public IEnumerable<CPos> Footprint;
		public bool Visible;
		public bool Immediate;
	}

	class BibLayer : IRenderOverlay, ITickRender
	{
		World world;
		Dictionary<Actor, CachedBib> visible;
		Dictionary<Actor, CachedBib> dirty;
		Cache<string, Sprite[]> sprites;

		public BibLayer(Actor self)
		{
			world = self.World;
			visible = new Dictionary<Actor, CachedBib>();
			dirty = new Dictionary<Actor, CachedBib>();
			sprites = new Cache<string, Sprite[]>(x => Game.modData.SpriteLoader.LoadAllSprites(x));
		}

		public void Update(Actor a, CachedBib bib)
		{
			dirty[a] = bib;
		}

		public Sprite[] LoadSprites(string bibType)
		{
			return sprites[bibType];
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<Actor>();
			foreach (var kv in dirty)
			{
				if (kv.Value.Immediate || kv.Value.Footprint.Any(c => !self.World.FogObscures(c)))
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

					kv.Value.DrawAt(wr.ScreenPxPosition(kv.Key.CenterPosition) - 0.5f * kv.Value.size, pal);
				}
			}
		}
	}

	public class BibInfo : ITraitInfo, Requires<BuildingInfo>
	{
		public readonly string Sprite = "bib3";

		public object Create(ActorInitializer init) { return new Bib(init.self, this); }
	}

	public class Bib : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly BibInfo info;
		readonly BibLayer bibLayer;
		bool firstAdd;

		public Bib(Actor self, BibInfo info)
		{
			this.info = info;
			bibLayer = self.World.WorldActor.Trait<BibLayer>();
			firstAdd = true;
		}

		void DoBib(Actor self, bool add)
		{
			var buildingInfo = self.Info.Traits.Get<BuildingInfo>();
			var size = buildingInfo.Dimensions.X;
			var bibOffset = buildingInfo.Dimensions.Y - 1;
			var sprites = bibLayer.LoadSprites(info.Sprite);

			if (sprites.Length != 2*size)
				throw new InvalidOperationException("{0} is an invalid bib for a {1}-wide building".F(info.Sprite, size));

			var immediate = !self.HasTrait<FrozenUnderFog>() ||
				(firstAdd && self.Info.Traits.GetOrDefault<FrozenUnderFogInfo>().StartsRevealed);

			var dirty = new CachedBib()
			{
				Footprint = FootprintUtils.Tiles(self),
				Tiles = new Dictionary<CPos, Sprite>(),
				Visible = add,
				Immediate = immediate
			};

			for (var i = 0; i < 2 * size; i++)
			{
				var cell = self.Location + new CVec(i % size, i / size + bibOffset);
				dirty.Tiles.Add(cell, sprites[i]);
			}

			firstAdd = false;
			bibLayer.Update(self, dirty);
		}

		public void AddedToWorld(Actor self) { DoBib(self, true); }
		public void RemovedFromWorld(Actor self) { DoBib(self, false); }
	}
}
