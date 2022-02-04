#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	abstract class ClusterManager
	{
		protected readonly CellLayer<ClusterContents> clusterCellLayer;

		protected readonly ClusterContents emptyCluster;
		protected readonly List<ClusterContents> clusters = new List<ClusterContents>();
		protected readonly World world;
		protected readonly Map map;

		public ClusterManager(World world)
		{
			this.world = world;
			map = world.Map;
			clusterCellLayer = new CellLayer<ClusterContents>(world.Map);
			emptyCluster = new ClusterContents().SetEmpty();
			clusters.Add(emptyCluster);
		}

		protected abstract bool ClusterCondition(CPos p);

		public bool IsConnected(CPos p1, CPos p2)
		{
			if (!clusterCellLayer.Contains(p1) || !clusterCellLayer.Contains(p2))
				return false;

			if (clusterCellLayer[p1] == clusterCellLayer[p2])
				return true;

			// Even though p1 and p2 are in different domains, it's possible
			// that some dynamic terrain (i.e. bridges) may connect them.
			return clusterCellLayer[p1].TransientConnections.Any(con => con.Active && clusterCellLayer[p2].TransientConnections.Contains(con));
		}

		public void UpdateCells(HashSet<CPos> dirtyCells)
		{
			var addedCells = dirtyCells.Where(cell => ClusterCondition(cell) && clusterCellLayer[cell].IsEmpty);
			var removedCells = dirtyCells.Where(cell => !ClusterCondition(cell) && !clusterCellLayer[cell].IsEmpty);

			foreach (var cell in addedCells)
			{
				var neighborClusters = new List<ClusterContents>();

				// Select all neighbor clusters
				var neighbors = CVec.Directions.Select(d => d + cell)
					.Where(c => map.Contains(c) && !removedCells.Contains(c) && ClusterCondition(c));

				ClusterContents foundCluster = null;
				foreach (var n in neighbors)
				{
					if (foundCluster == null)
					{
						// Join the first viable neighbor we find.
						foundCluster = clusterCellLayer[n];
						UpdateCell(cell, clusterCellLayer[n]);
					}
					else
					{
						// Assimilate nearby neighbors
						AssignCluster(clusterCellLayer[n], foundCluster);
					}
				}

				if (foundCluster == null)
				{
					// If we can't join a neighbor, create a new cluster
					UpdateCell(cell, new ClusterContents());
					clusters.Add(clusterCellLayer[cell]);
				}
			}

			foreach (var cell in removedCells)
			{
				// We don't need to test for splitting if it's the last cell in a domain
				if (clusterCellLayer[cell].Cells.Count > 1)
				{
					// Select all neighbors inside the map boundaries
					var neighbors = CVec.Directions.Select(d => d + cell)
						.Where(c => clusterCellLayer[cell].Cells.Contains(c) && !removedCells.Contains(c)).ToList();

					// We don't need to test for splitting if it's an edge of a domain
					if (neighbors.Count >= 2)
						RecalculateCluster(clusterCellLayer[cell], removedCells.ToList());
				}

				UpdateCell(cell, emptyCluster);
			}
		}

		public void AddFixedConnection(IEnumerable<CPos> connectedCells)
		{
			var cells = connectedCells.ToList();
			var connection = new ClusterTransientConnection(true, cells);
			foreach (var c in cells)
			{
				if (!clusterCellLayer[c].TransientConnections.Contains(connection))
					clusterCellLayer[c].TransientConnections.Add(connection);
			}
		}

		protected void UpdateTransientConnections(IEnumerable<ClusterTransientConnection> connections, ClusterContents oldCluster, ClusterContents newCluster)
		{
			if (oldCluster == newCluster)
				return;

			connections.Where(x => x.ConnectedCells.Any(y => clusterCellLayer[y] == newCluster))
				.Do(x =>
				{
					if (!newCluster.TransientConnections.Contains(x))
						newCluster.TransientConnections.Add(x);
				});

			connections.Where(x => !x.ConnectedCells.Any(y => clusterCellLayer[y] == oldCluster))
				.Do(x => newCluster.TransientConnections.Remove(x));
		}

		protected void UpdateCell(CPos cell, ClusterContents newCluster)
		{
			if (clusterCellLayer[cell] == newCluster)
				return;

			var oldCluster = clusterCellLayer[cell];

			if (!oldCluster.IsEmpty)
				oldCluster.Cells.Remove(cell);
			if (!newCluster.IsEmpty)
				newCluster.Cells.Add(cell);
			clusterCellLayer[cell] = newCluster;

			var affectedConnections = oldCluster.TransientConnections.Where(x => x.ConnectedCells.Contains(cell));
			if (affectedConnections.Any())
				UpdateTransientConnections(affectedConnections, oldCluster, newCluster);

			// if cluster is empty, remove it
			if (!oldCluster.IsEmpty && !oldCluster.Cells.Any())
				clusters.Remove(oldCluster);
		}

		protected void AssignCluster(ClusterContents oldCluster, ClusterContents newCluster)
		{
			if (oldCluster == newCluster)
				return;

			oldCluster.Cells.Do(x => clusterCellLayer[x] = newCluster);
			newCluster.Cells.AddRange(oldCluster.Cells);

			var affectedConnections = oldCluster.TransientConnections
				.Where(x => x.ConnectedCells
				.Any(y => oldCluster.Cells
				.Contains(y)));
			if (affectedConnections.Any())
				UpdateTransientConnections(affectedConnections, oldCluster, newCluster);

			clusters.Remove(oldCluster);
		}

		protected void RecalculateCluster(ClusterContents cluster, List<CPos> deletedCells)
		{
			var selectedCluster = cluster;
			var toProcess = new Queue<CPos>();
			var toCheck = new List<CPos>(cluster.Cells.Where(x => !deletedCells.Contains(x)));
			toCheck.Do(x => toProcess.Enqueue(x));

			while (toProcess.Count != 0)
			{
				var start = toProcess.Dequeue();
				if (!toCheck.Contains(start))
					continue;

				var clusterQueue = new Queue<CPos>();
				clusterQueue.Enqueue(start);

				while (clusterQueue.Count != 0)
				{
					var n = clusterQueue.Dequeue();
					if (!toCheck.Contains(n))
						continue;

					toCheck.Remove(n);
					UpdateCell(n, selectedCluster);

					CVec.Directions.Select(d => d + n)
						.Where(d => toCheck.Contains(d))
						.Do(d => clusterQueue.Enqueue(d));
				}

				// Don't create a new cluster if all cells are checked
				if (toCheck.Count != 0)
				{
					if (selectedCluster == cluster)
					{
						cluster.Cells.RemoveAll(x => toCheck.Contains(x));
					}

					selectedCluster = new ClusterContents();
					clusters.Add(selectedCluster);
				}
			}
		}

		protected virtual ushort BuildDomain()
		{
			ClusterContents cluster = null;

			var visited = new CellLayer<bool>(map);

			var toProcess = new Queue<CPos>();
			toProcess.Enqueue(MPos.Zero.ToCPos(map));

			// Flood-fill over each cluster.
			while (toProcess.Count != 0)
			{
				var start = toProcess.Dequeue();

				// Technically redundant with the check in the inner loop, but prevents
				// ballooning the cluster amount.
				if (visited[start])
					continue;

				var clusterQueue = new Queue<CPos>();
				clusterQueue.Enqueue(start);

				var currentTrue = ClusterCondition(start);
				cluster = new ClusterContents();
				clusters.Add(cluster);

				// Add all contiguous cells to our cluster, and make a note of
				// any non-contiguous cells for future clusters.
				while (clusterQueue.Count != 0)
				{
					var n = clusterQueue.Dequeue();
					if (visited[n])
						continue;

					// add cell to queue if cell does not belong in queue
					if (currentTrue != ClusterCondition(n))
					{
						toProcess.Enqueue(n);
						continue;
					}

					visited[n] = true;
					if (currentTrue)
					{
						clusterCellLayer[n] = cluster;
						cluster.Cells.Add(n);
					}
					else
						clusterCellLayer[n] = emptyCluster;

					// Don't crawl off the map, or add already-visited cells.
					CVec.Directions.Select(d => d + n)
						.Where(d => visited.Contains(d) && !visited[d])
						.Do(d => clusterQueue.Enqueue(d));
				}
			}

			return cluster.ClusterId;
		}
	}

	class ClusterContents
	{
		public bool IsEmpty { get; private set; }

		// Domain index for debugging only
		static ushort domainIndex = 1;
		public ushort ClusterId { get; private set; }
		public List<CPos> Cells = new List<CPos>();

		public readonly List<ClusterTransientConnection> TransientConnections = new List<ClusterTransientConnection>();

		public ClusterContents()
		{
			ClusterId = domainIndex;
			domainIndex += 1;
		}

		public ClusterContents SetEmpty()
		{
			ClusterId = 0;
			IsEmpty = true;
			return this;
		}
	}

	// For TS bridges and tunnels
	class ClusterTransientConnection
	{
		public bool Active;
		public readonly List<CPos> ConnectedCells;

		public ClusterTransientConnection(bool active, List<CPos> cells)
		{
			ConnectedCells = cells;
			Active = active;
		}
	}
}
