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
			if (info.ShroudVariants.Length != info.FogVariants.Length)
				throw new InvalidOperationException("ShroudRenderer must define the same number of shroud and fog variants!");

			if ((info.OverrideFullFog == null) ^ (info.OverrideFullShroud == null))
				throw new InvalidOperationException("ShroudRenderer cannot define overrides for only one of shroud or fog!");

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
		}

		static int FoggedEdges(Shroud s, CPos p, bool useExtendedIndex)
		{
			if (!s.IsVisible(p))
				return useExtendedIndex ? 240 : 15;

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
				return useExtendedIndex ? 240 : 15;

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
				var screen = wr.ScreenPosition(w.Map.CenterOfCell(cell));
				var variant = Game.CosmeticRandom.Next(info.ShroudVariants.Length);
				tiles[cell] = new ShroudTile(cell, screen, variant);
			}

			fogPalette = wr.Palette(info.FogPalette);
			shroudPalette = wr.Palette(info.ShroudPalette);
		}

		Sprite GetTile(int flags, int variant)
		{
			if (flags == 0)
				return null;

			return shroudSprites[variant * variantStride + spriteMap[flags]];
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

					t.Shroud = shrouded != 0 ? shroudSprites[t.Variant * variantStride + spriteMap[shrouded]] : null;
					t.Fog = shrouded != 0 ? fogSprites[t.Variant * variantStride + spriteMap[shrouded]] : null;
				}
			}
			else
			{
				foreach (var cell in map.Cells)
				{
					var t = tiles[cell];
					var shrouded = ShroudedEdges(shroud, t.Position, info.UseExtendedIndex);
					var fogged = FoggedEdges(shroud, t.Position, info.UseExtendedIndex);

					t.Shroud = shrouded != 0 ? shroudSprites[t.Variant * variantStride + spriteMap[shrouded]] : null;
					t.Fog = fogged != 0 ? fogSprites[t.Variant * variantStride + spriteMap[fogged]] : null;
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
