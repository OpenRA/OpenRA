#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public class ShroudRenderer
	{
		World world;
		Map map;
		Sprite[] shadowBits = Game.modData.SpriteLoader.LoadAllSprites("shadow");
		Sprite[,] sprites, fogSprites;
		int shroudHash;

		bool initializePalettes = true;
		PaletteReference fogPalette, shroudPalette;

		static readonly byte[][] SpecialShroudTiles =
		{
			new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
			new byte[] { 32, 32, 25, 25, 19, 19, 20, 20 },
			new byte[] { 33, 33, 33, 33, 26, 26, 26, 26, 21, 21, 21, 21, 23, 23, 23, 23 },
			new byte[] { 36, 36, 36, 36, 30, 30, 30, 30 },
			new byte[] { 34, 16, 34, 16, 34, 16, 34, 16, 27, 22, 27, 22, 27, 22, 27, 22 },
			new byte[] { 44 },
			new byte[] { 37, 37, 37, 37, 37, 37, 37, 37, 31, 31, 31, 31, 31, 31, 31, 31 },
			new byte[] { 40 },
			new byte[] { 35, 24, 17, 18 },
			new byte[] { 39, 39, 29, 29 },
			new byte[] { 45 },
			new byte[] { 43 },
			new byte[] { 38, 28 },
			new byte[] { 42 },
			new byte[] { 41 },
			new byte[] { 46 },
		};

		public ShroudRenderer(World world)
		{
			this.world = world;
			this.map = world.Map;

			sprites = new Sprite[map.MapSize.X, map.MapSize.Y];
			fogSprites = new Sprite[map.MapSize.X, map.MapSize.Y];

			// Force update on first render
			shroudHash = -1;
		}

		Sprite ChooseShroud(Shroud s, int i, int j)
		{
			if (!s.IsExplored(i, j))
				return shadowBits[0xf];

			// bits are for unexploredness: up, right, down, left
			var v = 0;
			// bits are for unexploredness: TL, TR, BR, BL
			var u = 0;

			if (!s.IsExplored(i, j - 1)) { v |= 1; u |= 3; }
			if (!s.IsExplored(i + 1, j)) { v |= 2; u |= 6; }
			if (!s.IsExplored(i, j + 1)) { v |= 4; u |= 12; }
			if (!s.IsExplored(i - 1, j)) { v |= 8; u |= 9; }

			var uSides = u;
			if (!s.IsExplored(i - 1, j - 1)) u |= 1;
			if (!s.IsExplored(i + 1, j - 1)) u |= 2;
			if (!s.IsExplored(i + 1, j + 1)) u |= 4;
			if (!s.IsExplored(i - 1, j + 1)) u |= 8;

			return shadowBits[SpecialShroudTiles[u ^ uSides][v]];
		}

		Sprite ChooseFog(Shroud s, int i, int j)
		{
			if (!s.IsVisible(i, j)) return shadowBits[0xf];
			if (!s.IsExplored(i, j)) return shadowBits[0xf];

			// bits are for unexploredness: up, right, down, left
			var v = 0;
			// bits are for unexploredness: TL, TR, BR, BL
			var u = 0;

			if (!s.IsVisible(i, j - 1)) { v |= 1; u |= 3; }
			if (!s.IsVisible(i + 1, j)) { v |= 2; u |= 6; }
			if (!s.IsVisible(i, j + 1)) { v |= 4; u |= 12; }
			if (!s.IsVisible(i - 1, j)) { v |= 8; u |= 9; }

			var uSides = u;

			if (!s.IsVisible(i - 1, j - 1)) u |= 1;
			if (!s.IsVisible(i + 1, j - 1)) u |= 2;
			if (!s.IsVisible(i + 1, j + 1)) u |= 4;
			if (!s.IsVisible(i - 1, j + 1)) u |= 8;

			return shadowBits[SpecialShroudTiles[u ^ uSides][v]];
		}

		void GenerateSprites(Shroud shroud)
		{
			var hash = shroud != null ? shroud.Hash : 0;
			if (shroudHash == hash)
				return;

			shroudHash = hash;
			if (shroud == null)
			{
				// Players with no shroud see the whole map so we only need to set the edges
				var b = map.Bounds;
				for (int i = b.Left; i < b.Right; i++)
					for (int j = b.Top; j < b.Bottom; j++)
				{
					var v = 0;
					var u = 0;

					if (j == b.Top) { v |= 1; u |= 3; }
					if (i == b.Right - 1) { v |= 2; u |= 6; }
					if (j == b.Bottom - 1) { v |= 4; u |= 12; }
					if (i == b.Left) { v |= 8; u |= 9; }

					var uSides = u;
					if (i == b.Left && j == b.Top) u |= 1;
					if (i == b.Right - 1 && j == b.Top) u |= 2;
					if (i == b.Right - 1 && j == b.Bottom - 1) u |= 4;
					if (i == b.Left && j == b.Bottom - 1) u |= 8;

					sprites[i, j] = fogSprites[i, j] = shadowBits[SpecialShroudTiles[u ^ uSides][v]];
				}
			}
			else
			{
				for (int i = map.Bounds.Left; i < map.Bounds.Right; i++)
					for (int j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
						sprites[i, j] = ChooseShroud(shroud, i, j);

				for (int i = map.Bounds.Left; i < map.Bounds.Right; i++)
					for (int j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
						fogSprites[i, j] = ChooseFog(shroud, i, j);
			}
		}

		internal void Draw(WorldRenderer wr, Shroud shroud)
		{
			if (initializePalettes)
			{
				if (world.LobbyInfo.GlobalSettings.Fog)
					fogPalette = wr.Palette("fog");

				shroudPalette = world.LobbyInfo.GlobalSettings.Fog ? wr.Palette("shroud") : wr.Palette("shroudfog");
				initializePalettes = false;
			}

			GenerateSprites(shroud);

			var clipRect = Game.viewport.WorldBounds(wr.world);

			// We draw the shroud when disabled to hide the sharp map edges
			DrawShroud(wr, clipRect, sprites, shroudPalette);

			if (world.LobbyInfo.GlobalSettings.Fog)
				DrawShroud(wr, clipRect, fogSprites, fogPalette);
		}

		void DrawShroud(WorldRenderer wr, Rectangle clip, Sprite[,] s, PaletteReference pal)
		{
			for (var j = clip.Top; j < clip.Bottom; j++)
			{
				var starti = clip.Left;
				var last = shadowBits[0x0f];
				for (var i = clip.Left; i < clip.Right; i++)
				{
					if ((s[i, j] == shadowBits[0x0f] && last == shadowBits[0x0f])
						|| (s[i, j] == shadowBits[0] && last == shadowBits[0]))
						continue;

					if (starti != i)
					{
						// Stretch a solid black sprite over the rows above
						// TODO: This doesn't make sense for isometric terrain
						Game.Renderer.WorldSpriteRenderer.DrawSprite(
							s[starti, j],
							Game.CellSize * new float2(starti, j),
							pal,
							new float2(Game.CellSize * (i - starti), Game.CellSize));
						starti = i + 1;
					}

					Game.Renderer.WorldSpriteRenderer.DrawSprite(s[i, j], Game.CellSize * new float2(i, j), pal);
					starti = i + 1;
					last = s[i, j];
				}

				// Stretch a solid black sprite over the rows to the left
				// TODO: This doesn't make sense for isometric terrain
				if (starti < clip.Right)
					Game.Renderer.WorldSpriteRenderer.DrawSprite(s[starti, j],
						Game.CellSize * new float2(starti, j), pal,
						new float2(Game.CellSize * (clip.Right - starti), Game.CellSize));
			}
		}
	}
}
