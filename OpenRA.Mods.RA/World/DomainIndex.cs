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

using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA
{
	// Identify untraversable regions of the map for faster pathfinding, especially with AI
	class DomainIndexInfo : TraitInfo<DomainIndex> {}

	public class DomainIndex : IWorldLoaded
	{
		Dictionary<uint, MovementClassDomainIndex> domainIndexes;

		public void WorldLoaded(World world)
		{
			domainIndexes = new Dictionary<uint, MovementClassDomainIndex>();
			var movementClasses = new HashSet<uint>(
				Rules.Info.Where(ai => ai.Value.Traits.Contains<MobileInfo>())
				.Select(ai => (uint)ai.Value.Traits.Get<MobileInfo>().GetMovementClass(world.TileSet)));

			foreach (var mc in movementClasses) domainIndexes[mc] = new MovementClassDomainIndex(world, mc);
		}

		public bool IsPassable(CPos p1, CPos p2, uint movementClass)
		{
			return domainIndexes[movementClass].IsPassable(p1, p2);
		}

		/// Regenerate the domain index for a group of cells
		public void UpdateCells(World world, IEnumerable<CPos> cells)
		{
			var dirty = new HashSet<CPos>(cells);
			foreach (var index in domainIndexes) index.Value.UpdateCells(world, dirty);
		}
	}

	class MovementClassDomainIndex
	{
		Rectangle bounds;

		uint movementClass;
		int[,] domains;
		Dictionary<int, HashSet<int>> transientConnections;

		public MovementClassDomainIndex(World world, uint movementClass)
		{
			bounds = world.Map.Bounds;
			this.movementClass = movementClass;
			domains = new int[(bounds.Width + bounds.X), (bounds.Height + bounds.Y)];
			transientConnections = new Dictionary<int, HashSet<int>>();

			BuildDomains(world);
		}

		public bool IsPassable(CPos p1, CPos p2)
		{
			if (domains[p1.X, p1.Y] == domains[p2.X, p2.Y]) return true;

			// Even though p1 and p2 are in different domains, it's possible
			// that some dynamic terrain (i.e. bridges) may connect them.
			return HasConnection(GetDomainOf(p1), GetDomainOf(p2));
		}

		public void UpdateCells(World world, HashSet<CPos> dirtyCells)
		{
			var neighborDomains = new List<int>();

			foreach (var cell in dirtyCells)
			{
				// Select all neighbors inside the map boundries
				var neighbors = CVec.directions.Select(d => d + cell)
					.Where(c => bounds.Contains(c.X, c.Y));

				bool found = false;
				foreach (var neighbor in neighbors)
				{
					if (!dirtyCells.Contains(neighbor))
					{
						int neighborDomain = GetDomainOf(neighbor);

						bool match = CanTraverseTile(world, neighbor);
						if (match) neighborDomains.Add(neighborDomain);

						// Set ourselves to the first non-dirty neighbor we find.
						if (!found)
						{
							SetDomain(cell, neighborDomain);
							found = true;
						}
					}
				}
			}

			foreach (var c1 in neighborDomains)
			{
				foreach (var c2 in neighborDomains)
				{
					CreateConnection(c1, c2);
				}
			}
		}

		int GetDomainOf(CPos p)
		{
			return domains[p.X, p.Y];
		}

		void SetDomain(CPos p, int domain)
		{
			domains[p.X, p.Y] = domain;
		}

		bool HasConnection(int d1, int d2)
		{
			// Search our connections graph for a possible route
			var visited = new HashSet<int>();
			var toProcess = new Stack<int>();
			toProcess.Push(d1);

			int i = 0;
			while (toProcess.Count() > 0)
			{
				int current = toProcess.Pop();
				if (!transientConnections.ContainsKey(current)) continue;
				foreach (int neighbor in transientConnections[current])
				{
					if (neighbor == d2) return true;
					if (!visited.Contains(neighbor)) toProcess.Push(neighbor);
				}

				visited.Add(current);
				i += 1;
			}

			return false;
		}

		void CreateConnection(int d1, int d2)
		{
			if (!transientConnections.ContainsKey(d1)) transientConnections[d1] = new HashSet<int>();
			if (!transientConnections.ContainsKey(d2)) transientConnections[d2] = new HashSet<int>();

			transientConnections[d1].Add(d2);
			transientConnections[d2].Add(d1);
		}

		bool CanTraverseTile(World world, CPos p)
		{
			string currentTileType = WorldUtils.GetTerrainType(world, p);
			int terrainOffset = world.TileSet.Terrain.OrderBy(t => t.Key).ToList().FindIndex(x => x.Key == currentTileType);
			return (movementClass & (1 << terrainOffset)) > 0;
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
				bool currentPassable = CanTraverseTile(world, start);

				var toProcess = new Queue<CPos>();
				var seen = new HashSet<CPos>();
				toProcess.Enqueue(start);

				do
				{
					CPos p = toProcess.Dequeue();
					if (seen.Contains(p)) continue;
					seen.Add(p);

					bool candidatePassable = CanTraverseTile(world, p);

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
