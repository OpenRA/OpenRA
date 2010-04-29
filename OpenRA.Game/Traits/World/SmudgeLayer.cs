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

using System;
using System.Linq;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	class SmudgeLayerInfo : ITraitInfo
	{
		public readonly string Type = "Scorch";
		public readonly string[] Types = {"sc1", "sc2", "sc3", "sc4", "sc5", "sc6"};
		public readonly int[] Depths = {1,1,1,1,1,1};
		public object Create(Actor self) { return new SmudgeLayer(self, this); }
	}

	class SmudgeLayer: IRenderOverlay, ILoadWorldHook
	{		
		public SmudgeLayerInfo Info;
		SpriteRenderer spriteRenderer;
		TileReference<byte,byte>[,] tiles;
		Sprite[][] smudgeSprites;
		World world;

		public SmudgeLayer(Actor self, SmudgeLayerInfo info)
		{
			spriteRenderer = Game.renderer.SpriteRenderer;
			this.Info = info;
			smudgeSprites = Info.Types.Select(x => SpriteSheetBuilder.LoadAllSprites(x)).ToArray();
		}
		
		public void WorldLoaded(World w)
		{
			world = w;
			tiles = new TileReference<byte,byte>[w.Map.MapSize.X,w.Map.MapSize.Y];
			
			// Add map smudges
			foreach (var s in w.Map.Smudges.Where( s => Info.Types.Contains(s.Type )))
				tiles[s.Location.X,s.Location.Y] = new TileReference<byte,byte>((byte)Array.IndexOf(Info.Types,s.Type),
				                                                  (byte)s.Depth);
		}
		
		public void AddSmudge(int2 loc)
		{
			if (!Rules.TerrainTypes[world.GetTerrainType(loc)].AcceptSmudge)
				return;

			// No smudge; create a new one
			if (tiles[loc.X, loc.Y].type == 0)
			{
				byte st = (byte)(1 + world.SharedRandom.Next(Info.Types.Length - 1));
				tiles[loc.X,loc.Y] = new TileReference<byte,byte>(st,(byte)0);
				return;
			}
			
			// Existing smudge; make it deeper
			int depth = Info.Depths[tiles[loc.X, loc.Y].type-1];
			if (tiles[loc.X, loc.Y].image < depth - 1)
				tiles[loc.X,loc.Y].image++;
		}
		
		public void Render()
		{
			var cliprect = Game.viewport.ShroudBounds().HasValue 
				? Rectangle.Intersect(Game.viewport.ShroudBounds().Value, world.Map.Bounds) : world.Map.Bounds;

			var minx = cliprect.Left;
			var maxx = cliprect.Right;

			var miny = cliprect.Top;
			var maxy = cliprect.Bottom;

			for (int x = minx; x < maxx; x++)
				for (int y = miny; y < maxy; y++)
				{
					var t = new int2(x, y);
					if (world.LocalPlayer != null && !world.LocalPlayer.Shroud.IsExplored(t) || tiles[x,y].type == 0) continue;
	
					spriteRenderer.DrawSprite(smudgeSprites[tiles[x,y].type- 1][tiles[x,y].image],
						Game.CellSize * t, "terrain");
				}
		}
	}
}
