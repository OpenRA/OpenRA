#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	using CellContents = ResourceLayer.CellContents;

	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorResourceLayerInfo : ITraitInfo, Requires<ResourceTypeInfo>
	{
		public virtual object Create(ActorInitializer init) { return new EditorResourceLayer(init.Self); }
	}

	public class EditorResourceLayer : IWorldLoaded, IRenderOverlay
	{
		protected readonly Map Map;
		protected readonly TileSet Tileset;
		protected readonly Dictionary<int, ResourceType> Resources;
		protected readonly CellLayer<CellContents> Tiles;
		protected readonly HashSet<CPos> Dirty = new HashSet<CPos>();

		public EditorResourceLayer(Actor self)
		{
			if (self.World.Type != WorldType.Editor)
				return;

			Map = self.World.Map;
			Tileset = self.World.TileSet;

			Tiles = new CellLayer<CellContents>(Map);
			Resources = self.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.ResourceType, r => r);

			Map.MapResources.Value.CellEntryChanged += UpdateCell;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (w.Type != WorldType.Editor)
				return;

			foreach (var cell in Map.AllCells)
				UpdateCell(cell);
		}

		public void UpdateCell(CPos cell)
		{
			var uv = cell.ToMPos(Map);
			var tile = Map.MapResources.Value[uv];

			ResourceType type;
			if (Resources.TryGetValue(tile.Type, out type))
			{
				Tiles[uv] = new CellContents
				{
					Type = type,
					Variant = ChooseRandomVariant(type),
				};

				Map.CustomTerrain[uv] = Tileset.GetTerrainIndex(type.Info.TerrainType);
			}
			else
			{
				Tiles[uv] = CellContents.Empty;
				Map.CustomTerrain[uv] = byte.MaxValue;
			}

			// Ingame resource rendering is a giant hack (#6395),
			// so we must also touch all the neighbouring tiles
			Dirty.Add(cell);
			foreach (var d in CVec.Directions)
			{
				var c = cell + d;
				if (Map.Contains(c))
					Dirty.Add(c);
			}
		}

		protected virtual string ChooseRandomVariant(ResourceType t)
		{
			return t.Variants.Keys.Random(Game.CosmeticRandom);
		}

		public int ResourceDensityAt(CPos c)
		{
			// Set density based on the number of neighboring resources
			var adjacent = 0;
			var type = Tiles[c].Type;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
					if (Map.MapResources.Value[c + new CVec(u, v)].Type == type.Info.ResourceType)
						adjacent++;

			return Math.Max(int2.Lerp(0, type.Info.MaxDensity, adjacent, 9), 1);
		}

		public virtual CellContents UpdateDirtyTile(CPos c)
		{
			var t = Tiles[c];
			var type = t.Type;

			// Empty tile
			if (type == null)
			{
				t.Sprite = null;
				return t;
			}

			// Set density based on the number of neighboring resources
			t.Density = ResourceDensityAt(c);

			var sprites = type.Variants[t.Variant];
			var frame = int2.Lerp(0, sprites.Length - 1, t.Density - 1, type.Info.MaxDensity);
			t.Sprite = sprites[frame];

			return t;
		}

		public void Render(WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				return;

			foreach (var c in Dirty)
				Tiles[c] = UpdateDirtyTile(c);

			Dirty.Clear();

			foreach (var uv in wr.Viewport.VisibleCells.MapCoords)
			{
				var t = Tiles[uv];
				if (t.Sprite != null)
					new SpriteRenderable(t.Sprite, wr.World.Map.CenterOfCell(uv.ToCPos(Map)),
						WVec.Zero, -511, t.Type.Palette, 1f, true).Render(wr);
			}
		}
	}
}
