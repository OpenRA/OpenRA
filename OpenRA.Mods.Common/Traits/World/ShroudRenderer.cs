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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ShroudRendererInfo : ITraitInfo
	{
		public readonly string Sequence = "shroud";
		public readonly string[] ShroudVariants = new[] { "shroud" };
		public readonly string[] FogVariants = new[] { "fog" };

		public readonly string ShroudPalette = "shroud";
		public readonly string FogPalette = "fog";

		[Desc("Bitfield of shroud directions for each frame. Lower four bits are",
			"corners clockwise from TL; upper four are edges clockwise from top")]
		public readonly int[] Index = new[] { 12, 9, 8, 3, 1, 6, 4, 2, 13, 11, 7, 14 };

		[Desc("Use the upper four bits when calculating frame")]
		public readonly bool UseExtendedIndex = false;

		[Desc("Override for source art that doesn't define a fully shrouded tile")]
		public readonly string OverrideFullShroud = null;
		public readonly int OverrideShroudIndex = 15;

		[Desc("Override for source art that doesn't define a fully fogged tile")]
		public readonly string OverrideFullFog = null;
		public readonly int OverrideFogIndex = 15;

		public readonly BlendMode ShroudBlend = BlendMode.Alpha;
		public object Create(ActorInitializer init) { return new ShroudRenderer(init.World, this); }
	}

	public class ShroudRenderer : IRenderShroud, IWorldLoaded
	{
		[Flags]
		enum Edges : byte
		{
			None = 0,
			TopLeft = 0x01,
			TopRight = 0x02,
			BottomRight = 0x04,
			BottomLeft = 0x08,
			AllCorners = TopLeft | TopRight | BottomRight | BottomLeft,
			TopSide = 0x10,
			RightSide = 0x20,
			BottomSide = 0x40,
			LeftSide = 0x80,
			AllSides = TopSide | RightSide | BottomSide | LeftSide,
			Top = TopSide | TopLeft | TopRight,
			Right = RightSide | TopRight | BottomRight,
			Bottom = BottomSide | BottomRight | BottomLeft,
			Left = LeftSide | TopLeft | BottomLeft,
			All = Top | Right | Bottom | Left
		}

		struct TileInfo
		{
			public readonly float2 ScreenPosition;
			public readonly byte Variant;

			public TileInfo(float2 screenPosition, byte variant)
			{
				ScreenPosition = screenPosition;
				Variant = variant;
			}
		}

		readonly ShroudRendererInfo info;
		readonly Map map;
		readonly Edges notVisibleEdges;
		readonly byte variantStride;
		readonly byte[] edgesToSpriteIndexOffset;

		readonly CellLayer<TileInfo> tileInfos;
		readonly CellLayer<bool> shroudDirty;

		readonly Vertex[] fogVertices, shroudVertices;
		readonly Sprite[] fogSprites, shroudSprites;
		readonly CellLayer<Sprite> fogSpriteLayer, shroudSpriteLayer;
		PaletteReference fogPalette, shroudPalette;

		Shroud currentShroud;
		bool mapBorderShroudIsCached;

		public ShroudRenderer(World world, ShroudRendererInfo info)
		{
			if (info.ShroudVariants.Length != info.FogVariants.Length)
				throw new ArgumentException("ShroudRenderer must define the same number of shroud and fog variants!", "info");

			if ((info.OverrideFullFog == null) ^ (info.OverrideFullShroud == null))
				throw new ArgumentException("ShroudRenderer cannot define overrides for only one of shroud or fog!", "info");

			if (info.ShroudVariants.Length > byte.MaxValue)
				throw new ArgumentException("ShroudRenderer cannot define this many shroud and fog variants.", "info");

			if (info.Index.Length >= byte.MaxValue)
				throw new ArgumentException("ShroudRenderer cannot define this many indexes for shroud directions.", "info");

			this.info = info;
			map = world.Map;

			tileInfos = new CellLayer<TileInfo>(map);
			shroudDirty = new CellLayer<bool>(map);
			var verticesLength = map.MapSize.X * map.MapSize.Y * 4;
			fogVertices = new Vertex[verticesLength];
			shroudVertices = new Vertex[verticesLength];
			fogSpriteLayer = new CellLayer<Sprite>(map);
			shroudSpriteLayer = new CellLayer<Sprite>(map);

			// Load sprite variants
			var variantCount = info.ShroudVariants.Length;
			variantStride = (byte)(info.Index.Length + (info.OverrideFullShroud != null ? 1 : 0));
			shroudSprites = new Sprite[variantCount * variantStride];
			fogSprites = new Sprite[variantCount * variantStride];

			for (var j = 0; j < variantCount; j++)
			{
				var shroud = map.SequenceProvider.GetSequence(info.Sequence, info.ShroudVariants[j]);
				var fog = map.SequenceProvider.GetSequence(info.Sequence, info.FogVariants[j]);
				for (var i = 0; i < info.Index.Length; i++)
				{
					shroudSprites[j * variantStride + i] = shroud.GetSprite(i);
					fogSprites[j * variantStride + i] = fog.GetSprite(i);
				}

				if (info.OverrideFullShroud != null)
				{
					var i = (j + 1) * variantStride - 1;
					shroudSprites[i] = map.SequenceProvider.GetSequence(info.Sequence, info.OverrideFullShroud).GetSprite(0);
					fogSprites[i] = map.SequenceProvider.GetSequence(info.Sequence, info.OverrideFullFog).GetSprite(0);
				}
			}

			// Mapping of shrouded directions -> sprite index
			edgesToSpriteIndexOffset = new byte[(byte)(info.UseExtendedIndex ? Edges.All : Edges.AllCorners) + 1];
			for (var i = 0; i < info.Index.Length; i++)
				edgesToSpriteIndexOffset[info.Index[i]] = (byte)i;

			if (info.OverrideFullShroud != null)
				edgesToSpriteIndexOffset[info.OverrideShroudIndex] = (byte)(variantStride - 1);

			notVisibleEdges = info.UseExtendedIndex ? Edges.AllSides : Edges.AllCorners;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			// Initialize tile cache
			// Adds a 1-cell border around the border to cover any sprites peeking outside the map
			foreach (var uv in CellRegion.Expand(w.Map.Cells, 1).MapCoords)
			{
				var screen = wr.ScreenPosition(w.Map.CenterOfCell(uv.ToCPos(map)));
				var variant = (byte)Game.CosmeticRandom.Next(info.ShroudVariants.Length);
				tileInfos[uv] = new TileInfo(screen, variant);
			}

			fogPalette = wr.Palette(info.FogPalette);
			shroudPalette = wr.Palette(info.ShroudPalette);
		}

		Edges GetEdges(MPos uv, Func<MPos, bool> isVisible)
		{
			if (!isVisible(uv))
				return notVisibleEdges;

			var cell = uv.ToCPos(map);

			// If a side is shrouded then we also count the corners.
			var edge = Edges.None;
			if (!isVisible((cell + new CVec(0, -1)).ToMPos(map))) edge |= Edges.Top;
			if (!isVisible((cell + new CVec(1, 0)).ToMPos(map))) edge |= Edges.Right;
			if (!isVisible((cell + new CVec(0, 1)).ToMPos(map))) edge |= Edges.Bottom;
			if (!isVisible((cell + new CVec(-1, 0)).ToMPos(map))) edge |= Edges.Left;

			var ucorner = edge & Edges.AllCorners;
			if (!isVisible((cell + new CVec(-1, -1)).ToMPos(map))) edge |= Edges.TopLeft;
			if (!isVisible((cell + new CVec(1, -1)).ToMPos(map))) edge |= Edges.TopRight;
			if (!isVisible((cell + new CVec(1, 1)).ToMPos(map))) edge |= Edges.BottomRight;
			if (!isVisible((cell + new CVec(-1, 1)).ToMPos(map))) edge |= Edges.BottomLeft;

			// RA provides a set of frames for tiles with shrouded
			// corners but unshrouded edges. We want to detect this
			// situation without breaking the edge -> corner enabling
			// in other combinations. The XOR turns off the corner
			// bits that are enabled twice, which gives the behavior
			// we want here.
			return info.UseExtendedIndex ? edge ^ ucorner : edge & Edges.AllCorners;
		}

		public void RenderShroud(WorldRenderer wr, Shroud shroud)
		{
			Update(shroud);
			Render(wr.Viewport.VisibleCells);
		}

		void Update(Shroud newShroud)
		{
			if (currentShroud != newShroud)
			{
				if (currentShroud != null)
					currentShroud.CellEntryChanged -= MarkCellAndNeighborsDirty;

				if (newShroud != null)
				{
					shroudDirty.Clear(true);
					newShroud.CellEntryChanged += MarkCellAndNeighborsDirty;
				}

				currentShroud = newShroud;
			}

			if (currentShroud != null)
			{
				mapBorderShroudIsCached = false;
			}
			else if (!mapBorderShroudIsCached)
			{
				mapBorderShroudIsCached = true;
				CacheMapBorderShroud();
			}
		}

		void MarkCellAndNeighborsDirty(CPos cell)
		{
			// Mark this cell and its 8 neighbors as being out of date.
			// We don't want to do anything more than this for several performance reasons:
			// - If the cells remain off-screen for a long time, they may change several times before we next view
			// them, so calculating their new vertices is wasted effort since we may recalculate them again before we
			// even get a chance to render them.
			// - Cells tend to be invalidated in groups (imagine as a unit moves, it advances a wave of sight and
			// leaves a trail of fog filling in behind). If we recalculated a cell and its neighbors when the first
			// cell in a group changed, many cells would be recalculated again when the second cell, right next to the
			// first, is updated. In fact we might do on the order of 3x the work we needed to!
			shroudDirty[cell + new CVec(-1, -1)] = true;
			shroudDirty[cell + new CVec(0, -1)] = true;
			shroudDirty[cell + new CVec(1, -1)] = true;
			shroudDirty[cell + new CVec(-1, 0)] = true;
			shroudDirty[cell] = true;
			shroudDirty[cell + new CVec(1, 0)] = true;
			shroudDirty[cell + new CVec(-1, 1)] = true;
			shroudDirty[cell + new CVec(0, 1)] = true;
			shroudDirty[cell + new CVec(1, 1)] = true;
		}

		void CacheMapBorderShroud()
		{
			// Cache the whole of the map border shroud ahead of time, since it never changes.
			Func<MPos, bool> mapContains = map.Contains;
			foreach (var uv in CellRegion.Expand(map.Cells, 1).MapCoords)
			{
				var offset = VertexArrayOffset(uv);
				var edges = GetEdges(uv, mapContains);
				var tileInfo = tileInfos[uv];
				CacheTile(uv, offset, edges, tileInfo, shroudSprites, shroudVertices, shroudPalette, shroudSpriteLayer);
				CacheTile(uv, offset, edges, tileInfo, fogSprites, fogVertices, fogPalette, fogSpriteLayer);
			}
		}

		void Render(CellRegion visibleRegion)
		{
			// Due to diamond tile staggering, we need to expand the cordon to get full shroud coverage.
			if (map.TileShape == TileShape.Diamond)
				visibleRegion = CellRegion.Expand(visibleRegion, 1);

			if (currentShroud == null)
				RenderMapBorderShroud(visibleRegion);
			else
				RenderPlayerShroud(visibleRegion);
		}

		void RenderMapBorderShroud(CellRegion visibleRegion)
		{
			// The map border shroud only affects the map border. If none of the visible cells are on the border, then
			// we don't need to render anything and can bail early for performance.
			if (CellRegion.Expand(map.Cells, -1).Contains(visibleRegion))
				return;

			// Render the shroud that just encroaches at the map border. This shroud is always fully cached, so we can
			// just render straight from the cache.
			foreach (var uv in visibleRegion.MapCoords)
			{
				var offset = VertexArrayOffset(uv);
				RenderCachedTile(shroudSpriteLayer[uv], shroudVertices, offset);
				RenderCachedTile(fogSpriteLayer[uv], fogVertices, offset);
			}
		}

		void RenderPlayerShroud(CellRegion visibleRegion)
		{
			// Render the shroud by drawing the appropriate tile over each cell that is visible on-screen.
			// For performance we keep a cache tiles we have drawn previously so we don't have to recalculate the
			// vertices for tiles every frame, since this is costly.
			// Any shroud marked as dirty has either never been calculated, or has changed since we last drew that
			// tile. We will calculate the vertices for that tile and cache them before drawing it.
			// Any shroud that is not marked as dirty means our cached tile is still correct - we can just draw the
			// cached vertices.
			var visibleUnderShroud = currentShroud.IsExploredTest(visibleRegion);
			var visibleUnderFog = currentShroud.IsVisibleTest(visibleRegion);
			foreach (var uv in visibleRegion.MapCoords)
			{
				var offset = VertexArrayOffset(uv);
				if (shroudDirty[uv])
				{
					shroudDirty[uv] = false;
					RenderDirtyTile(uv, offset, visibleUnderShroud, shroudSprites, shroudVertices, shroudPalette, shroudSpriteLayer);
					RenderDirtyTile(uv, offset, visibleUnderFog, fogSprites, fogVertices, fogPalette, fogSpriteLayer);
				}
				else
				{
					RenderCachedTile(shroudSpriteLayer[uv], shroudVertices, offset);
					RenderCachedTile(fogSpriteLayer[uv], fogVertices, offset);
				}
			}
		}

		int VertexArrayOffset(MPos uv)
		{
			return 4 * (uv.V * map.MapSize.X + uv.U);
		}

		void RenderDirtyTile(MPos uv, int offset, Func<MPos, bool> isVisible,
			Sprite[] sprites, Vertex[] vertices, PaletteReference palette, CellLayer<Sprite> spriteLayer)
		{
			var tile = tileInfos[uv];
			var edges = GetEdges(uv, isVisible);
			var sprite = CacheTile(uv, offset, edges, tile, sprites, vertices, palette, spriteLayer);
			RenderCachedTile(sprite, vertices, offset);
		}

		void RenderCachedTile(Sprite sprite, Vertex[] vertices, int offset)
		{
			if (sprite != null)
				Game.Renderer.WorldSpriteRenderer.DrawSprite(sprite, vertices, offset);
		}

		Sprite CacheTile(MPos uv, int offset, Edges edges, TileInfo tileInfo,
			Sprite[] sprites, Vertex[] vertices, PaletteReference palette, CellLayer<Sprite> spriteLayer)
		{
			var sprite = GetSprite(sprites, edges, tileInfo.Variant);
			if (sprite != null)
			{
				var size = sprite.Size;
				var location = tileInfo.ScreenPosition - 0.5f * size;
				OpenRA.Graphics.Util.FastCreateQuad(
					vertices, location + sprite.FractionalOffset * size,
					sprite, palette.TextureIndex, offset, size);
			}

			spriteLayer[uv] = sprite;
			return sprite;
		}

		Sprite GetSprite(Sprite[] sprites, Edges edges, int variant)
		{
			if (edges == Edges.None)
				return null;

			return sprites[variant * variantStride + edgesToSpriteIndexOffset[(byte)edges]];
		}
	}
}
