using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	class Shroud
	{
		bool[,] explored = new bool[128, 128];
		Sprite[] shadowBits = SpriteSheetBuilder.LoadAllSprites("shadow");
		Sprite[,] sprites = new Sprite[128, 128];
		bool dirty;

		public void Explore(Actor a)
		{
			foreach (var t in Game.FindTilesInCircle((1f/Game.CellSize * a.CenterLocation).ToInt2(), a.Info.Sight))
				explored[t.X, t.Y] = true;

			dirty = true;
		}

		Sprite ChooseShroud(int i, int j)
		{
			// bits are for exploredness: left, right, up, down, self
			var n = new[] {
				0xf,0xf,0xf,0xf,	
				0xf,0x0f,0x0f,0xf,
				0xf,0x0f,0x0f,0xf,	
				0xf,0xf,0xf,0xf,
				0,7,13,0,	
				14,6,12,4,
				11,3,9,1,	
				0,2,8,0,
			};
			
			var v = 0;
			if (explored[i-1,j]) v |= 1;
			if (explored[i+1,j]) v |= 2;
			if (explored[i,j-1]) v |= 4;
			if (explored[i,j+1]) v |= 8;
			if (explored[i, j]) v |= 16;

			var x = n[v];

			if (x == 0)
			{
				// bits are for exploredness: TL, TR, BR, BL
				var m = new[] { 
					46, 41, 42, 38,
					43, 45, 39, 35,
					40, 37, 44, 34,
					36, 33, 32, 47,
				};

				var u = 0;
				if (explored[i - 1, j - 1]) u |= 1;
				if (explored[i + 1, j - 1]) u |= 2;
				if (explored[i + 1, j + 1]) u |= 4;
				if (explored[i - 1, j + 1]) u |= 8;
				return shadowBits[m[u]];
			}

			return shadowBits[x];
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

			for (var j = 0; j < 128; j++)
				for (var i = 0; i < 128; i++)
					if (sprites[i,j] != null)
						r.DrawSprite(sprites[i, j], 
							Game.CellSize * new float2(i, j), 
							PaletteType.Shroud);
		}
	}
}
