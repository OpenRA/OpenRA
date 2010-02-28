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
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	class ResourceLayerInfo : ITraitInfo
	{
		public readonly string[] SpriteNames = { };
		public readonly int[] OverlayIndices = { };
		public readonly string Palette = "terrain";
		public object Create(Actor self) { return new ResourceLayer(self, this); }
	}

	class ResourceLayer : IRenderOverlay, ILoadWorldHook
	{
		ResourceLayerInfo info;
		Sprite[][] sprites;
		CellContents[,] content = new CellContents[128,128];
		SpriteRenderer sr;

		public ResourceLayer(Actor self, ResourceLayerInfo info)
		{
			this.info = info;
			sprites = info.SpriteNames.Select( f => SpriteSheetBuilder.LoadAllSprites(f)).ToArray();
			sr = new SpriteRenderer( Game.renderer, true );
		}

		public void Render()
		{
			var shroud = Game.world.LocalPlayer.Shroud;
			var map = Game.world.Map;

			for (int y = map.YOffset; y < map.YOffset + map.Height; y++)
				for (int x = map.XOffset; x < map.XOffset + map.Width; x++)
				{
					if (!shroud.IsExplored(new int2(x, y))) continue;
					if (content[x, y].contents != null)
						sr.DrawSprite(content[x, y].contents[content[x, y].density],
							Game.CellSize * new int2(x, y),
							info.Palette);
				}

			sr.Flush();
		}

		public void WorldLoaded(World w)
		{
			var map = w.Map;

			for (int y = map.YOffset; y < map.YOffset + map.Height; y++)
				for (int x = map.XOffset; x < map.XOffset + map.Width; x++)
					if (info.OverlayIndices.Contains(w.Map.MapTiles[x, y].overlay))
						content[x, y].contents = ChooseContent(w, w.Map.MapTiles[x, y].overlay);
		}

		Sprite[] ChooseContent(World w, int overlay)
		{
			return sprites[w.SharedRandom.Next(sprites.Length)];
		}

		public struct CellContents
		{
			public Sprite[] contents;
			public int density;
		}
	}
}
