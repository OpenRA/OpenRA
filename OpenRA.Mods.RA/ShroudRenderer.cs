#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ShroudRendererInfo : ITraitInfo
	{
		public string Sequence = "shroud";
		public string[] Variants = new[] { "shroud" };

		[Desc("Bitfield of shroud directions for each frame. Lower four bits are",
		      "corners clockwise from TL; upper four are edges clockwise from top")]
		public int[] Index = new[] { 12, 9, 8, 3, 1, 6, 4, 2, 13, 11, 7, 14 };

		[Desc("Use the upper four bits when calculating frame")]
		public bool UseExtendedIndex = false;

		[Desc("Palette index for synthesized unexplored tile")]
		public int ShroudColor = 12;
		public BlendMode ShroudBlend = BlendMode.Alpha;
		public object Create(ActorInitializer init) { return new ShroudRenderer(init.world, this); }
	}

	public class ShroudRenderer : IRenderShroud, IWorldLoaded
	{
		[Flags]
		enum Edges
		{
			None = 0,
			TopLeft = 0x01,
			TopRight = 0x02,
			BottomRight = 0x04,
			BottomLeft = 0x08,
			AllCorners = TopLeft | TopRight | BottomRight | BottomLeft,
			Top = 0x10 | TopLeft | TopRight,
			Right = 0x20 | TopRight | BottomRight,
			Bottom = 0x40 | BottomRight | BottomLeft,
			Left = 0x80 | TopLeft | BottomLeft
		}

		struct ShroudTile
		{
			public float2 ScreenPosition;
			public int Variant;

			public Sprite Fog;
			public Sprite Shroud;
		}

		readonly Sprite[] sprites;
		readonly Sprite unexploredTile;
		readonly int[] spriteMap;
		readonly bool useExtendedIndex;

		readonly Rectangle bounds;
		readonly ShroudTile[] tiles;
		readonly int tileStride, variantStride;

		bool clearedForNullShroud;
		int lastShroudHash;
		Rectangle updatedBounds;

		PaletteReference fogPalette, shroudPalette;

		public ShroudRenderer(World world, ShroudRendererInfo info)
		{
			var map = world.Map;
			bounds = map.Bounds;
			useExtendedIndex = info.UseExtendedIndex;

			tiles = new ShroudTile[map.MapSize.X * map.MapSize.Y];
			tileStride = map.MapSize.X;

			// Load sprite variants
			sprites = new Sprite[info.Variants.Length * info.Index.Length];
			variantStride = info.Index.Length;
			for (var j = 0; j < info.Variants.Length; j++)
			{
				var seq = map.SequenceProvider.GetSequence(info.Sequence, info.Variants[j]);
				for (var i = 0; i < info.Index.Length; i++)
					sprites[j * variantStride + i] = seq.GetSprite(i);
			}

			// Mapping of shrouded directions -> sprite index
			spriteMap = new int[useExtendedIndex ? 256 : 16];
			for (var i = 0; i < info.Index.Length; i++)
				spriteMap[info.Index[i]] = i;

			// Set individual tile variants to reduce tiling
			for (var i = 0; i < tiles.Length; i++)
				tiles[i].Variant = Game.CosmeticRandom.Next(info.Variants.Length);

			// Synthesize unexplored tile if it isn't defined
			if (!info.Index.Contains(0))
			{
				var ts = Game.modData.Manifest.TileSize;
				var data = Exts.MakeArray<byte>(ts.Width * ts.Height, _ => (byte)info.ShroudColor);
				var s = map.SequenceProvider.SpriteLoader.SheetBuilder.Add(data, ts);
				unexploredTile = new Sprite(s.sheet, s.bounds, s.offset, s.channel, info.ShroudBlend);
			}
			else
				unexploredTile = sprites[spriteMap[0]];
		}

		static Edges GetEdges(int x, int y, bool useExtendedIndex, Func<int, int, bool> isVisible)
		{
			if (!isVisible(x, y))
				return Edges.AllCorners;

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
			return useExtendedIndex ? u ^ ucorner : u & Edges.AllCorners;
		}

		static Edges GetObserverEdges(int x, int y, bool useExtendedIndex, Rectangle bounds)
		{
			var top = y == bounds.Top;
			var right = x == bounds.Right - 1;
			var bottom = y == bounds.Bottom - 1;
			var left = x == bounds.Left;

			var u = Edges.None;
			if (top) u |= Edges.Top;
			if (right) u |= Edges.Right;
			if (bottom) u |= Edges.Bottom;
			if (left) u |= Edges.Left;

			var ucorner = u & Edges.AllCorners;
			if (top && left) u |= Edges.TopLeft;
			if (top && right) u |= Edges.TopRight;
			if (bottom && right) u |= Edges.BottomRight;
			if (bottom && left) u |= Edges.BottomLeft;

			return useExtendedIndex ? u ^ ucorner : u & Edges.AllCorners;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			// Cache the tile positions to avoid unnecessary calculations
			var xMin = bounds.Left;
			var yMin = bounds.Top;
			var xMax = bounds.Right;
			var yMax = bounds.Bottom;
			for (var y = yMin; y < yMax; y++)
			{
				var rowIndex = y * tileStride;
				for (var x = xMin; x < xMax; x++)
					tiles[rowIndex + x].ScreenPosition = wr.ScreenPosition(new CPos(x, y).CenterPosition);
			}

			fogPalette = wr.Palette("fog");
			shroudPalette = wr.Palette("shroud");
		}

		Sprite GetTile(Edges edges, int variant)
		{
			if (edges == Edges.None)
				return null;

			if (edges == Edges.AllCorners)
				return unexploredTile;

			return sprites[variant * variantStride + spriteMap[(int)edges]];
		}

		void Update(Shroud shroud, int left, int top, int right, int bottom)
		{
			if (shroud != null)
			{
				// If the current shroud hasn't changed and we have already updated the specified area, we don't need to do anything.
				var newBounds = Rectangle.FromLTRB(left, top, right, bottom);
				if (lastShroudHash == shroud.Hash && !clearedForNullShroud && updatedBounds.Contains(newBounds))
					return;

				lastShroudHash = shroud.Hash;
				clearedForNullShroud = false;
				updatedBounds = newBounds;
				UpdateShroud(shroud);
			}
			else if (!clearedForNullShroud)
			{
				// We need to clear any applied shroud.
				clearedForNullShroud = true;
				updatedBounds = Rectangle.Empty;
				UpdateNullShroud();
			}
		}

		void UpdateShroud(Shroud shroud)
		{
			Func<int, int, bool> visibleUnderShroud = shroud.IsExplored;
			Func<int, int, bool> visibleUnderFog = shroud.IsVisible;
			var xMin = updatedBounds.Left;
			var yMin = updatedBounds.Top;
			var xMax = updatedBounds.Right;
			var yMax = updatedBounds.Bottom;
			for (var y = yMin; y < yMax; y++)
			{
				var rowIndex = y * tileStride;
				for (var x = xMin; x < xMax; x++)
				{
					var index = rowIndex + x;
					var shrouded = GetEdges(x, y, useExtendedIndex, visibleUnderShroud);
					var fogged = GetEdges(x, y, useExtendedIndex, visibleUnderFog);
					var variant = tiles[index].Variant;
					tiles[index].Shroud = GetTile(shrouded, variant);
					tiles[index].Fog = GetTile(fogged, variant);
				}
			}
		}

		void UpdateNullShroud()
		{
			var xMin = bounds.Left;
			var yMin = bounds.Top;
			var xMax = bounds.Right;
			var yMax = bounds.Bottom;
			for (var y = yMin; y < yMax; y++)
			{
				var rowIndex = y * tileStride;
				for (var x = xMin; x < xMax; x++)
				{
					var index = rowIndex + x;
					var mask = GetObserverEdges(x, y, useExtendedIndex, bounds);
					var tile = GetTile(mask, tiles[index].Variant);
					tiles[index].Shroud = tile;
					tiles[index].Fog = tile;
				}
			}
		}

		public void RenderShroud(WorldRenderer wr, Shroud shroud)
		{
			var clip = wr.Viewport.CellBounds;
			var top = clip.Top;
			var bottom = clip.Bottom;
			var left = clip.Left;
			var right = clip.Right;

			Update(shroud, left, top, right, bottom);

			for (var y = top; y < bottom; y++)
			{
				var rowIndex = y * tileStride;
				for (var x = left; x < right; x++)
				{
					var tile = tiles[rowIndex + x];
					var s = tile.Shroud;
					var f = tile.Fog;

					if (s != null)
					{
						var pos = tile.ScreenPosition - 0.5f * s.size;
						Game.Renderer.WorldSpriteRenderer.DrawSprite(s, pos, shroudPalette);
					}

					if (f != null)
					{
						var pos = tile.ScreenPosition - 0.5f * f.size;
						Game.Renderer.WorldSpriteRenderer.DrawSprite(f, pos, fogPalette);
					}
				}
			}
		}
	}
}
