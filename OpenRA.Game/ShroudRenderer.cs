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
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA
{
	public class ShroudRenderer
	{
		Traits.Shroud shroud;
		Sprite[] shadowBits = SpriteSheetBuilder.LoadAllSprites("shadow");
		Sprite[,] sprites, fogSprites;
		
		bool dirty = true;
		bool disabled = false;
		Map map;

		public Rectangle? Bounds { get { return shroud.exploredBounds; } }

		public ShroudRenderer(Player owner, Map map)
		{
			this.shroud = owner.World.WorldActor.Trait<Traits.Shroud>();
			this.map = map;
			
			sprites = new Sprite[map.MapSize.X, map.MapSize.Y];
			fogSprites = new Sprite[map.MapSize.X, map.MapSize.Y];

			shroud.Dirty += () => dirty = true;
		}

		public bool Disabled
		{
			get { return disabled; }
			set { disabled = value; dirty = true;}
		}

		public bool IsExplored(int2 xy) { return IsExplored(xy.X, xy.Y); }
		public bool IsExplored(int x, int y)
		{
			if (disabled)
				return true;
			return shroud.exploredCells[x,y];
		}

		public bool IsVisible(int2 xy) { return IsVisible(xy.X, xy.Y); }
		public bool IsVisible(int x, int y)
		{
			if (disabled)
				return true;
			return shroud.visibleCells[x,y] != 0;
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
			if( !shroud.exploredCells[ i, j ] ) return shadowBits[ 0xf ];

			// bits are for unexploredness: up, right, down, left
			var v = 0;
			// bits are for unexploredness: TL, TR, BR, BL
			var u = 0;

			if( !shroud.exploredCells[ i, j - 1 ] ) { v |= 1; u |= 3; }
			if( !shroud.exploredCells[ i + 1, j ] ) { v |= 2; u |= 6; }
			if( !shroud.exploredCells[ i, j + 1 ] ) { v |= 4; u |= 12; }
			if( !shroud.exploredCells[ i - 1, j ] ) { v |= 8; u |= 9; }

			var uSides = u;

			if( !shroud.exploredCells[ i - 1, j - 1 ] ) u |= 1;
			if( !shroud.exploredCells[ i + 1, j - 1 ] ) u |= 2;
			if( !shroud.exploredCells[ i + 1, j + 1 ] ) u |= 4;
			if( !shroud.exploredCells[ i - 1, j + 1 ] ) u |= 8;

			return shadowBits[ SpecialShroudTiles[ u ^ uSides ][ v ] ];
		}

		Sprite ChooseFog(int i, int j)
		{
			if (shroud.visibleCells[i, j] == 0) return shadowBits[0xf];
			if (!shroud.exploredCells[i, j]) return shadowBits[0xf];

			// bits are for unexploredness: up, right, down, left
			var v = 0;
			// bits are for unexploredness: TL, TR, BR, BL
			var u = 0;

			if (shroud.visibleCells[i, j - 1] == 0) { v |= 1; u |= 3; }
			if (shroud.visibleCells[i + 1, j] == 0) { v |= 2; u |= 6; }
			if (shroud.visibleCells[i, j + 1] == 0) { v |= 4; u |= 12; }
			if (shroud.visibleCells[i - 1, j] == 0) { v |= 8; u |= 9; }

			var uSides = u;

			if (shroud.visibleCells[i - 1, j - 1] == 0) u |= 1;
			if (shroud.visibleCells[i + 1, j - 1] == 0) u |= 2;
			if (shroud.visibleCells[i + 1, j + 1] == 0) u |= 4;
			if (shroud.visibleCells[i - 1, j + 1] == 0) u |= 8;

			return shadowBits[SpecialShroudTiles[u ^ uSides][v]];
		}

		internal void Draw()
		{
			if (disabled)
				return;
			
			if (dirty)
			{
				dirty = false;
				for (int i = map.TopLeft.X; i < map.BottomRight.X; i++)
					for (int j = map.TopLeft.Y; j < map.BottomRight.Y; j++)
						sprites[i, j] = ChooseShroud(i, j);
				for (int i = map.TopLeft.X; i < map.BottomRight.X; i++)
					for (int j = map.TopLeft.Y; j < map.BottomRight.Y; j++)
						fogSprites[i, j] = ChooseFog(i, j);
			}

			var clipRect = Bounds.HasValue ? Rectangle.Intersect(Bounds.Value, map.Bounds) : map.Bounds;
			clipRect = Rectangle.Intersect(Game.viewport.ViewBounds(), clipRect);
			var miny = clipRect.Top;
			var maxy = clipRect.Bottom;
			var minx = clipRect.Left;
			var maxx = clipRect.Right;

			DrawShroud( minx, miny, maxx, maxy, fogSprites, "fog" );
			DrawShroud( minx, miny, maxx, maxy, sprites, "shroud" );
		}

		void DrawShroud( int minx, int miny, int maxx, int maxy, Sprite[,] s, string pal )
		{
			var shroudPalette = Game.world.WorldRenderer.GetPaletteIndex(pal);

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
