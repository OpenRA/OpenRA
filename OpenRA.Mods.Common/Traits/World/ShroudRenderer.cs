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
using System.Drawing.Imaging;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
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

	public sealed class ShroudRenderer : IRenderShroud, IWorldLoaded, IDisposable
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

		static readonly Sprite EmptySprite = new Sprite(new Sheet(new Size(1, 1)), Rectangle.Empty, TextureChannel.Alpha);

		readonly ShroudRendererInfo info;
		readonly Map map;
		readonly Edges notVisibleEdges;
		readonly byte variantStride;
		readonly byte[] edgesToSpriteIndexOffset;

		readonly CellLayer<TileInfo> tileInfos;
		readonly CellLayer<bool> shroudDirty;
		readonly HashSet<CPos> cellsDirty;
		readonly HashSet<CPos> cellsAndNeighborsDirty;

		readonly int verticesLength;
		readonly SheetBuilder sheetBuilder;
		readonly Sheet sheet;
		readonly BlendMode blendMode;
		readonly Vertex[] quadBuffer = new Vertex[4];
		readonly IVertexBuffer<Vertex> fogVertexBuffer, shroudVertexBuffer;
		readonly Sprite[] fogSprites, shroudSprites;
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
			cellsDirty = new HashSet<CPos>();
			cellsAndNeighborsDirty = new HashSet<CPos>();
			verticesLength = map.MapSize.X * map.MapSize.Y * 4;
			fogVertexBuffer = Game.Renderer.CreateVertexBuffer(verticesLength);
			shroudVertexBuffer = Game.Renderer.CreateVertexBuffer(verticesLength);

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

			// Enforce that all sprites reside on the same sheet to make life easy during rendering.
			// If all the sprites are on one sheet, we can just render the vertex buffers in one pass.
			var allSprites = shroudSprites.Concat(fogSprites);
			sheet = shroudSprites[0].Sheet;
			if (allSprites.All(sprite => sprite.Sheet == sheet))
				sheet = CopySpritesOntoSingleSheet(out sheetBuilder);

			blendMode = allSprites.Select(sprite => sprite.BlendMode).Distinct().Single();

			// Mapping of shrouded directions -> sprite index
			edgesToSpriteIndexOffset = new byte[(byte)(info.UseExtendedIndex ? Edges.All : Edges.AllCorners) + 1];
			for (var i = 0; i < info.Index.Length; i++)
				edgesToSpriteIndexOffset[info.Index[i]] = (byte)i;

			if (info.OverrideFullShroud != null)
				edgesToSpriteIndexOffset[info.OverrideShroudIndex] = (byte)(variantStride - 1);

			notVisibleEdges = info.UseExtendedIndex ? Edges.AllSides : Edges.AllCorners;
		}

		Sheet CopySpritesOntoSingleSheet(out SheetBuilder sheetBuilder)
		{
			sheetBuilder = null;
			var allSprites = shroudSprites.Concat(fogSprites);
			var sizeEstimate = Exts.NextPowerOf2((int)Math.Sqrt(allSprites.Sum(sprite => sprite.Size.X * sprite.Size.Y) / 4.0));
			var width = sizeEstimate;
			var height = sizeEstimate;
			var sheetBitmaps = new Cache<Sheet, Bitmap>(s => s.AsBitmap());
			try
			{
				do
				{
					var builder = new SheetBuilder(SheetType.BGRA, Exts.NextPowerOf2(new Size(width, height)));
					try
					{
						var initialSheet = builder.Current;
						Action<Sprite[]> copySpritesToNewSheet = sprites =>
						{
							for (var i = 0; i < sprites.Length; i++)
							{
								var sprite = sprites[i];
								using (var spriteBitmap = sheetBitmaps[sprite.Sheet].Clone(sprite.Bounds, PixelFormat.Format32bppArgb))
									sprites[i] = builder.Add(spriteBitmap);
							}
						};
						copySpritesToNewSheet(shroudSprites);
						copySpritesToNewSheet(fogSprites);
						if (initialSheet == builder.Current)
							sheetBuilder = builder;
						else
						{
							// If we overflowed onto more sheets, we need to try again with bigger sheets until everything fit onto one.
							if (width > height)
								height *= 2;
							else
								width *= 2;
						}
					}
					finally
					{
						if (sheetBuilder == null)
							builder.Dispose();
					}
				}
				while (sheetBuilder == null);
			}
			finally
			{
				foreach (var bitmap in sheetBitmaps.Values)
					bitmap.Dispose();
			}

			return sheetBuilder.Current;
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
					currentShroud.CellsChanged -= MarkCellsDirty;

				if (newShroud != null)
				{
					shroudDirty.Clear(true);
					newShroud.CellsChanged += MarkCellsDirty;
				}

				cellsDirty.Clear();
				cellsAndNeighborsDirty.Clear();

				currentShroud = newShroud;
			}

			if (currentShroud != null)
			{
				mapBorderShroudIsCached = false;

				// We need to mark newly dirtied areas of the shroud.
				// Expand the dirty area to cover the neighboring cells, since shroud is affected by neighboring cells.
				foreach (var cell in cellsDirty)
				{
					cellsAndNeighborsDirty.Add(cell);
					foreach (var direction in CVec.Directions)
						cellsAndNeighborsDirty.Add(cell + direction);
				}

				foreach (var cell in cellsAndNeighborsDirty)
					shroudDirty[cell] = true;

				cellsDirty.Clear();
				cellsAndNeighborsDirty.Clear();
			}
			else if (!mapBorderShroudIsCached)
			{
				mapBorderShroudIsCached = true;
				CacheMapBorderShroud();
			}
		}

		void MarkCellsDirty(IEnumerable<CPos> cellsChanged)
		{
			// Mark changed cells as being out of date.
			// We don't want to do anything more than this for several performance reasons:
			// - If the cells remain off-screen for a long time, they may change several times before we next view
			// them, so calculating their new vertices is wasted effort since we may recalculate them again before we
			// even get a chance to render them.
			// - Cells tend to be invalidated in groups (imagine as a unit moves, it advances a wave of sight and
			// leaves a trail of fog filling in behind). If we recalculated a cell and its neighbors when the first
			// cell in a group changed, many cells would be recalculated again when the second cell, right next to the
			// first, is updated. In fact we might do on the order of 3x the work we needed to!
			cellsDirty.UnionWith(cellsChanged);
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
				CacheTile(offset, edges, tileInfo, shroudSprites, shroudPalette, shroudVertexBuffer);
				CacheTile(offset, edges, tileInfo, fogSprites, fogPalette, fogVertexBuffer);
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
			RenderShroud();
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
				if (!shroudDirty[uv])
					continue;
				shroudDirty[uv] = false;
				var offset = VertexArrayOffset(uv);
				var tileInfo = tileInfos[uv];
				CacheTile(offset, GetEdges(uv, visibleUnderShroud), tileInfo, shroudSprites, shroudPalette, shroudVertexBuffer);
				CacheTile(offset, GetEdges(uv, visibleUnderFog), tileInfo, fogSprites, fogPalette, fogVertexBuffer);
			}

			RenderShroud();
		}

		void RenderShroud()
		{
			Game.Renderer.Flush();
			Game.Renderer.WorldSpriteRenderer.DrawVertexBuffer(
				shroudVertexBuffer, 0, verticesLength,
				PrimitiveType.QuadList, sheet, blendMode);
			Game.Renderer.WorldSpriteRenderer.DrawVertexBuffer(
				fogVertexBuffer, 0, verticesLength,
				PrimitiveType.QuadList, sheet, blendMode);
		}

		int VertexArrayOffset(MPos uv)
		{
			return 4 * (uv.V * map.MapSize.X + uv.U);
		}

		void CacheTile(int offset, Edges edges, TileInfo tileInfo,
			Sprite[] sprites, PaletteReference palette, IVertexBuffer<Vertex> vertexBuffer)
		{
			var sprite = GetSprite(sprites, edges, tileInfo.Variant);
			if (sprite != null)
			{
				var location = tileInfo.ScreenPosition - 0.5f * sprite.Size;
				OpenRA.Graphics.Util.FastCreateQuad(quadBuffer, location, sprite, palette, 0);
			}
			else
			{
				OpenRA.Graphics.Util.FastCreateQuad(quadBuffer, float2.Zero, EmptySprite, 0, 0, float2.Zero);
			}

			vertexBuffer.SetData(quadBuffer, offset, 4);
		}

		Sprite GetSprite(Sprite[] sprites, Edges edges, int variant)
		{
			if (edges == Edges.None)
				return null;

			return sprites[variant * variantStride + edgesToSpriteIndexOffset[(byte)edges]];
		}

		public void Dispose()
		{
			fogVertexBuffer.Dispose();
			shroudVertexBuffer.Dispose();
			if (sheetBuilder != null)
				sheetBuilder.Dispose();
		}
	}
}
