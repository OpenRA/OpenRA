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

		struct ShroudTile
		{
			public readonly float2 ScreenPosition;
			public readonly byte Variant;

			public Sprite Fog;
			public Sprite Shroud;

			public ShroudTile(float2 screenPosition, byte variant)
			{
				ScreenPosition = screenPosition;
				Variant = variant;

				Fog = null;
				Shroud = null;
			}
		}

		readonly ShroudRendererInfo info;
		readonly Sprite[] shroudSprites, fogSprites;
		readonly byte[] spriteMap;
		readonly CellLayer<ShroudTile> tiles;
		readonly byte variantStride;
		readonly Map map;
		readonly Edges notVisibleEdges;

		bool clearedForNullShroud;
		int lastShroudHash;
		CellRegion updatedRegion;

		PaletteReference fogPalette, shroudPalette;

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

			tiles = new CellLayer<ShroudTile>(map);

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
			spriteMap = new byte[(byte)(info.UseExtendedIndex ? Edges.All : Edges.AllCorners) + 1];
			for (var i = 0; i < info.Index.Length; i++)
				spriteMap[info.Index[i]] = (byte)i;

			if (info.OverrideFullShroud != null)
				spriteMap[info.OverrideShroudIndex] = (byte)(variantStride - 1);

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
				tiles[u, v] = new ShroudTile(screen, variant);

				// Set the cells outside the border so they don't need to be touched again
				if (!map.Contains(u, v))
				{
					var shroudTile = tiles[u, v];
					shroudTile.Shroud = GetTile(shroudSprites, notVisibleEdges, variant);
					tiles[u, v] = shroudTile;
				}
			}

			fogPalette = wr.Palette(info.FogPalette);
			shroudPalette = wr.Palette(info.ShroudPalette);
		}

		Sprite GetTile(Sprite[] sprites, Edges edges, int variant)
		{
			if (edges == Edges.None)
				return null;

			return sprites[variant * variantStride + spriteMap[(byte)edges]];
		}

		void Update(Shroud shroud, CellRegion region)
		{
			if (shroud != null)
			{
				// If the current shroud hasn't changed and we have already updated the specified area, we don't need to do anything.
				if (lastShroudHash == shroud.Hash && !clearedForNullShroud && updatedRegion != null && updatedRegion.Contains(region))
					return;

				lastShroudHash = shroud.Hash;
				clearedForNullShroud = false;
				updatedRegion = region;
				UpdateShroud(shroud);
			}
			else if (!clearedForNullShroud)
			{
				// We need to clear any applied shroud.
				clearedForNullShroud = true;
				updatedRegion = new CellRegion(map.TileShape, new CPos(0, 0), new CPos(-1, -1));
				UpdateNullShroud();
			}
		}

		void UpdateShroud(Shroud shroud)
		{
			var visibleUnderShroud = shroud.IsExploredTest(updatedRegion);
			var visibleUnderFog = shroud.IsVisibleTest(updatedRegion);
			foreach (var uv in updatedRegion.MapCoords)
			{
				var u = uv.X;
				var v = uv.Y;
				var shrouded = GetEdges(u, v, visibleUnderShroud);
				var fogged = GetEdges(u, v, visibleUnderFog);
				var shroudTile = tiles[u, v];
				var variant = shroudTile.Variant;
				shroudTile.Shroud = GetTile(shroudSprites, shrouded, variant);
				shroudTile.Fog = GetTile(fogSprites, fogged, variant);
				tiles[u, v] = shroudTile;
			}
		}

		void UpdateNullShroud()
		{
			foreach (var cell in map.Cells)
			{
				var edges = GetObserverEdges(cell);
				var shroudTile = tiles[cell];
				var variant = shroudTile.Variant;
				shroudTile.Shroud = GetTile(shroudSprites, edges, variant);
				shroudTile.Fog = GetTile(fogSprites, edges, variant);
				tiles[cell] = shroudTile;
			}
		}

		public void RenderShroud(WorldRenderer wr, Shroud shroud)
		{
			Update(shroud, wr.Viewport.VisibleCells);

			foreach (var uv in CellRegion.Expand(wr.Viewport.VisibleCells, 1).MapCoords)
			{
				var t = tiles[uv.X, uv.Y];

				if (t.Shroud != null)
				{
					var pos = t.ScreenPosition - 0.5f * t.Shroud.Size;
					Game.Renderer.WorldSpriteRenderer.DrawSprite(t.Shroud, pos, shroudPalette);
				}

				if (t.Fog != null)
				{
					var pos = t.ScreenPosition - 0.5f * t.Fog.Size;
					Game.Renderer.WorldSpriteRenderer.DrawSprite(t.Fog, pos, fogPalette);
				}
			}
		}
	}
}
