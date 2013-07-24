#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class ResourceLayerInfo : TraitInfo<ResourceLayer> { }

	public class ResourceLayer : IRenderOverlay, IWorldLoaded
	{
		World world;

		ResourceType[] resourceTypes;
		CellContents[,] content;
		bool hasSetupPalettes;

		public void Render(WorldRenderer wr)
		{
			if (!hasSetupPalettes)
			{
				hasSetupPalettes = true;
				foreach (var rt in world.WorldActor.TraitsImplementing<ResourceType>())
					rt.info.PaletteRef = wr.Palette(rt.info.Palette);
			}

			var clip = Game.viewport.WorldBounds(world);
			for (var x = clip.Left; x < clip.Right; x++)
				for (var y = clip.Top; y < clip.Bottom; y++)
				{
					if (world.ShroudObscures(new CPos(x, y)))
						continue;

					var c = content[x, y];
					if (c.Image != null)
						c.Image[c.Density].DrawAt(
							new CPos(x, y).ToPPos().ToFloat2(),
							c.Type.info.PaletteRef);
				}
		}

		public void WorldLoaded(World w)
		{
			this.world = w;
			content = new CellContents[w.Map.MapSize.X, w.Map.MapSize.Y];

			resourceTypes = w.WorldActor.TraitsImplementing<ResourceType>().ToArray();
			foreach (var rt in resourceTypes)
				rt.info.Sprites = rt.info.SpriteNames.Select(a => Game.modData.SpriteLoader.LoadAllSprites(a)).ToArray();

			var map = w.Map;

			for (var x = map.Bounds.Left; x < map.Bounds.Right; x++)
				for (var y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
				{
					var type = resourceTypes.FirstOrDefault(
						r => r.info.ResourceType == w.Map.MapResources.Value[x, y].type);

					if (type == null)
						continue;

					if (!AllowResourceAt(type, new CPos(x, y)))
						continue;

					content[x, y].Type = type;
					content[x, y].Image = ChooseContent(type);
				}

			for (var x = map.Bounds.Left; x < map.Bounds.Right; x++)
				for (var y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
					if (content[x, y].Type != null)
					{
						content[x, y].Density = GetIdealDensity(x, y);
						w.Map.CustomTerrain[x, y] = content[x, y].Type.info.TerrainType;
					}
		}

		public bool AllowResourceAt(ResourceType rt, CPos a)
		{
			if (!world.Map.IsInMap(a.X, a.Y))
				return false;

			if (!rt.info.AllowedTerrainTypes.Contains(world.GetTerrainInfo(a).Type))
				return false;

			if (!rt.info.AllowUnderActors && world.ActorMap.AnyUnitsAt(a))
				return false;

			return true;
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
					if (content[i + u, j + v].Type == t)
						++sum;
			return sum;
		}

		int GetIdealDensity(int x, int y)
		{
			return (GetAdjacentCellsWith(content[x, y].Type, x, y) *
				(content[x, y].Image.Length - 1)) / 9;
		}

		public void AddResource(ResourceType t, int i, int j, int n)
		{
			if (content[i, j].Type == null)
			{
				content[i, j].Type = t;
				content[i, j].Image = ChooseContent(t);
				content[i, j].Density = -1;
			}

			if (content[i, j].Type != t)
				return;

			content[i, j].Density = Math.Min(
				content[i, j].Image.Length - 1,
				content[i, j].Density + n);

			world.Map.CustomTerrain[i, j] = t.info.TerrainType;
		}

		public bool IsFull(int i, int j)
		{
			return content[i, j].Density == content[i, j].Image.Length - 1;
		}

		public ResourceType Harvest(CPos p)
		{
			var type = content[p.X, p.Y].Type;
			if (type == null)
				return null;

			if (--content[p.X, p.Y].Density < 0)
			{
				content[p.X, p.Y].Type = null;
				content[p.X, p.Y].Image = null;
				world.Map.CustomTerrain[p.X, p.Y] = null;
			}

			return type;
		}

		public void Destroy(CPos p)
		{
			// Don't break other users of CustomTerrain if there are no resources
			if (content[p.X, p.Y].Type == null)
				return;

			content[p.X, p.Y].Type = null;
			content[p.X, p.Y].Image = null;
			content[p.X, p.Y].Density = 0;
			world.Map.CustomTerrain[p.X, p.Y] = null;
		}

		public ResourceType GetResource(CPos p) { return content[p.X, p.Y].Type; }
		public int GetResourceDensity(CPos p) { return content[p.X, p.Y].Density; }
		public int GetMaxResourceDensity(CPos p)
		{
			if (content[p.X, p.Y].Image == null)
				return 0;

			return content[p.X, p.Y].Image.Length - 1;
		}

		public struct CellContents
		{
			public ResourceType Type;
			public Sprite[] Image;
			public int Density;
		}
	}
}
