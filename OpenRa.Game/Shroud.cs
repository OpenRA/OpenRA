using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa
{
	public class Shroud
	{
		bool[,] explored = new bool[128, 128];
		Sprite[] shadowBits = SpriteSheetBuilder.LoadAllSprites("shadow");
		Sprite[,] sprites = new Sprite[128, 128];
		bool dirty = true;
		bool hasGPS = false;
		Player owner;

		public Shroud(Player owner) { this.owner = owner; }
		
		float gapOpaqueTicks = (int)(Rules.General.GapRegenInterval * 25 * 60);
		int[,] gapField = new int[128, 128];
		bool[,] gapActive = new bool[128, 128];
		
		public bool HasGPS
		{
			get { return hasGPS; }
			set { hasGPS = value; dirty = true;}
		}

		public void Tick()
		{
			// Clear active flags
			gapActive = new bool[128, 128];
			foreach (var a in Game.world.Actors.Where(a => a.traits.Contains<GeneratesGap>() && owner != a.Owner))
			{
				foreach (var t in a.traits.Get<GeneratesGap>().GetShroudedTiles())
					gapActive[t.X, t.Y] = true;
			}

			for (int j = 1; j < 127; j++)
				for (int i = 1; i < 127; i++)
				{
					if (gapField[i, j] > 0 && !gapActive[i, j])
					{
						// Convert gap to shroud
						if (gapField[i, j] >= gapOpaqueTicks && explored[i, j])
							explored[i, j] = false;
						
						// Clear gap
						gapField[i, j] = 0;
						dirty = true;
					}
					// Increase gap tick; rerender if necessary
					if (gapActive[i, j] && 0 == gapField[i, j]++)
						dirty = true;
				}
		}
		
		public bool IsExplored(int2 xy) { return IsExplored(xy.X, xy.Y); }
		public bool IsExplored(int x, int y)
		{
			if (gapField[ x, y ] >= Rules.General.GapRegenInterval * 25 * 60)
				return false;
			
			if (hasGPS)
				return true;
			
			return explored[ x, y ];
		}
		
		public bool DisplayOnRadar(int x, int y)
		{
			// Active gap is never shown on radar, even if a unit is in range
			if (gapActive[x , y])
				return false;
			
			return IsExplored(x,y);
		}
		
		public void Explore(Actor a)
		{
			foreach (var t in Game.world.FindTilesInCircle(
				(1f / Game.CellSize * a.CenterLocation).ToInt2(), 
				a.Info.Traits.Get<OwnedActorInfo>().Sight))
			{
				explored[t.X, t.Y] = true;
				gapField[t.X, t.Y] = 0;
			}
			dirty = true;
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
			if( !IsExplored( i, j ) ) return shadowBits[ 0xf ];

			// bits are for unexploredness: up, right, down, left
			var v = 0;
			// bits are for unexploredness: TL, TR, BR, BL
			var u = 0;

			if( !IsExplored( i, j - 1 ) ) { v |= 1; u |= 3; }
			if( !IsExplored( i + 1, j ) ) { v |= 2; u |= 6; }
			if( !IsExplored( i, j + 1 ) ) { v |= 4; u |= 12; }
			if( !IsExplored( i - 1, j ) ) { v |= 8; u |= 9; }

			var uSides = u;

			if( !IsExplored( i - 1, j - 1 ) ) u |= 1;
			if( !IsExplored( i + 1, j - 1 ) ) u |= 2;
			if( !IsExplored( i + 1, j + 1 ) ) u |= 4;
			if( !IsExplored( i - 1, j + 1 ) ) u |= 8;

			return shadowBits[ SpecialShroudTiles[ u ^ uSides ][ v ] ];
		}

		internal void Draw(SpriteRenderer r)
		{
			if (dirty)
			{
				dirty = false;
				for (int j = 1; j < 127; j++)
					for (int i = 1; i < 127; i++)
						sprites[i, j] = ChooseShroud(i, j);
			}

			for (var j = 1; j < 127; j++)
			{
				var starti = 1;
				for (var i = 1; i < 127; i++)
				{
					if (sprites[i, j] == shadowBits[0x0f])
						continue;

					if (starti != i)
					{
						r.DrawSprite(sprites[starti,j],
						    Game.CellSize * new float2(starti, j),
						    PaletteType.Shroud,
						    new float2(Game.CellSize * (i - starti), Game.CellSize));
						starti = i+1;
					}

					r.DrawSprite(sprites[i, j],
						Game.CellSize * new float2(i, j),
						PaletteType.Shroud);
					starti = i+1;
				}

				if (starti < 127)
					r.DrawSprite(sprites[starti, j],
						Game.CellSize * new float2(starti, j),
						PaletteType.Shroud,
						new float2(Game.CellSize * (127 - starti), Game.CellSize));
			}
		}
	}
}
