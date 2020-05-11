#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Attach this to the world actor. Required for LaysTerrain to work.")]
	public class BuildableTerrainLayerInfo : TraitInfo
	{
		[Desc("Palette to render the layer sprites in.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[Desc("The hitpoints, which can be reduced by the DamagesConcreteWarhead.")]
		public readonly int MaxStrength = 9000;

		public override object Create(ActorInitializer init) { return new BuildableTerrainLayer(init.Self, this); }
	}

	public class BuildableTerrainLayer : IRenderOverlay, IWorldLoaded, ITickRender, INotifyActorDisposing
	{
		readonly BuildableTerrainLayerInfo info;
		readonly Dictionary<CPos, Sprite> dirty = new Dictionary<CPos, Sprite>();
		readonly Map map;
		readonly CellLayer<int> strength;

		BuildingInfluence bi;
		TerrainSpriteLayer render;
		Theater theater;
		bool disposed;

		public BuildableTerrainLayer(Actor self, BuildableTerrainLayerInfo info)
		{
			this.info = info;
			map = self.World.Map;
			strength = new CellLayer<int>(self.World.Map);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			theater = wr.Theater;
			bi = w.WorldActor.Trait<BuildingInfluence>();
			render = new TerrainSpriteLayer(w, wr, theater.Sheet, BlendMode.Alpha, wr.Palette(info.Palette), wr.World.Type != WorldType.Editor);
		}

		public void AddTile(CPos cell, TerrainTile tile)
		{
			if (!strength.Contains(cell))
				return;

			map.CustomTerrain[cell] = map.Rules.TileSet.GetTerrainIndex(tile);
			strength[cell] = info.MaxStrength;

			// Terrain tiles define their origin at the topleft
			var s = theater.TileSprite(tile);
			dirty[cell] = new Sprite(s.Sheet, s.Bounds, s.ZRamp, float2.Zero, s.Channel, s.BlendMode);
		}

		public void HitTile(CPos cell, int damage)
		{
			if (!strength.Contains(cell) || strength[cell] == 0 || bi.GetBuildingAt(cell) != null)
				return;

			strength[cell] = strength[cell] - damage;
			if (strength[cell] < 1)
				RemoveTile(cell);
		}

		public void RemoveTile(CPos cell)
		{
			if (!strength.Contains(cell))
				return;

			map.CustomTerrain[cell] = byte.MaxValue;
			strength[cell] = 0;
			dirty[cell] = null;
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
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

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			render.Draw(wr.Viewport);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			render.Dispose();
			disposed = true;
		}
	}
}
