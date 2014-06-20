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
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ShroudRendererInfo : ITraitInfo
	{
		public readonly string Sequence = "shroud";
		public readonly string[] Variants = new[] { "shroud" };

		public readonly string ShroudPalette = "shroud";
		public readonly string FogPalette = "fog";

		[Desc("Bitfield of shroud directions for each frame. Lower four bits are",
		      "corners clockwise from TL; upper four are edges clockwise from top")]
		public readonly int[] Index = new[] { 12, 9, 8, 3, 1, 6, 4, 2, 13, 11, 7, 14 };

		[Desc("Use the upper four bits when calculating frame")]
		public readonly bool UseExtendedIndex = false;

		[Desc("Palette index for synthesized unexplored tile")]
		public readonly int ShroudColor = 12;
		public readonly BlendMode ShroudBlend = BlendMode.Alpha;
		public object Create(ActorInitializer init) { return new ShroudRenderer(init.world, this); }
	}

	public class ShroudRenderer : IRenderShroud, IWorldLoaded
	{
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
		readonly Sprite[] sprites;
		readonly Sprite unexploredTile;
		readonly int[] spriteMap;
		readonly CellLayer<ShroudTile> tiles;
		readonly int variantStride;
		readonly Map map;

		PaletteReference fogPalette, shroudPalette;
		int shroudHash;

		public ShroudRenderer(World world, ShroudRendererInfo info)
		{
			this.info = info;
			map = world.Map;

			tiles = new CellLayer<ShroudTile>(map);

			// Force update on first render
			shroudHash = -1;

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
			spriteMap = new int[info.UseExtendedIndex ? 256 : 16];
			for (var i = 0; i < info.Index.Length; i++)
				spriteMap[info.Index[i]] = i;

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

		static int FoggedEdges(Shroud s, CPos p, bool useExtendedIndex)
		{
			if (!s.IsVisible(p))
				return 15;

			// If a side is shrouded then we also count the corners
			var u = 0;
			if (!s.IsVisible(p + new CVec(0, -1))) u |= 0x13;
			if (!s.IsVisible(p + new CVec(1, 0))) u |= 0x26;
			if (!s.IsVisible(p + new CVec(0, 1))) u |= 0x4C;
			if (!s.IsVisible(p + new CVec(-1, 0))) u |= 0x89;

			var uside = u & 0x0F;
			if (!s.IsVisible(p + new CVec(-1, -1))) u |= 0x01;
			if (!s.IsVisible(p + new CVec(1, -1))) u |= 0x02;
			if (!s.IsVisible(p + new CVec(1, 1))) u |= 0x04;
			if (!s.IsVisible(p + new CVec(-1, 1))) u |= 0x08;

			// RA provides a set of frames for tiles with shrouded
			// corners but unshrouded edges. We want to detect this
			// situation without breaking the edge -> corner enabling
			// in other combinations. The XOR turns off the corner
			// bits that are enabled twice, which gives the behavior
			// we want here.
			return useExtendedIndex ? u ^ uside : u & 0x0F;
		}

		static int ShroudedEdges(Shroud s, CPos p, bool useExtendedIndex)
		{
			if (!s.IsExplored(p))
				return 15;

			// If a side is shrouded then we also count the corners
			var u = 0;
			if (!s.IsExplored(p + new CVec(0, -1))) u |= 0x13;
			if (!s.IsExplored(p + new CVec(1, 0))) u |= 0x26;
			if (!s.IsExplored(p + new CVec(0, 1))) u |= 0x4C;
			if (!s.IsExplored(p + new CVec(-1, 0))) u |= 0x89;

			var uside = u & 0x0F;
			if (!s.IsExplored(p + new CVec(-1, -1))) u |= 0x01;
			if (!s.IsExplored(p + new CVec(1, -1))) u |= 0x02;
			if (!s.IsExplored(p + new CVec(1, 1))) u |= 0x04;
			if (!s.IsExplored(p + new CVec(-1, 1))) u |= 0x08;

			// RA provides a set of frames for tiles with shrouded
			// corners but unshrouded edges. We want to detect this
			// situation without breaking the edge -> corner enabling
			// in other combinations. The XOR turns off the corner
			// bits that are enabled twice, which gives the behavior
			// we want here.
			return useExtendedIndex ? u ^ uside : u & 0x0F;
		}

		static int ObserverShroudedEdges(CPos p, Rectangle bounds, bool useExtendedIndex)
		{
			var u = 0;
			if (p.Y == bounds.Top) u |= 0x13;
			if (p.X == bounds.Right - 1) u |= 0x26;
			if (p.Y == bounds.Bottom - 1) u |= 0x4C;
			if (p.X == bounds.Left)	u |= 0x89;

			var uside = u & 0x0F;
			if (p.X == bounds.Left && p.Y == bounds.Top) u |= 0x01;
			if (p.X == bounds.Right - 1 && p.Y == bounds.Top) u |= 0x02;
			if (p.X == bounds.Right - 1 && p.Y == bounds.Bottom - 1) u |= 0x04;
			if (p.X == bounds.Left && p.Y == bounds.Bottom - 1) u |= 0x08;

			return useExtendedIndex ? u ^ uside : u & 0x0F;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			// Initialize tile cache
			foreach (var cell in map.Cells)
			{
				var screen = wr.ScreenPosition(cell.CenterPosition);
				var variant = Game.CosmeticRandom.Next(info.Variants.Length);
				tiles[cell] = new ShroudTile(cell, screen, variant);
			}

			fogPalette = wr.Palette(info.FogPalette);
			shroudPalette = wr.Palette(info.ShroudPalette);
		}

		Sprite GetTile(int flags, int variant)
		{
			if (flags == 0)
				return null;

			if (flags == 15)
				return unexploredTile;

			return sprites[variant * variantStride + spriteMap[flags]];
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
					var shrouded = ObserverShroudedEdges(t.Position, map.Bounds, info.UseExtendedIndex);

					t.Shroud = GetTile(shrouded, t.Variant);
					t.Fog = GetTile(shrouded, t.Variant);
				}
			}
			else
			{
				foreach (var cell in map.Cells)
				{
					var t = tiles[cell];
					var shrouded = ShroudedEdges(shroud, t.Position, info.UseExtendedIndex);
					var fogged = FoggedEdges(shroud, t.Position, info.UseExtendedIndex);

					t.Shroud = GetTile(shrouded, t.Variant);
					t.Fog = GetTile(fogged, t.Variant);
				}
			}
		}

		public void RenderShroud(WorldRenderer wr, Shroud shroud)
		{
			Update(shroud);

			foreach (var cell in wr.Viewport.VisibleCells)
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
