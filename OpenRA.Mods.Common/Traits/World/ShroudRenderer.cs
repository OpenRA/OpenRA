#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
		public object Create(ActorInitializer init) { return new ShroudRenderer(init.world, this); }
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
		readonly Sprite[] fogSprites, shroudSprites;

		Shroud shroud;
		bool clearedForNullShroud;
		PaletteReference fogPalette, shroudPalette;
		readonly Vertex[] fogVertices, shroudVertices;
		readonly CellLayer<Sprite> fogSpriteLayer, shroudSpriteLayer;

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

		Edges GetEdges(int u, int v, Func<int, int, bool> isVisible)
		{
			if (!isVisible(u, v))
				return notVisibleEdges;

			var cell = Map.MapToCell(map.TileShape, new CPos(u, v));
			Func<CPos, bool> isCellVisible = c =>
			{
				var uv = Map.CellToMap(map.TileShape, c);
				return isVisible(uv.X, uv.Y);
			};

			// If a side is shrouded then we also count the corners
			var edge = Edges.None;
			if (!isCellVisible(cell + new CVec(0, -1))) edge |= Edges.Top;
			if (!isCellVisible(cell + new CVec(1, 0))) edge |= Edges.Right;
			if (!isCellVisible(cell + new CVec(0, 1))) edge |= Edges.Bottom;
			if (!isCellVisible(cell + new CVec(-1, 0))) edge |= Edges.Left;

			var ucorner = edge & Edges.AllCorners;
			if (!isCellVisible(cell + new CVec(-1, -1))) edge |= Edges.TopLeft;
			if (!isCellVisible(cell + new CVec(1, -1))) edge |= Edges.TopRight;
			if (!isCellVisible(cell + new CVec(1, 1))) edge |= Edges.BottomRight;
			if (!isCellVisible(cell + new CVec(-1, 1))) edge |= Edges.BottomLeft;

			// RA provides a set of frames for tiles with shrouded
			// corners but unshrouded edges. We want to detect this
			// situation without breaking the edge -> corner enabling
			// in other combinations. The XOR turns off the corner
			// bits that are enabled twice, which gives the behavior
			// we want here.
			return info.UseExtendedIndex ? edge ^ ucorner : edge & Edges.AllCorners;
		}

		Edges GetObserverEdges(CPos p)
		{
			var u = Edges.None;
			if (!map.Contains(p + new CVec(0, -1))) u |= Edges.Top;
			if (!map.Contains(p + new CVec(1, 0))) u |= Edges.Right;
			if (!map.Contains(p + new CVec(0, 1))) u |= Edges.Bottom;
			if (!map.Contains(p + new CVec(-1, 0))) u |= Edges.Left;

			var ucorner = u & Edges.AllCorners;
			if (!map.Contains(p + new CVec(-1, -1))) u |= Edges.TopLeft;
			if (!map.Contains(p + new CVec(1, -1))) u |= Edges.TopRight;
			if (!map.Contains(p + new CVec(1, 1))) u |= Edges.BottomRight;
			if (!map.Contains(p + new CVec(-1, 1))) u |= Edges.BottomLeft;

			return info.UseExtendedIndex ? u ^ ucorner : u & Edges.AllCorners;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			// Initialize tile cache
			// Adds a 1-cell border around the border to cover any sprites peeking outside the map
			foreach (var uv in CellRegion.Expand(w.Map.Cells, 1).MapCoords)
			{
				var u = uv.X;
				var v = uv.Y;
				var screen = wr.ScreenPosition(w.Map.CenterOfCell(Map.MapToCell(map.TileShape, uv)));
				var variant = (byte)Game.CosmeticRandom.Next(info.ShroudVariants.Length);
				tileInfos[u, v] = new TileInfo(screen, variant);
			}

			fogPalette = wr.Palette(info.FogPalette);
			shroudPalette = wr.Palette(info.ShroudPalette);
		}

		public void RenderShroud(WorldRenderer wr, Shroud shroud)
		{
			Update(shroud);
			Render(wr.Viewport.VisibleCells);
		}

		void Update(Shroud updateShroud)
		{
			if (shroud != updateShroud)
			{
				if (shroud != null)
					shroud.CellEntryChanged -= MarkCellAndNeighborsDirty;

				if (updateShroud != null)
				{
					shroudDirty.Clear(true);
					updateShroud.CellEntryChanged += MarkCellAndNeighborsDirty;
				}

				shroud = updateShroud;
			}

			if (shroud != null)
			{
				clearedForNullShroud = false;
			}
			else if (!clearedForNullShroud)
			{
				clearedForNullShroud = true;
				UpdateNullShroud();
			}
		}

		void MarkCellAndNeighborsDirty(CPos cell)
		{
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

		void UpdateNullShroud()
		{
			foreach (var uv in map.Cells.MapCoords)
			{
				var u = uv.X;
				var v = uv.Y;
				var offset = Offset(u, v);
				var edges = GetObserverEdges(Map.MapToCell(map.TileShape, uv));
				var tileInfo = tileInfos[u, v];
				CacheTile(u, v, offset, edges, tileInfo, shroudSprites, shroudVertices, shroudPalette, shroudSpriteLayer);
				CacheTile(u, v, offset, edges, tileInfo, fogSprites, fogVertices, fogPalette, fogSpriteLayer);
			}
		}

		void Render(CellRegion visibleRegion)
		{
			var renderRegion = CellRegion.Expand(visibleRegion, 1).MapCoords;

			// Render observer shroud.
			if (shroud == null)
			{
				foreach (var uv in renderRegion)
				{
					var u = uv.X;
					var v = uv.Y;
					var offset = Offset(u, v);
					RenderCachedTile(shroudSpriteLayer[u, v], shroudVertices, offset);
					RenderCachedTile(fogSpriteLayer[u, v], fogVertices, offset);
				}

				return;
			}

			// Render player shroud.
			var visibleUnderShroud = shroud.IsExploredTest(visibleRegion);
			var visibleUnderFog = shroud.IsVisibleTest(visibleRegion);
			foreach (var uv in renderRegion)
			{
				var u = uv.X;
				var v = uv.Y;
				var offset = Offset(u, v);
				if (shroudDirty[u, v])
				{
					shroudDirty[u, v] = false;
					RenderDirtyTile(u, v, offset, visibleUnderShroud, shroudSprites, shroudVertices, shroudPalette, shroudSpriteLayer);
					RenderDirtyTile(u, v, offset, visibleUnderFog, fogSprites, fogVertices, fogPalette, fogSpriteLayer);
				}
				else
				{
					RenderCachedTile(shroudSpriteLayer[u, v], shroudVertices, offset);
					RenderCachedTile(fogSpriteLayer[u, v], fogVertices, offset);
				}
			}
		}

		int Offset(int u, int v)
		{
			return 4 * (v * map.MapSize.X + u);
		}

		Sprite CacheTile(int u, int v, int offset, Edges edges, TileInfo tileInfo,
			Sprite[] sprites, Vertex[] vertices, PaletteReference palette, CellLayer<Sprite> spriteLayer)
		{
			var sprite = GetSprite(sprites, edges, tileInfo.Variant);
			if (sprite != null)
			{
				var size = sprite.size;
				var location = tileInfo.ScreenPosition - 0.5f * size;
				OpenRA.Graphics.Util.FastCreateQuad(vertices, location + sprite.fractionalOffset * size,
					sprite, palette.Index, offset, size);
			}

			spriteLayer[u, v] = sprite;
			return sprite;
		}

		Sprite GetSprite(Sprite[] sprites, Edges edges, int variant)
		{
			if (edges == Edges.None)
				return null;

			return sprites[variant * variantStride + edgesToSpriteIndexOffset[(byte)edges]];
		}

		void RenderDirtyTile(int u, int v, int offset, Func<int, int, bool> isVisible,
			Sprite[] sprites, Vertex[] vertices, PaletteReference palette, CellLayer<Sprite> spriteLayer)
		{
			var tile = tileInfos[u, v];
			var edges = GetEdges(u, v, isVisible);
			var sprite = CacheTile(u, v, offset, edges, tile, sprites, vertices, palette, spriteLayer);
			RenderCachedTile(sprite, vertices, offset);
		}

		void RenderCachedTile(Sprite sprite, Vertex[] vertices, int offset)
		{
			if (sprite != null)
				Game.Renderer.WorldSpriteRenderer.DrawSprite(sprite, vertices, offset);
		}
	}
}
