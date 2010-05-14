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
using OpenRA.GameRules;
using System.Drawing;

namespace OpenRA.Traits
{
	public class ResourceLayerInfo : ITraitInfo
	{
		public object Create(Actor self) { return new ResourceLayer(self); }
	}

	public class ResourceLayer: IRenderOverlay, ILoadWorldHook, ICustomTerrain
	{		
		SpriteRenderer sr;
		World world;

		public ResourceType[] resourceTypes;
		CellContents[,] content;

		public ResourceLayer(Actor self)
		{
			sr = Game.renderer.SpriteRenderer;
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
					if (world.LocalPlayer != null &&
				    		!world.LocalPlayer.Shroud.IsExplored(new int2(x, y)) &&
				    		!world.LocalPlayer.Shroud.Disabled)
							continue;

					var c = content[x, y];
					if (c.image != null)
						sr.DrawSprite(c.image[c.density],
							Game.CellSize * new int2(x, y),
							c.type.info.Palette);
				}
		}

		public void WorldLoaded(World w)
		{
			this.world = w;
			content = new CellContents[w.Map.MapSize.X, w.Map.MapSize.Y];

			resourceTypes = w.WorldActor.traits.WithInterface<ResourceType>().ToArray();
			foreach (var rt in resourceTypes)
				rt.info.Sprites = rt.info.SpriteNames.Select(a => SpriteSheetBuilder.LoadAllSprites(a)).ToArray();

			var map = w.Map;

			for (int x = map.XOffset; x < map.XOffset + map.Width; x++)
				for (int y = map.YOffset; y < map.YOffset + map.Height; y++)
				{
					content[x, y].type = resourceTypes.FirstOrDefault(
						r => r.info.ResourceType == w.Map.MapResources[x, y].type);
					if (content[x, y].type != null)
						content[x, y].image = ChooseContent(content[x, y].type);
				}

			for (int x = map.XOffset; x < map.XOffset + map.Width; x++)
				for (int y = map.YOffset; y < map.YOffset + map.Height; y++)
					if (content[x, y].type != null)
						content[x, y].density = GetIdealDensity(x, y);
		}
		
		public float GetSpeedModifier(int2 p, UnitMovementType umt)
		{
			if (content[p.X,p.Y].type == null)
				return 1.0f;
			return content[p.X,p.Y].type.GetSpeedModifier(umt);
		}
		
		public float GetCost(int2 p,UnitMovementType umt)
		{
			if (content[p.X,p.Y].type == null)
				return 1.0f;
			return content[p.X,p.Y].type.GetCost(umt);
		}
		
		Sprite[] ChooseContent(ResourceType t)
		{
			return t.info.Sprites[world.SharedRandom.Next(t.info.Sprites.Length)];
		}

		int GetAdjacentCellsWith(ResourceType t, int i, int j)
		{
			int sum = 0;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
					if (content[i+u, j+v].type == t)
						++sum;
			return sum;
		}

		int GetIdealDensity(int x, int y)
		{
			return (GetAdjacentCellsWith(content[x, y].type, x, y) *
				(content[x, y].image.Length - 1)) / 9;
		}

		public void AddResource(ResourceType t, int i, int j, int n)
		{
			if (content[i, j].type == null)
			{
				content[i, j].type = t;
				content[i, j].image = ChooseContent(t);
				content[i, j].density = -1;
			}

			if (content[i, j].type != t)
				return;

			content[i, j].density = Math.Min(
				content[i, j].image.Length - 1, 
				content[i, j].density + n);
		}

		public bool IsFull(int i, int j) { return content[i, j].density == content[i, j].image.Length - 1; }

		public ResourceType Harvest(int2 p)
		{
			var type = content[p.X,p.Y].type;
			if (type == null) return null;

			if (--content[p.X, p.Y].density < 0)
			{
				content[p.X, p.Y].type = null;
				content[p.X, p.Y].image = null;
			}
			return type;
		}

		public void Destroy(int2 p)
		{
			content[p.X, p.Y].type = null;
			content[p.X, p.Y].image = null;
			content[p.X, p.Y].density = 0;
		}

		public ResourceType GetResource(int2 p) { return content[p.X, p.Y].type; }

		public struct CellContents
		{
			public ResourceType type;
			public Sprite[] image;
			public int density;
		}
	}
}
