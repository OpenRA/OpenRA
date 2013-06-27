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

using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Orders;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Mods.RA.Move;
using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA.Mods.RA
{
	// Identify untraversable regions of the map for faster pathfinding, especially with AI
	class DomainIndexInfo : TraitInfo<DomainIndex> {}

	public class DomainIndex : IWorldLoaded
	{
		Dictionary<uint, MovementClassDomainIndex> domains;

		public void WorldLoaded(World world)
		{
			domains = new Dictionary<uint, MovementClassDomainIndex>();
			var movementClasses = new HashSet<uint>(
				Rules.Info.Where(ai => ai.Value.Traits.Contains<MobileInfo>())
				.Select(ai => (uint)ai.Value.Traits.Get<MobileInfo>().GetMovementClass(world.TileSet)));

			foreach(var mc in movementClasses) domains[mc] = new MovementClassDomainIndex(world, mc);
		}

		public bool IsPassable(CPos p1, CPos p2, uint movementClass)
		{
			return domains[movementClass].IsPassable(p1, p2);
		}
	}

	class MovementClassDomainIndex
	{
		Rectangle bounds;
		uint movementClass;
		int[,] domains;

		public MovementClassDomainIndex(World world, uint movementClass)
		{
			this.movementClass = movementClass;
			bounds = world.Map.Bounds;
			domains = new int[(bounds.Width + bounds.X), (bounds.Height + bounds.Y)];
			BuildDomains(world);
		}

		public int GetDomainOf(CPos p)
		{
			return domains[p.X, p.Y];
		}

		public bool IsPassable(CPos p1, CPos p2)
		{
			return domains[p1.X, p1.Y] == domains[p2.X, p2.Y];
		}

		public void SetDomain(CPos p, int domain)
		{
			domains[p.X, p.Y] = domain;
		}


		void BuildDomains(World world)
		{
			Map map = world.Map;

			int i = 1;
			var unassigned = new HashSet<CPos>();

			// Fill up our set of yet-unassigned map cells
			for (int x = map.Bounds.Left; x < bounds.Right; x += 1)
			{
				for (int y = bounds.Top; y < bounds.Bottom; y += 1)
				{
					unassigned.Add(new CPos(x, y));
				}
			}

			while (unassigned.Count != 0)
			{
				var start = unassigned.First();
				unassigned.Remove(start);

				// Wander around looking for water transitions
				string currentTileType = WorldUtils.GetTerrainType(world, start);
				int terrainOffset = world.TileSet.Terrain.OrderBy(t => t.Key).ToList().FindIndex(x => x.Key == currentTileType);
				bool currentPassable = (movementClass & (1 << terrainOffset)) > 0;

				var toProcess = new Queue<CPos>();
				var seen = new HashSet<CPos>();
				toProcess.Enqueue(start);

				do
				{
					CPos p = toProcess.Dequeue();
					if (seen.Contains(p)) continue;
					seen.Add(p);

					string candidateTileType = WorldUtils.GetTerrainType(world, p);
					int candidateTerrainOffset = world.TileSet.Terrain.OrderBy(t => t.Key).ToList().FindIndex(x => x.Key == candidateTileType);
					bool candidatePassable = (movementClass & (1 << candidateTerrainOffset)) > 0;

					// Check if we're still in one contiguous domain
					if (currentPassable == candidatePassable)
					{
						SetDomain(p, i);
						unassigned.Remove(p);

						// Visit our neighbors, if we haven't already
						foreach (var d in CVec.directions)
						{
							CPos nextPos = p + d;
							if (nextPos.X >= map.Bounds.Left && nextPos.Y >= map.Bounds.Top &&
								nextPos.X < map.Bounds.Right && nextPos.Y < map.Bounds.Bottom)
							{
								if (!seen.Contains(nextPos)) toProcess.Enqueue(nextPos);
							}
						}
					}
				} while (toProcess.Count != 0);

				i += 1;
			}

			Log.Write("debug", "{0}: Found {1} domains", map.Title, i-1);
		}
	}
}
