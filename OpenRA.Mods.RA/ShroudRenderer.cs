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

namespace OpenRA.Mods.RA
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

		class ShroudTile
		{
			public readonly CPos Position;
			public readonly float2 ScreenPosition;
			public readonly int Variant;

			public Sprite Fog;
			public Sprite Shroud;

			public ShroudTile(CPos position, float2 screenPosition, int variant)
			{
				Position = position;
				ScreenPosition = screenPosition;
				Variant = variant;
			}
		}

		readonly ShroudRendererInfo info;
		readonly Sprite[] shroudSprites, fogSprites;
		readonly int[] spriteMap;
		readonly CellLayer<ShroudTile> tiles;
		readonly int variantStride;
		readonly Map map;
		readonly Edges notVisibleEdges;

		PaletteReference fogPalette, shroudPalette;
		int shroudHash;

		public ShroudRenderer(World world, ShroudRendererInfo info)
		{
			if (info.ShroudVariants.Length != info.FogVariants.Length)
				throw new ArgumentException("ShroudRenderer must define the same number of shroud and fog variants!", "info");

			if ((info.OverrideFullFog == null) ^ (info.OverrideFullShroud == null))
				throw new ArgumentException("ShroudRenderer cannot define overrides for only one of shroud or fog!", "info");

			this.info = info;
			map = world.Map;

			tiles = new CellLayer<ShroudTile>(map);

			// Force update on first render
			shroudHash = -1;

			// Load sprite variants
			var variantCount = info.ShroudVariants.Length;
			variantStride = info.Index.Length + (info.OverrideFullShroud != null ? 1 : 0);
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
			spriteMap = new int[info.UseExtendedIndex ? 256 : 16];
			for (var i = 0; i < info.Index.Length; i++)
				spriteMap[info.Index[i]] = i;

			if (info.OverrideFullShroud != null)
				spriteMap[info.OverrideShroudIndex] = variantStride - 1;

			notVisibleEdges = info.UseExtendedIndex ? Edges.AllSides : Edges.AllCorners;
		}

		Edges GetEdges(int x, int y, Func<int, int, bool> isVisible)
		{
			if (!isVisible(x, y))
				return notVisibleEdges;

			// If a side is shrouded then we also count the corners
			var u = Edges.None;
			if (!isVisible(x, y - 1)) u |= Edges.Top;
			if (!isVisible(x + 1, y)) u |= Edges.Right;
			if (!isVisible(x, y + 1)) u |= Edges.Bottom;
			if (!isVisible(x - 1, y)) u |= Edges.Left;

			var ucorner = u & Edges.AllCorners;
			if (!isVisible(x - 1, y - 1)) u |= Edges.TopLeft;
			if (!isVisible(x + 1, y - 1)) u |= Edges.TopRight;
			if (!isVisible(x + 1, y + 1)) u |= Edges.BottomRight;
			if (!isVisible(x - 1, y + 1)) u |= Edges.BottomLeft;

			// RA provides a set of frames for tiles with shrouded
			// corners but unshrouded edges. We want to detect this
			// situation without breaking the edge -> corner enabling
			// in other combinations. The XOR turns off the corner
			// bits that are enabled twice, which gives the behavior
			// we want here.
			return info.UseExtendedIndex ? u ^ ucorner : u & Edges.AllCorners;
		}

		Edges GetObserverEdges(int x, int y)
		{
			var u = Edges.None;
			if (!map.Contains(new CPos(x, y - 1))) u |= Edges.Top;
			if (!map.Contains(new CPos(x + 1, y))) u |= Edges.Right;
			if (!map.Contains(new CPos(x, y + 1))) u |= Edges.Bottom;
			if (!map.Contains(new CPos(x - 1, y))) u |= Edges.Left;

			var ucorner = u & Edges.AllCorners;
			if (!map.Contains(new CPos(x - 1, y - 1))) u |= Edges.TopLeft;
			if (!map.Contains(new CPos(x + 1, y - 1))) u |= Edges.TopRight;
			if (!map.Contains(new CPos(x + 1, y + 1))) u |= Edges.BottomRight;
			if (!map.Contains(new CPos(x - 1, y + 1))) u |= Edges.BottomLeft;

			return info.UseExtendedIndex ? u ^ ucorner : u & Edges.AllCorners;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			// Initialize tile cache
			// Adds a 1-cell border around the border to cover any sprites peeking outside the map
			foreach (var cell in CellRegion.Expand(w.Map.Cells, 1))
			{
				var screen = wr.ScreenPosition(w.Map.CenterOfCell(cell));
				var variant = Game.CosmeticRandom.Next(info.ShroudVariants.Length);
				tiles[cell] = new ShroudTile(cell, screen, variant);

				// Set the cells outside the border so they don't need to be touched again
				if (!map.Contains(cell))
					tiles[cell].Shroud = GetTile(notVisibleEdges, variant);
			}

			fogPalette = wr.Palette(info.FogPalette);
			shroudPalette = wr.Palette(info.ShroudPalette);
		}

		Sprite GetTile(Edges edges, int variant)
		{
			if (edges == Edges.None)
				return null;

			return shroudSprites[variant * variantStride + spriteMap[(byte)edges]];
		}

		void Update(Shroud shroud)
		{
			var hash = shroud != null ? shroud.Hash : 0;
			if (shroudHash == hash)
				return;

			shroudHash = hash;
			if (shroud == null)
			{
				// Players with no shroud see the whole map so we only need to set the edges
				foreach (var cell in map.Cells)
				{
					var t = tiles[cell];
					var shrouded = GetObserverEdges(cell.X, cell.Y);

					t.Shroud = shrouded != 0 ? shroudSprites[t.Variant * variantStride + spriteMap[(byte)shrouded]] : null;
					t.Fog = shrouded != 0 ? fogSprites[t.Variant * variantStride + spriteMap[(byte)shrouded]] : null;
				}
			}
			else
			{
				foreach (var cell in map.Cells)
				{
					Func<int, int, bool> visibleUnderShroud = (x, y) => shroud.IsExplored(new CPos(x, y));
					Func<int, int, bool> visibleUnderFog = (x, y) => shroud.IsVisible(new CPos(x, y));
					var t = tiles[cell];
					var shrouded = GetEdges(cell.X, cell.Y, visibleUnderShroud);
					var fogged = GetEdges(cell.X, cell.Y, visibleUnderFog);

					t.Shroud = shrouded != 0 ? shroudSprites[t.Variant * variantStride + spriteMap[(byte)shrouded]] : null;
					t.Fog = fogged != 0 ? fogSprites[t.Variant * variantStride + spriteMap[(byte)fogged]] : null;
				}
			}
		}

		public void RenderShroud(WorldRenderer wr, Shroud shroud)
		{
			Update(shroud);

			foreach (var cell in CellRegion.Expand(wr.Viewport.VisibleCells, 1))
			{
				var t = tiles[cell];

				if (t.Shroud != null)
				{
					var pos = t.ScreenPosition - 0.5f * t.Shroud.size;
					Game.Renderer.WorldSpriteRenderer.DrawSprite(t.Shroud, pos, shroudPalette);
				}

				if (t.Fog != null)
				{
					var pos = t.ScreenPosition - 0.5f * t.Fog.size;
					Game.Renderer.WorldSpriteRenderer.DrawSprite(t.Fog, pos, fogPalette);
				}
			}
		}
	}
}
