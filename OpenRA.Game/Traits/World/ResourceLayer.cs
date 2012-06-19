#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using System.Collections.Generic;

namespace OpenRA.Traits
{
	public class ResourceLayerInfo : TraitInfo<ResourceLayer> { }

	public class ResourceLayer : IRenderOverlay, IWorldLoaded
	{
		World world;

		public ResourceType[] resourceTypes;
		CellContents[,] content;

		Dictionary<int2, Actor> claimedPoints;

		bool hasSetupPalettes;

		public void Render(WorldRenderer wr)
		{
			if (!hasSetupPalettes)
			{
				hasSetupPalettes = true;
				foreach (var rt in world.WorldActor.TraitsImplementing<ResourceType>())
					rt.info.PaletteIndex = wr.GetPaletteIndex(rt.info.Palette);
			}

			var clip = Game.viewport.WorldBounds(world);
			for (int x = clip.Left; x < clip.Right; x++)
				for (int y = clip.Top; y < clip.Bottom; y++)
				{
					if (!world.LocalShroud.IsExplored(new int2(x, y)))
						continue;

					var c = content[x, y];
					if (c.image != null)
						c.image[c.density].DrawAt(
							Game.CellSize * new int2(x, y),
							c.type.info.PaletteIndex);
				}
		}

		public void WorldLoaded(World w)
		{
			this.world = w;
			content = new CellContents[w.Map.MapSize.X, w.Map.MapSize.Y];

			// NOTE(jsd): 32 seems a sane default initial capacity for the total # of harvesters in a game. Purely a guesstimate.
			claimedPoints = new Dictionary<int2, Actor>(32);

			resourceTypes = w.WorldActor.TraitsImplementing<ResourceType>().ToArray();
			foreach (var rt in resourceTypes)
				rt.info.Sprites = rt.info.SpriteNames.Select(a => Game.modData.SpriteLoader.LoadAllSprites(a)).ToArray();

			var map = w.Map;

			for (int x = map.Bounds.Left; x < map.Bounds.Right; x++)
				for (int y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
				{
					var type = resourceTypes.FirstOrDefault(
						r => r.info.ResourceType == w.Map.MapResources.Value[x, y].type);

					if (type == null)
						continue;

					if (!AllowResourceAt(type, new int2(x, y)))
						continue;

					content[x, y].type = type;
					content[x, y].image = ChooseContent(type);
				}

			for (int x = map.Bounds.Left; x < map.Bounds.Right; x++)
				for (int y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
					if (content[x, y].type != null)
					{
						content[x, y].density = GetIdealDensity(x, y);
						w.Map.CustomTerrain[x, y] = content[x, y].type.info.TerrainType;
					}
		}

		public bool AllowResourceAt(ResourceType rt, int2 a)
		{
			if (!world.Map.IsInMap(a.X, a.Y)) return false;
			if (!rt.info.AllowedTerrainTypes.Contains(world.GetTerrainInfo(a).Type)) return false;
			if (!rt.info.AllowUnderActors && world.ActorMap.AnyUnitsAt(a)) return false;
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
					if (content[i + u, j + v].type == t)
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

			world.Map.CustomTerrain[i, j] = t.info.TerrainType;
		}

		public bool IsFull(int i, int j) { return content[i, j].density == content[i, j].image.Length - 1; }

		public bool ClaimResource(Actor claimer, int2 p)
		{
			// Has anyone else claimed this point?
			Actor claimedBy;
			if (claimedPoints.TryGetValue(p, out claimedBy))
			{
				// Same claimer:
				if (claimedBy == claimer) return true;

				// This is to prevent in-fighting amongst friendly harvesters:
				if (claimer.Owner == claimedBy.Owner) return false;
				if (claimer.Owner.Stances[claimedBy.Owner] == Stance.Ally) return false;

				// If an enemy/neutral claimed this, don't respect that claim and fall through:
			}

			// Either nobody else claims this point or an enemy/neutral claims it:
			UnclaimAllResourcesBy(claimer);
			claimedPoints[p] = claimer;
			return true;
		}

		public void UnclaimAllResourcesBy(Actor claimer)
		{
			List<int2> pointsToRemove = new List<int2>(1);

			foreach (var pair in claimedPoints)
			{
				if (pair.Value == claimer)
					pointsToRemove.Add(pair.Key);
			}

			foreach (var point in pointsToRemove)
				claimedPoints.Remove(point);
		}

		public bool IsClaimedByAnyoneElse(Actor self, int2 p)
		{
			Actor claimedBy;
			if (claimedPoints.TryGetValue(p, out claimedBy))
			{
				// Same claimer:
				if (claimedBy == self) return false;

				// This is to prevent in-fighting amongst friendly harvesters:
				if (self.Owner == claimedBy.Owner) return true;
				if (self.Owner.Stances[claimedBy.Owner] == Stance.Ally) return true;

				// If an enemy/neutral claimed this, don't respect that claim and fall through:
			}

			return false;
		}

		public ResourceType Harvest(int2 p)
		{
			var type = content[p.X, p.Y].type;
			if (type == null) return null;

			if (--content[p.X, p.Y].density < 0)
			{
				content[p.X, p.Y].type = null;
				content[p.X, p.Y].image = null;
				world.Map.CustomTerrain[p.X, p.Y] = null;
			}
			return type;
		}

		public void Destroy(int2 p)
		{
			// Don't break other users of CustomTerrain if there are no resources
			if (content[p.X, p.Y].type == null)
				return;

			content[p.X, p.Y].type = null;
			content[p.X, p.Y].image = null;
			content[p.X, p.Y].density = 0;
			world.Map.CustomTerrain[p.X, p.Y] = null;
		}

		public ResourceType GetResource(int2 p) { return content[p.X, p.Y].type; }
		public int GetResourceDensity(int2 p) { return content[p.X, p.Y].density; }

		public struct CellContents
		{
			public ResourceType type;
			public Sprite[] image;
			public int density;
		}
	}
}
