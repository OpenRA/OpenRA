#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Identify untraversable regions of the map for faster pathfinding, especially with AI.",
		"This trait is required. Every mod needs it attached to the world actor.")]
	class DomainIndexInfo : TraitInfo<DomainIndex> { }

	public class DomainIndex : IWorldLoaded
	{
		Dictionary<uint, MovementClassDomainIndex> domainIndexes;

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			domainIndexes = new Dictionary<uint, MovementClassDomainIndex>();
			var movementClasses = new HashSet<uint>(
				world.Map.Rules.Actors.Where(ai => ai.Value.Traits.Contains<MobileInfo>())
				.Select(ai => (uint)ai.Value.Traits.Get<MobileInfo>().GetMovementClass(world.TileSet)));

			foreach (var mc in movementClasses)
				domainIndexes[mc] = new MovementClassDomainIndex(world, mc);
		}

		public bool IsPassable(CPos p1, CPos p2, uint movementClass)
		{
			return domainIndexes[movementClass].IsPassable(p1, p2);
		}

		/// Regenerate the domain index for a group of cells
		public void UpdateCells(World world, IEnumerable<CPos> cells)
		{
			var dirty = new HashSet<CPos>(cells);
			foreach (var index in domainIndexes)
				index.Value.UpdateCells(world, dirty);
		}
	}

	class MovementClassDomainIndex
	{
		Map map;

		uint movementClass;
		CellLayer<int> domains;
		Dictionary<int, HashSet<int>> transientConnections;

		public MovementClassDomainIndex(World world, uint movementClass)
		{
			map = world.Map;
			this.movementClass = movementClass;
			domains = new CellLayer<int>(world.Map);
			transientConnections = new Dictionary<int, HashSet<int>>();

			using (new PerfTimer("BuildDomains: {0}".F(world.Map.Title)))
				BuildDomains(world);
		}

		public bool IsPassable(CPos p1, CPos p2)
		{
			if (!map.Contains(p1) || !map.Contains(p2))
				return false;

			if (domains[p1] == domains[p2])
				return true;

			// Even though p1 and p2 are in different domains, it's possible
			// that some dynamic terrain (i.e. bridges) may connect them.
			return HasConnection(domains[p1], domains[p2]);
		}

		public void UpdateCells(World world, HashSet<CPos> dirtyCells)
		{
			var neighborDomains = new List<int>();

			foreach (var cell in dirtyCells)
			{
				// Select all neighbors inside the map boundries
				var thisCell = cell;	// benign closure hazard
				var neighbors = CVec.Directions.Select(d => d + thisCell)
					.Where(c => map.Contains(c));

				var found = false;
				foreach (var n in neighbors)
				{
					if (!dirtyCells.Contains(n))
					{
						var neighborDomain = domains[n];
						if (CanTraverseTile(world, n))
							neighborDomains.Add(neighborDomain);

						// Set ourselves to the first non-dirty neighbor we find.
						if (!found)
						{
							domains[cell] = neighborDomain;
							found = true;
						}
					}
				}
			}

			foreach (var c1 in neighborDomains)
				foreach (var c2 in neighborDomains)
					CreateConnection(c1, c2);
		}

		bool HasConnection(int d1, int d2)
		{
			// Search our connections graph for a possible route
			var visited = new HashSet<int>();
			var toProcess = new Stack<int>();
			toProcess.Push(d1);

			while (toProcess.Any())
			{
				var current = toProcess.Pop();
				if (!transientConnections.ContainsKey(current))
					continue;

				foreach (var neighbor in transientConnections[current])
				{
					if (neighbor == d2)
						return true;

					if (!visited.Contains(neighbor))
						toProcess.Push(neighbor);
				}

				visited.Add(current);
			}

			return false;
		}

		void CreateConnection(int d1, int d2)
		{
			if (!transientConnections.ContainsKey(d1))
				transientConnections[d1] = new HashSet<int>();
			if (!transientConnections.ContainsKey(d2))
				transientConnections[d2] = new HashSet<int>();

			transientConnections[d1].Add(d2);
			transientConnections[d2].Add(d1);
		}

		bool CanTraverseTile(World world, CPos p)
		{
			var terrainOffset = world.Map.GetTerrainIndex(p);
			return (movementClass & (1 << terrainOffset)) > 0;
		}

		void BuildDomains(World world)
		{
			var map = world.Map;

			var domain = 1;

			var visited = new CellLayer<bool>(map);

			var toProcess = new Queue<CPos>();
			toProcess.Enqueue(new CPos(map.Bounds.Left, map.Bounds.Top));

			// Flood-fill over each domain
			while (toProcess.Count != 0)
			{
				var start = toProcess.Dequeue();

				// Technically redundant with the check in the inner loop, but prevents
				// ballooning the domain counter.
				if (visited[start])
					continue;

				var domainQueue = new Queue<CPos>();
				domainQueue.Enqueue(start);

				var currentPassable = CanTraverseTile(world, start);

				// Add all contiguous cells to our domain, and make a note of
				// any non-contiguous cells for future domains
				while (domainQueue.Count != 0)
				{
					var n = domainQueue.Dequeue();
					if (visited[n])
						continue;

					var candidatePassable = CanTraverseTile(world, n);
					if (candidatePassable != currentPassable)
					{
						toProcess.Enqueue(n);
						continue;
					}

					visited[n] = true;
					domains[n] = domain;

					// Don't crawl off the map, or add already-visited cells
					var neighbors = CVec.Directions.Select(d => n + d)
						.Where(p => map.Contains(p) && !visited[p]);

					foreach (var neighbor in neighbors)
						domainQueue.Enqueue(neighbor);
				}

				domain += 1;
			}

			Log.Write("debug", "Found {0} domains on map {1}.", domain - 1, map.Title);
		}
	}
}
