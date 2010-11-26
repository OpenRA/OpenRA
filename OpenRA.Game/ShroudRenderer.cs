#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA
{
	public class ShroudRenderer
	{
		Traits.Shroud shroud;
		Sprite[] shadowBits = SpriteSheetBuilder.LoadAllSprites("shadow");
		Sprite[,] sprites, fogSprites;
		
		bool dirty = true;
		Map map;

		public ShroudRenderer(World world)
		{
			this.shroud = world.LocalShroud;
			this.map = world.Map;
			
			sprites = new Sprite[map.MapSize.X, map.MapSize.Y];
			fogSprites = new Sprite[map.MapSize.X, map.MapSize.Y];
			shroud.Dirty += () => dirty = true;
		}

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
				
		Sprite ChooseShroud(int i, int j)
		{
			if( !shroud.IsExplored( i, j ) ) return shadowBits[ 0xf ];

			// bits are for unexploredness: up, right, down, left
			var v = 0;
			// bits are for unexploredness: TL, TR, BR, BL
			var u = 0;

			if( !shroud.IsExplored( i, j - 1 ) ) { v |= 1; u |= 3; }
			if( !shroud.IsExplored( i + 1, j ) ) { v |= 2; u |= 6; }
			if( !shroud.IsExplored( i, j + 1 ) ) { v |= 4; u |= 12; }
			if( !shroud.IsExplored( i - 1, j ) ) { v |= 8; u |= 9; }

			var uSides = u;

			if( !shroud.IsExplored( i - 1, j - 1 ) ) u |= 1;
			if( !shroud.IsExplored( i + 1, j - 1 ) ) u |= 2;
			if( !shroud.IsExplored( i + 1, j + 1 ) ) u |= 4;
			if( !shroud.IsExplored( i - 1, j + 1 ) ) u |= 8;

			return shadowBits[ SpecialShroudTiles[ u ^ uSides ][ v ] ];
		}
				
		Sprite ChooseFog(int i, int j)
		{
			if (!shroud.IsVisible(i,j)) return shadowBits[0xf];
			if (!shroud.IsExplored(i, j)) return shadowBits[0xf];

			// bits are for unexploredness: up, right, down, left
			var v = 0;
			// bits are for unexploredness: TL, TR, BR, BL
			var u = 0;

			if (!shroud.IsVisible(i, j - 1)) { v |= 1; u |= 3; }
			if (!shroud.IsVisible(i + 1, j)) { v |= 2; u |= 6; }
			if (!shroud.IsVisible(i, j + 1)) { v |= 4; u |= 12; }
			if (!shroud.IsVisible(i - 1, j)) { v |= 8; u |= 9; }

			var uSides = u;

			if (!shroud.IsVisible(i - 1, j - 1)) u |= 1;
			if (!shroud.IsVisible(i + 1, j - 1)) u |= 2;
			if (!shroud.IsVisible(i + 1, j + 1)) u |= 4;
			if (!shroud.IsVisible(i - 1, j + 1)) u |= 8;

			return shadowBits[SpecialShroudTiles[u ^ uSides][v]];
		}

		internal void Draw( WorldRenderer wr )
		{			
			if (dirty)
			{
				dirty = false;
				for (int i = map.Bounds.Left; i < map.Bounds.Right; i++)
					for (int j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
						sprites[i, j] = ChooseShroud(i, j);
				
				for (int i = map.Bounds.Left; i < map.Bounds.Right; i++)
					for (int j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
						fogSprites[i, j] = ChooseFog(i, j);
			}

			var clipRect = (shroud != null && shroud.Bounds.HasValue) ? Rectangle.Intersect(shroud.Bounds.Value, map.Bounds) : map.Bounds;
			clipRect = Rectangle.Intersect(Game.viewport.ViewBounds(), clipRect);
			var miny = clipRect.Top;
			var maxy = clipRect.Bottom;
			var minx = clipRect.Left;
			var maxx = clipRect.Right;

			DrawShroud( wr, minx, miny, maxx, maxy, fogSprites, "fog" );
			DrawShroud( wr, minx, miny, maxx, maxy, sprites, "shroud" );
		}

		void DrawShroud( WorldRenderer wr, int minx, int miny, int maxx, int maxy, Sprite[,] s, string pal )
		{
			var shroudPalette = wr.GetPaletteIndex(pal);

			for (var j = miny; j < maxy; j++)
			{
				var starti = minx;
				for (var i = minx; i < maxx; i++)
				{
					if (s[i, j] == shadowBits[0x0f])
						continue;

					if (starti != i)
					{
						s[starti, j].DrawAt(
							Game.CellSize * new float2(starti, j),
							shroudPalette,
							new float2(Game.CellSize * (i - starti), Game.CellSize));
						starti = i + 1;
					}

					s[i, j].DrawAt(
						Game.CellSize * new float2(i, j),
						shroudPalette);
					starti = i + 1;
				}

				if (starti < maxx)
					s[starti, j].DrawAt(
						Game.CellSize * new float2(starti, j),
						shroudPalette,
						new float2(Game.CellSize * (maxx - starti), Game.CellSize));
			}
		}
	}
}
