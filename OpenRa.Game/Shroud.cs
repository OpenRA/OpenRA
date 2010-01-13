using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class Shroud
	{
		bool[,] explored = new bool[128, 128];
		Sprite[] shadowBits = SpriteSheetBuilder.LoadAllSprites("shadow");
		Sprite[,] sprites = new Sprite[128, 128];
		bool dirty;
		bool hasGPS = false;
		
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
			foreach (var a in Game.world.Actors.Where(a => a.traits.Contains<GeneratesGap>()))
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
			foreach (var t in Game.FindTilesInCircle((1f / Game.CellSize * a.CenterLocation).ToInt2(), a.Info.Sight))
			{
				explored[t.X, t.Y] = true;
				gapField[t.X, t.Y] = 0;
			}
			dirty = true;
		}


		static readonly byte[] ShroudTiles = 
		{
			0xf,0xf,0xf,0xf,	
			0xf,0xf,0xf,0xf,
			0xf,0xf,0xf,0xf,	
			0xf,0xf,0xf,0xf,
			0,7,13,0,	
			14,6,12,4,
			11,3,9,1,	
			0,2,8,0,
		};

		static readonly byte[] ExtraShroudTiles = 
		{
			46, 41, 42, 38,
			43, 45, 39, 35,
			40, 37, 44, 34,
			36, 33, 32, 47,
		};
		
		Sprite ChooseShroud(int i, int j)
		{
			// bits are for exploredness: left, right, up, down, self
			var v = 0;
			if (IsExplored(i - 1, j)) v |= 1;
			if (IsExplored(i + 1, j)) v |= 2;
			if (IsExplored(i, j - 1)) v |= 4;
			if (IsExplored(i, j + 1)) v |= 8;
			if (IsExplored(i, j)) v |= 16;

			var x = ShroudTiles[v];
			if (x != 0)
				return shadowBits[x];
			
			// bits are for exploredness: TL, TR, BR, BL
			var u = 0;
			if (IsExplored(i - 1, j - 1)) u |= 1;
			if (IsExplored(i + 1, j - 1)) u |= 2;
			if (IsExplored(i + 1, j + 1)) u |= 4;
			if (IsExplored(i - 1, j + 1)) u |= 8;
			return shadowBits[ExtraShroudTiles[u]];
		}

		public void Draw(SpriteRenderer r)
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
