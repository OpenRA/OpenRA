#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Attach this to the world actor. Renders the state of " + nameof(PavementLayer))]
	public class PavementRendererInfo : TraitInfo
	{
		[Desc("Palette to render the layer sprites in.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		public override object Create(ActorInitializer init) { return new PavementRenderer(init.Self, this); }
	}

	public class PavementRenderer : IRenderOverlay, IWorldLoaded, ITickRender, IRadarTerrainLayer, INotifyActorDisposing
	{
		readonly World world;
		readonly PavementRendererInfo info;
		readonly PavementLayer pavementLayer;
		readonly Dictionary<CPos, Sprite> dirty;
		readonly ITiledTerrainRenderer terrainRenderer;
		readonly CellLayer<(Color, Color)> radarColor;

		TerrainSpriteLayer terrainSpriteLayer;
		ITemplatedTerrainInfo templatedTerrainInfo;
		PaletteReference paletteReference;

		public PavementRenderer(Actor self, PavementRendererInfo info)
		{
			this.info = info;
			world = self.World;
			dirty = new Dictionary<CPos, Sprite>();
			terrainRenderer = self.Trait<ITiledTerrainRenderer>();
			pavementLayer = self.Trait<PavementLayer>();
			radarColor = new CellLayer<(Color, Color)>(world.Map);
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			paletteReference = wr.Palette(info.Palette);

			terrainSpriteLayer = new TerrainSpriteLayer(w, wr, terrainRenderer.MissingTile, BlendMode.Alpha, wr.World.Type != WorldType.Editor);

			if (w.Map.Rules.TerrainInfo is not ITemplatedTerrainInfo terrainInfo)
				throw new InvalidDataException(nameof(PavementRenderer) + " requires a template-based tileset.");

			templatedTerrainInfo = terrainInfo;
			borderIndices = BorderIndicesPerTilesetId[templatedTerrainInfo.Id];

			pavementLayer.Occupied.CellEntryChanged += Add;
		}

		IReadOnlyDictionary<LAT.Adjacency, ushort> borderIndices;

		// 596(TEMPERATE) / 1046(SNOW) are unused (pavement sides with a clear hole in the middle).
		static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<LAT.Adjacency, ushort>> BorderIndicesPerTilesetId = new Dictionary<string, IReadOnlyDictionary<LAT.Adjacency, ushort>>
		{
			{
				"TEMPERATE",
				new Dictionary<LAT.Adjacency, ushort>
				{
					{ LAT.Adjacency.None, 671 },
					{ LAT.Adjacency.MinusY, 597 },
					{ LAT.Adjacency.PlusX, 598 },
					{ LAT.Adjacency.MinusY | LAT.Adjacency.PlusX, 599 },
					{ LAT.Adjacency.PlusY, 600 },
					{ LAT.Adjacency.PlusY | LAT.Adjacency.MinusY, 601 },
					{ LAT.Adjacency.PlusY | LAT.Adjacency.PlusX, 602 },
					{ LAT.Adjacency.PlusY | LAT.Adjacency.MinusY | LAT.Adjacency.PlusX, 603 },
					{ LAT.Adjacency.MinusX, 604 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.MinusY, 605 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.PlusX, 606 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.MinusY | LAT.Adjacency.PlusX, 607 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.PlusY, 608 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.MinusY | LAT.Adjacency.PlusY, 609 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.PlusY | LAT.Adjacency.PlusX, 609 },
					{ LAT.Adjacency.PlusY | LAT.Adjacency.MinusX | LAT.Adjacency.MinusY | LAT.Adjacency.PlusX, 611 },
				}
			},
			{
				"SNOW",
				new Dictionary<LAT.Adjacency, ushort>
				{
					{ LAT.Adjacency.None, 1031 },
					{ LAT.Adjacency.MinusY, 1047 },
					{ LAT.Adjacency.PlusX, 1048 },
					{ LAT.Adjacency.MinusY | LAT.Adjacency.PlusX, 1049 },
					{ LAT.Adjacency.PlusY, 1050 },
					{ LAT.Adjacency.PlusY | LAT.Adjacency.MinusY, 1051 },
					{ LAT.Adjacency.PlusY | LAT.Adjacency.PlusX, 1052 },
					{ LAT.Adjacency.PlusY | LAT.Adjacency.MinusY | LAT.Adjacency.PlusX, 1053 },
					{ LAT.Adjacency.MinusX, 1054 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.MinusY, 1055 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.PlusX, 1056 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.MinusY | LAT.Adjacency.PlusX, 1057 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.PlusY, 1058 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.MinusY | LAT.Adjacency.PlusY, 1059 },
					{ LAT.Adjacency.MinusX | LAT.Adjacency.PlusY | LAT.Adjacency.PlusX, 1059 },
					{ LAT.Adjacency.PlusY | LAT.Adjacency.MinusX | LAT.Adjacency.MinusY | LAT.Adjacency.PlusX, 1061 },
				}
			},
		};

		LAT.Adjacency FindClearSides(CPos cell)
		{
			// Borders are only valid on flat cells
			if (world.Map.Ramp[cell] != 0)
				return LAT.Adjacency.None;

			var clearSides = LAT.Adjacency.None;
			if (!pavementLayer.Occupied[cell + new CVec(0, -1)])
				clearSides |= LAT.Adjacency.MinusY;

			if (!pavementLayer.Occupied[cell + new CVec(-1, 0)])
				clearSides |= LAT.Adjacency.MinusX;

			if (!pavementLayer.Occupied[cell + new CVec(1, 0)])
				clearSides |= LAT.Adjacency.PlusX;

			if (!pavementLayer.Occupied[cell + new CVec(0, 1)])
				clearSides |= LAT.Adjacency.PlusY;

			return clearSides;
		}

		void Add(CPos cell)
		{
			UpdateRenderedSprite(cell);
			foreach (var direction in CVec.Directions)
			{
				var neighbor = direction + cell;
				UpdateRenderedSprite(neighbor);
			}
		}

		void UpdateRenderedSprite(CPos cell)
		{
			if (!pavementLayer.Occupied[cell])
				return;

			var clearSides = FindClearSides(cell);
			borderIndices.TryGetValue(clearSides, out var tileTemplateId);

			var template = templatedTerrainInfo.Templates[tileTemplateId];
			var index = Game.CosmeticRandom.Next(template.TilesCount);
			var terrainTile = new TerrainTile(template.Id, (byte)index);

			var sprite = terrainRenderer.TileSprite(terrainTile);
			var offset = new float3(0, 0, -10);
			dirty[cell] = new Sprite(sprite.Sheet, sprite.Bounds, 1, sprite.Offset + offset, sprite.Channel, sprite.BlendMode);

			var tileInfo = world.Map.Rules.TerrainInfo.GetTerrainInfo(terrainTile);
			radarColor[cell] = (tileInfo.GetColor(world.LocalRandom), tileInfo.GetColor(world.LocalRandom));
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var kv in dirty)
			{
				var cell = kv.Key;
				if (!self.World.FogObscures(cell))
				{
					var sprite = kv.Value;
					terrainSpriteLayer.Update(cell, sprite, paletteReference);
					remove.Add(cell);
				}
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			terrainSpriteLayer.Draw(wr.Viewport);
		}

		event Action<CPos> IRadarTerrainLayer.CellEntryChanged
		{
			add => radarColor.CellEntryChanged += value;
			remove => radarColor.CellEntryChanged -= value;
		}

		bool IRadarTerrainLayer.TryGetTerrainColorPair(MPos uv, out (Color Left, Color Right) value)
		{
			value = default;

			if (world.Map.CustomTerrain[uv] == byte.MaxValue)
				return false;

			var cell = uv.ToCPos(world.Map);
			if (!pavementLayer.Occupied[cell])
				return false;

			value = radarColor[uv];
			return true;
		}

		bool disposed;
		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			pavementLayer.Occupied.CellEntryChanged -= Add;

			terrainSpriteLayer.Dispose();
			disposed = true;
		}
	}
}
