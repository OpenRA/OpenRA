#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	class SmudgeRenderer
	{
		static string[] smudgeSpriteNames =
			{ 
				"bib3", "bib2", "bib1", "sc1", "sc2", "sc3", "sc4", "sc5", "sc6",
				"cr1", "cr2", "cr3", "cr4", "cr5", "cr6", 
			};

		readonly Sprite[] smudgeSprites;

		SpriteRenderer spriteRenderer;
		Map map;

		public SmudgeRenderer( Renderer renderer, Map map )
		{
			this.spriteRenderer = new SpriteRenderer( renderer, true );
			this.map = map;

			smudgeSprites = smudgeSpriteNames.SelectMany(f => SpriteSheetBuilder.LoadAllSprites(f)).ToArray();
		}

		public void Draw()
		{
			var shroud = Game.world.LocalPlayer.Shroud;

			for (int y = map.YOffset; y < map.YOffset + map.Height; y++)
				for (int x = map.XOffset; x < map.XOffset + map.Width; x++)
				{
					if (!shroud.IsExplored(new int2(x,y))) continue;

					var tr = map.MapTiles[x,y];
					if (tr.smudge != 0 && tr.smudge <= smudgeSprites.Length)
					{
						var location = new int2(x, y);
						spriteRenderer.DrawSprite(smudgeSprites[tr.smudge - 1],
							Game.CellSize * (float2)location, "terrain");
					}
				}

			spriteRenderer.Flush();
		}
	}
}
