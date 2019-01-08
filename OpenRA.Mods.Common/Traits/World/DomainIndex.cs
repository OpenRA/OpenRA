#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
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
		TileSet tileSet;
		Dictionary<uint, MovementClassDomainIndex> domainIndexes;

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			domainIndexes = new Dictionary<uint, MovementClassDomainIndex>();
			tileSet = world.Map.Rules.TileSet;
			var locomotors = world.WorldActor.TraitsImplementing<Locomotor>().Where(l => !string.IsNullOrEmpty(l.Info.Name));
			var movementClasses = locomotors.Select(t => (uint)t.Info.GetMovementClass(tileSet)).Distinct();

			foreach (var mc in movementClasses)
				domainIndexes[mc] = new MovementClassDomainIndex(world, mc);
		}

		public bool IsPassable(CPos p1, CPos p2, LocomotorInfo li)
		{
			// HACK: Work around units in other movement layers from being blocked
			// when the point in the main layer is not pathable
			if (p1.Layer != 0 || p2.Layer != 0)
				return true;

			if (li.DisableDomainPassabilityCheck)
				return true;

			var movementClass = li.GetMovementClass(tileSet);
			return domainIndexes[movementClass].IsPassable(p1, p2);
		}

		/// Regenerate the domain index for a group of cells
		public void UpdateCells(World world, IEnumerable<CPos> cells)
		{
			var dirty = cells.ToHashSet();
			foreach (var index in domainIndexes)
				index.Value.UpdateCells(world, dirty);
		}

		public void AddFixedConnection(IEnumerable<CPos> cells)
		{
			foreach (var index in domainIndexes)
				index.Value.AddFixedConnection(cells);
		}
	}

	class MovementClassDomainIndex
	{
		readonly Map map;
		readonly uint movementClass;
		readonly CellLayer<ushort> domains;
		readonly Dictionary<ushort, HashSet<ushort>> transientConnections;

		public MovementClassDomainIndex(World world, uint movementClass)
		{
			map = world.Map;
			this.movementClass = movementClass;
			domains = new CellLayer<ushort>(world.Map);
			transientConnections = new Dictionary<ushort, HashSet<ushort>>();

			using (new PerfTimer("BuildDomains: {0} for movement class {1}".F(world.Map.Title, movementClass)))
				BuildDomains(world);
		}

		public bool IsPassable(CPos p1, CPos p2)
		{
			if (!domains.Contains(p1) || !domains.Contains(p2))
				return false;

			if (domains[p1] == domains[p2])
				return true;

			// Even though p1 and p2 are in different domains, it's possible
			// that some dynamic terrain (i.e. bridges) may connect them.
			return HasConnection(domains[p1], domains[p2]);
		}

		public void UpdateCells(World world, HashSet<CPos> dirtyCells)
		{
			var neighborDomains = new List<ushort>();

			foreach (var cell in dirtyCells)
			{
				// Select all neighbors inside the map boundaries
				var thisCell = cell; // benign closure hazard
				var neighbors = CVec.Directions.Select(d => d + thisCell)
					.Where(c => map.Contains(c));

				var found = false;
				foreach (var n in neighbors)
				{
					if (!dirtyCells.Contains(n))
					{
						var neighborDomain = domains[n];
						if (CanTraverseTile(world, n))
						{
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
			}

			foreach (var c1 in neighborDomains)
				foreach (var c2 in neighborDomains)
					CreateConnection(c1, c2);
		}

		public void AddFixedConnection(IEnumerable<CPos> cells)
		{
			// HACK: this is a temporary workaround to add a permanent connection between the domains of the listed cells.
			// This is sufficient for fixed point-to-point tunnels, but not for dynamically updating custom layers
			// such as destroyable elevated bridges.
			// To support those the domain index will need to learn about custom movement layers, but that then requires
			// a complete refactor of the domain code to deal with MobileInfo or better a shared pathfinder class type.
			var cellDomains = cells.Select(c => domains[c]).ToHashSet();
			foreach (var c1 in cellDomains)
				foreach (var c2 in cellDomains.Where(c => c != c1))
					CreateConnection(c1, c2);
		}

		bool HasConnection(ushort d1, ushort d2)
		{
			// Search our connections graph for a possible route
			var visited = new HashSet<ushort>();
			var toProcess = new Stack<ushort>();
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

		void CreateConnection(ushort d1, ushort d2)
		{
			if (!transientConnections.ContainsKey(d1))
				transientConnections[d1] = new HashSet<ushort>();
			if (!transientConnections.ContainsKey(d2))
				transientConnections[d2] = new HashSet<ushort>();

			transientConnections[d1].Add(d2);
			transientConnections[d2].Add(d1);
		}

		bool CanTraverseTile(World world, CPos p)
		{
			if (!map.Contains(p))
				return false;

			var terrainOffset = world.Map.GetTerrainIndex(p);
			return (movementClass & (1 << terrainOffset)) > 0;
		}

		void BuildDomains(World world)
		{
			ushort domain = 1;

			var visited = new CellLayer<bool>(map);

			var toProcess = new Queue<CPos>();
			toProcess.Enqueue(MPos.Zero.ToCPos(map));

			// Flood-fill over each domain.
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
				// any non-contiguous cells for future domains.
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

					// PERF: Avoid LINQ.
					foreach (var direction in CVec.Directions)
					{
						// Don't crawl off the map, or add already-visited cells.
						var neighbor = direction + n;
						if (visited.Contains(neighbor) && !visited[neighbor])
							domainQueue.Enqueue(neighbor);
					}
				}

				domain += 1;
			}

			Log.Write("debug", "Found {0} domains for movement class {1} on map {2}.", domain - 1, movementClass, map.Title);
		}
	}
}
