#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Attach this to the world actor. Required for LaysTerrain to work.")]
	public class BuildableTerrainLayerInfo : ITraitInfo
	{
		[Desc("Palette to render the layer sprites in.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		public object Create(ActorInitializer init) { return new BuildableTerrainLayer(init.Self, this); }
	}

	public class BuildableTerrainLayer : IRenderOverlay, IWorldLoaded, ITickRender, INotifyActorDisposing
	{
		readonly BuildableTerrainLayerInfo info;
		readonly Dictionary<CPos, Sprite> dirty = new Dictionary<CPos, Sprite>();
		readonly Map map;

		TerrainSpriteLayer render;
		Theater theater;

		public BuildableTerrainLayer(Actor self, BuildableTerrainLayerInfo info)
		{
			this.info = info;
			map = self.World.Map;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			theater = wr.Theater;
			render = new TerrainSpriteLayer(w, wr, theater.Sheet, BlendMode.Alpha, wr.Palette(info.Palette), wr.World.Type != WorldType.Editor);
		}

		public void AddTile(CPos cell, TerrainTile tile)
		{
			map.CustomTerrain[cell] = map.Rules.TileSet.GetTerrainIndex(tile);

			// Terrain tiles define their origin at the topleft
			var s = theater.TileSprite(tile);
			dirty[cell] = new Sprite(s.Sheet, s.Bounds, s.ZRamp, float2.Zero, s.Channel, s.BlendMode);
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var kv in dirty)
			{
				if (!self.World.FogObscures(kv.Key))
				{
					render.Update(kv.Key, kv.Value);
					remove.Add(kv.Key);
				}
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		public void Render(WorldRenderer wr)
		{
			render.Draw(wr.Viewport);
		}

		bool disposed;
		public void Disposing(Actor self)
		{
			if (disposed)
				return;

			render.Dispose();
			disposed = true;
		}
	}
}
