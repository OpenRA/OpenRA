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
using OpenRA.Graphics;
using System;

namespace OpenRA.Traits
{
	class ResourceLayerInfo : ITraitInfo
	{
		public object Create(Actor self) { return new ResourceLayer(self); }
	}

	class ResourceLayer : IRenderOverlay, ILoadWorldHook
	{
		SpriteRenderer sr;
		World w;

		public ResourceTypeInfo[] resourceTypes;
		public CellContents[,] content = new CellContents[128, 128];

		public ResourceLayer(Actor self)
		{
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

					var c = content[x, y];
					if (c.image != null)
						sr.DrawSprite(c.image[c.density],
							Game.CellSize * new int2(x, y),
							c.type.Palette);
				}

			sr.Flush();
		}

		public void WorldLoaded(World w)
		{
			this.w = w;
			resourceTypes = w.WorldActor.Info.Traits.WithInterface<ResourceTypeInfo>().ToArray();
			foreach (var rt in resourceTypes)
				rt.Sprites = rt.SpriteNames.Select(a => SpriteSheetBuilder.LoadAllSprites(a)).ToArray();

			var map = w.Map;

			for (int y = map.YOffset; y < map.YOffset + map.Height; y++)
				for (int x = map.XOffset; x < map.XOffset + map.Width; x++)
				{
					content[x,y].type = resourceTypes.FirstOrDefault(
						r => r.Overlays.Contains(w.Map.MapTiles[x, y].overlay));
					if (content[x, y].type != null)
						content[x, y].image = ChooseContent(content[x, y].type);
				}

			for (int y = map.YOffset; y < map.YOffset + map.Height; y++)
				for (int x = map.XOffset; x < map.XOffset + map.Width; x++)
					if (content[x, y].type != null)
						content[x, y].density = GetIdealDensity(x, y);
		}

		public Sprite[] ChooseContent(ResourceTypeInfo info)
		{
			return info.Sprites[w.SharedRandom.Next(info.Sprites.Length)];
		}

		public int GetAdjacentCellsWith(ResourceTypeInfo info, int i, int j)
		{
			int sum = 0;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
					if (content[i+u, j+v].type == info)
						++sum;
			return sum;
		}

		public int GetIdealDensity(int x, int y)
		{
			return (GetAdjacentCellsWith(content[x, y].type, x, y) *
				content[x, y].image.Length) / 9;
		}

		public void AddResource(ResourceTypeInfo info, int i, int j, int n)
		{
			if (content[i, j].type == null)
			{
				content[i, j].type = info;
				content[i, j].image = ChooseContent(info);
				content[i, j].density = -1;
			}

			if (content[i, j].type != info)
				return;

			content[i, j].density = Math.Min(
				content[i, j].image.Length - 1, 
				content[i, j].density + n);
		}

		public ResourceTypeInfo Harvest(int2 p)
		{
			var type = content[p.X,p.Y].type;
			if (type == null) return null;

			if (--content[p.X, p.Y].density < 0)
				content[p.X, p.Y].type = null;
			return type;
		}

		public void Destroy(int2 p)
		{
			content[p.X, p.Y].type = null;
			content[p.X, p.Y].image = null;
			content[p.X, p.Y].density = 0;
		}

		public void Grow(ResourceTypeInfo info)
		{
			var map = w.Map;
			var mini = map.XOffset; var maxi = map.XOffset + map.Width;
			var minj = map.YOffset; var maxj = map.YOffset + map.Height;

			var newDensity = new byte[128, 128];
			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
					if (content[i, j].type == info)
						newDensity[i, j] = (byte)GetIdealDensity(i, j);

			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
					if (content[i, j].type == info && content[i, j].density < newDensity[i, j])
						++content[i, j].density;
		}

		public void Spread(ResourceTypeInfo info, Random r, float chance)
		{
			var map = w.Map;

			var mini = map.XOffset; var maxi = map.XOffset + map.Width;
			var minj = map.YOffset; var maxj = map.YOffset + map.Height;

			var growMask = new bool[128, 128];
			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
					if (content[i,j].type == null
						&& r.NextDouble() < chance
						&& GetAdjacentCellsWith(info, i,j ) > 0
						&& w.IsCellBuildable(new int2(i, j), UnitMovementType.Wheel))
						growMask[i, j] = true;

			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
					if (growMask[i, j])
					{
						content[i, j].type = info;
						content[i, j].image = ChooseContent(info);
						content[i, j].density = 0;
					}
		}

		public struct CellContents
		{
			public ResourceTypeInfo type;
			public Sprite[] image;
			public int density;
		}
	}
}
