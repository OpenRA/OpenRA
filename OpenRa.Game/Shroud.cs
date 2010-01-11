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
		
		public bool HasGPS
		{
			get { return hasGPS; }
			set { hasGPS = value; dirty = true;}
		}

		public bool IsExplored(int2 xy) { return IsExplored(xy.X, xy.Y); }
		public bool IsExplored(int x, int y)
		{
			if (hasGPS)
				return true;
			
			return explored[ x, y ];
		}
		
		public void Explore(Actor a)
		{
			foreach (var t in Game.FindTilesInCircle((1f / Game.CellSize * a.CenterLocation).ToInt2(), a.Info.Traits.Get<OwnedActorInfo>().Sight))
				explored[t.X, t.Y] = true;

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
