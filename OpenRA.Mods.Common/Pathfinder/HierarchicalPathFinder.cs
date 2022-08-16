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
using System.Collections.ObjectModel;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// Provides pathfinding abilities for actors that use a specific <see cref="Locomotor"/>.
	/// Maintains a hierarchy of abstract graphs that provide a more accurate heuristic function during
	/// A* pathfinding than the one available from <see cref="PathSearch.DefaultCostEstimator(Locomotor)"/>.
	/// This allows for faster pathfinding.
	/// </summary>
	/// <remarks>
	/// <para>The goal of this pathfinder is to increase performance of path searches. <see cref="PathSearch"/> is used
	/// to perform a path search as usual, but a different heuristic function is provided that is more accurate. This
	/// means fewer nodes have to be explored during the search, resulting in a performance increase.</para>
	///
	/// <para>When an A* path search is performed, the search expands outwards from the source location until the
	/// target is found. The heuristic controls how this expansion occurs. When the heuristic of h(n) = 0 is given, we
	/// get Dijkstra's algorithm. The search grows outwards from the source node in an expanding circle with no sense
	/// of direction. This will find the shortest path by brute force. It will explore many nodes during the search,
	/// including lots of nodes in the opposite direction to the target.</para>
	///
	/// <para><see cref="PathSearch.DefaultCostEstimator(Locomotor)"/> provides heuristic for searching a 2D grid. It
	/// estimates the cost as the straight-line distance between the source and target nodes. The search grows in a
	/// straight line towards the target node. This is a vast improvement over Dijkstra's algorithm as we now
	/// prioritize exploring nodes that lie closer to the target, rather than exploring nodes that take us away from
	/// the target.</para>
	///
	/// <para>This default straight-line heuristic still has drawbacks - it is unaware of the obstacles on the grid. If
	/// the route to be found requires steering around obstacles then this heuristic can perform badly. Imagine a path
	/// that must steer around a lake, or move back on itself to get out of a dead end. In these cases the straight-line
	/// heuristic moves blindly towards the target, when actually the path requires that we move sidewards or even
	/// backwards to find a route. When this occurs then the straight-line heuristic ends up exploring nodes that
	/// aren't useful - they lead us into dead ends or directly into an obstacle that we need to go around instead.
	/// </para>
	///
	/// <para>The <see cref="HierarchicalPathFinder"/> improves the heuristic by making it aware of unreachable map
	/// terrain. A "low-resolution" version of the map is maintained, and used to provide an initial route. When the
	/// search is conducted it explores along this initial route. This allows the search to "know" it needs to go
	/// sideways around the lake or backwards out of the dead-end, meaning we can explore even fewer nodes.</para>
	///
	/// <para>The "low-resolution" version of the map is referred to as the abstract graph. The abstract graph is
	/// created by dividing the map up into a series of grids, of say 10x10 nodes. Within each grid, we determine the
	/// connected regions of nodes within that grid. If all the nodes within the grid connect to each other, we have
	/// one such region. If they are split up by impassable terrain then we may have two or more regions within the
	/// grid. Every region will be represented by one node in the abstract graph (an abstract node, for short).</para>
	///
	/// <para>When a path search is to be performed, we first perform a A* search on the abstract graph with the
	/// <see cref="PathSearch.DefaultCostEstimator(Locomotor)"/>. This graph is much smaller than the full map, so
	/// this search is quick. The resulting path gives us the initial route between each abstract node. We can then use
	/// this to create the improved heuristic for use on the path search on the full resolution map. When determining
	/// the cost for the node, we can use the straight-line distance towards the next abstract node as our estimate.
	/// Our search is therefore guided along the initial route.</para>
	///
	/// <para>This implementation only maintains one level of abstract graph, but a hierarchy of such graphs is
	/// possible. This allows the top-level and lowest resolution graph to be as small as possible - important because
	/// it will be searched using the dumbest heuristic. Each level underneath is higher-resolution and contains more
	/// nodes, but uses a heuristic informed from the previous level to guide the search in the right direction.</para>
	///
	/// <para>This implementation is aware of movement costs over terrain given by
	/// <see cref="Locomotor.MovementCostToEnterCell"/>. It is aware of changes to the costs in terrain and able to
	/// update the abstract graph when this occurs. It is able to search the abstract graph as if
	/// <see cref="BlockedByActor.None"/> had been specified. It is not aware of actors on the map. So blocking actors
	/// will not be accounted for in the heuristic.</para>
	///
	/// <para>If the obstacle on the map is from terrain (e.g. a cliff or lake) the heuristic will work well. If the
	/// obstacle is from a blocking actor (trees, walls, buildings, units) the heuristic is unaware of these. Therefore
	/// the same problem where the search goes in the wrong direction is possible, e.g. through a choke-point that has
	/// been walled off. In this scenario the performance benefit will be lost, as the search will have to explore more
	/// nodes until it can get around the obstacle.</para>
	///
	/// <para>In summary, the <see cref="HierarchicalPathFinder"/> reduces the performance impact of path searches that
	/// must go around terrain, but does not improve performance of searches that must go around actors.</para>
	/// </remarks>
	public sealed class HierarchicalPathFinder
	{
		// This value determined via empiric testing as the best performance trade-off.
		const int GridSize = 10;

		readonly World world;
		readonly Locomotor locomotor;
		readonly Func<CPos, CPos, int> costEstimator;
		readonly HashSet<int> dirtyGridIndexes = new HashSet<int>();
		Grid mapBounds;
		int gridXs;
		int gridYs;

		/// <summary>
		/// Index by a <see cref="GridIndex"/>.
		/// </summary>
		GridInfo[] gridInfos;

		/// <summary>
		/// The abstract graph is represented here.
		/// An abstract node is the key, and costs to other abstract nodes are then available.
		/// Abstract nodes with no connections are NOT present in the graph.
		/// A lookup will fail, rather than return an empty list.
		/// </summary>
		Dictionary<CPos, List<GraphConnection>> abstractGraph;

		/// <summary>
		/// The abstract domains are represented here.
		/// An abstract node is the key, and a domain index is given.
		/// If the domain index of two nodes is equal, a path exists between them (ignoring all blocking actors).
		/// If unequal, no path is possible.
		/// </summary>
		readonly Dictionary<CPos, uint> abstractDomains;

		/// <summary>
		/// Knows about the abstract nodes within a grid. Can map a local cell to its abstract node.
		/// </summary>
		readonly struct GridInfo
		{
			readonly CPos?[] singleAbstractCellForLayer;
			readonly Dictionary<CPos, CPos> localCellToAbstractCell;

			public GridInfo(CPos?[] singleAbstractCellForLayer, Dictionary<CPos, CPos> localCellToAbstractCell)
			{
				this.singleAbstractCellForLayer = singleAbstractCellForLayer;
				this.localCellToAbstractCell = localCellToAbstractCell;
			}

			/// <summary>
			/// Maps a local cell to a abstract node in the graph.
			/// Returns null when the local cell is unreachable.
			/// </summary>
			public CPos? AbstractCellForLocalCell(CPos localCell)
			{
				var abstractCell = singleAbstractCellForLayer[localCell.Layer];
				if (abstractCell != null)
					return abstractCell;
				if (localCellToAbstractCell.TryGetValue(localCell, out var abstractCellFromMap))
					return abstractCellFromMap;
				return null;
			}

			public void CopyAbstractCellsInto(HashSet<CPos> set)
			{
				foreach (var single in singleAbstractCellForLayer)
					if (single != null)
						set.Add(single.Value);
				foreach (var cell in localCellToAbstractCell.Values)
					set.Add(cell);
			}
		}

		/// <summary>
		/// Represents an abstract graph with some extra edges inserted.
		/// Instead of building a new dictionary with the edges added, we build a supplemental dictionary of changes.
		/// This is to avoid copying the entire abstract graph.
		/// </summary>
		sealed class AbstractGraphWithInsertedEdges
		{
			readonly Dictionary<CPos, List<GraphConnection>> abstractEdges;
			readonly Dictionary<CPos, IEnumerable<GraphConnection>> changedEdges;

			public AbstractGraphWithInsertedEdges(
				Dictionary<CPos, List<GraphConnection>> abstractEdges,
				IList<GraphEdge> sourceEdges,
				GraphEdge? targetEdge,
				Func<CPos, CPos, int> costEstimator)
			{
				this.abstractEdges = abstractEdges;
				changedEdges = new Dictionary<CPos, IEnumerable<GraphConnection>>(sourceEdges.Count * 9 + (targetEdge != null ? 9 : 0));
				foreach (var sourceEdge in sourceEdges)
					InsertEdgeAsBidirectional(sourceEdge, costEstimator);
				if (targetEdge != null)
					InsertEdgeAsBidirectional(targetEdge.Value, costEstimator);
			}

			void InsertEdgeAsBidirectional(GraphEdge edge, Func<CPos, CPos, int> costEstimator)
			{
				InsertConnections(edge.Source, edge.Destination, costEstimator);
			}

			void InsertConnections(CPos localCell, CPos abstractCell, Func<CPos, CPos, int> costEstimator)
			{
				if (!abstractEdges.TryGetValue(abstractCell, out var edges))
					edges = new List<GraphConnection>();
				changedEdges[localCell] = edges
					.Select(e => new GraphConnection(e.Destination, costEstimator(localCell, e.Destination)))
					.Append(new GraphConnection(abstractCell, costEstimator(localCell, abstractCell)));

				IEnumerable<GraphConnection> abstractChangedEdges = edges;
				if (changedEdges.TryGetValue(abstractCell, out var existingEdges))
					abstractChangedEdges = existingEdges;
				changedEdges[abstractCell] = abstractChangedEdges
					.Append(new GraphConnection(localCell, costEstimator(abstractCell, localCell)));

				foreach (var conn in edges)
				{
					IEnumerable<GraphConnection> connChangedEdges;
					if (changedEdges.TryGetValue(conn.Destination, out var existingConnEdges))
						connChangedEdges = existingConnEdges;
					else
						connChangedEdges = abstractEdges[conn.Destination];

					changedEdges[conn.Destination] = connChangedEdges
						.Append(new GraphConnection(localCell, costEstimator(conn.Destination, localCell)));
				}
			}

			public List<GraphConnection> GetConnections(CPos position)
			{
				if (changedEdges.TryGetValue(position, out var changedEdge))
					return changedEdge.ToList();
				if (abstractEdges.TryGetValue(position, out var abstractEdge))
					return abstractEdge;
				return new List<GraphConnection>();
			}
		}

		public HierarchicalPathFinder(World world, Locomotor locomotor)
		{
			this.world = world;
			this.locomotor = locomotor;
			if (locomotor.Info.TerrainSpeeds.Count == 0)
				return;

			costEstimator = PathSearch.DefaultCostEstimator(locomotor);

			BuildGrids();
			BuildCostTable();
			abstractDomains = new Dictionary<CPos, uint>(gridXs * gridYs);
			RebuildDomains();

			// When we build the cost table, it depends on the movement costs of the cells at that time.
			// When this changes, we must update the cost table.
			locomotor.CellCostChanged += RequireCostRefreshInCell;
		}

		public (
			IReadOnlyDictionary<CPos, List<GraphConnection>> AbstractGraph,
			IReadOnlyDictionary<CPos, uint> AbstractDomains) GetOverlayData()
		{
			if (costEstimator == null)
				return default;

			// Ensure the abstract graph and domains are up to date when using the overlay.
			RebuildDirtyGrids();
			RebuildDomains();
			return (
				new ReadOnlyDictionary<CPos, List<GraphConnection>>(abstractGraph),
				new ReadOnlyDictionary<CPos, uint>(abstractDomains));
		}

		/// <summary>
		/// Divides the map area up into a series of grids.
		/// </summary>
		void BuildGrids()
		{
			Grid GetCPosBounds(Map map)
			{
				if (map.Grid.Type == MapGridType.RectangularIsometric)
				{
					var bottomRight = map.AllCells.BottomRight;
					var bottomRightU = bottomRight.ToMPos(map).U;
					return new Grid(
						new CPos(0, -bottomRightU),
						new CPos(bottomRight.X + 1, bottomRight.Y + bottomRightU + 1),
						false);
				}

				return new Grid(CPos.Zero, (CPos)map.MapSize, false);
			}

			mapBounds = GetCPosBounds(world.Map);
			gridXs = Exts.IntegerDivisionRoundingAwayFromZero(mapBounds.Width, GridSize);
			gridYs = Exts.IntegerDivisionRoundingAwayFromZero(mapBounds.Height, GridSize);

			var customMovementLayers = world.GetCustomMovementLayers();
			gridInfos = new GridInfo[gridXs * gridYs];
			for (var gridX = mapBounds.TopLeft.X; gridX < mapBounds.BottomRight.X; gridX += GridSize)
				for (var gridY = mapBounds.TopLeft.Y; gridY < mapBounds.BottomRight.Y; gridY += GridSize)
					gridInfos[GridIndex(new CPos(gridX, gridY))] = BuildGrid(gridX, gridY, customMovementLayers);
		}

		/// <summary>
		/// Determines the abstract nodes within a single grid. One abstract node will be created for each set of cells
		/// that are reachable from each other within the grid area. A grid with open terrain will commonly have one
		/// abstract node. If impassable terrain such as cliffs or water divides the cells into 2 or more distinct
		/// regions, one abstract node is created for each region. We also remember which cells belong to which
		/// abstract node. Given a local cell, this allows us to determine which abstract node it belongs to.
		/// </summary>
		GridInfo BuildGrid(int gridX, int gridY, ICustomMovementLayer[] customMovementLayers)
		{
			var singleAbstractCellForLayer = new CPos?[customMovementLayers.Length];
			var localCellToAbstractCell = new Dictionary<CPos, CPos>();
			for (byte gridLayer = 0; gridLayer < customMovementLayers.Length; gridLayer++)
			{
				if (gridLayer != 0 &&
					(customMovementLayers[gridLayer] == null ||
					!customMovementLayers[gridLayer].EnabledForLocomotor(locomotor.Info)))
					continue;

				var grid = GetGrid(new CPos(gridX, gridY, gridLayer), mapBounds);
				var accessibleCells = new HashSet<CPos>();
				for (var y = gridY; y < grid.BottomRight.Y; y++)
				{
					for (var x = gridX; x < grid.BottomRight.X; x++)
					{
						var cell = new CPos(x, y, gridLayer);
						if (locomotor.MovementCostForCell(cell) != PathGraph.MovementCostForUnreachableCell)
							accessibleCells.Add(cell);
					}
				}

				CPos AbstractCellForLocalCells(List<CPos> cells, byte layer)
				{
					var minX = int.MaxValue;
					var minY = int.MaxValue;
					var maxX = int.MinValue;
					var maxY = int.MinValue;
					foreach (var cell in cells)
					{
						minX = Math.Min(cell.X, minX);
						minY = Math.Min(cell.Y, minY);
						maxX = Math.Max(cell.X, maxX);
						maxY = Math.Max(cell.Y, maxY);
					}

					var regionWidth = maxX - minX;
					var regionHeight = maxY - minY;
					var desired = new CPos(minX + regionWidth / 2, minY + regionHeight / 2, layer);

					// Make sure the abstract cell is one of the available local cells.
					// This ensures each abstract cell we generate is unique.
					// We'll choose an abstract node as close to the middle of the region as possible.
					var abstractCell = desired;
					var distance = int.MaxValue;
					foreach (var cell in cells)
					{
						var newDistance = (cell - desired).LengthSquared;
						if (distance > newDistance)
						{
							distance = newDistance;
							abstractCell = cell;
						}
					}

					return abstractCell;
				}

				// Flood fill the search area from one of the accessible cells.
				// We can use this to determine the connected regions within the grid.
				// Each region we discover will be represented by an abstract node.
				// Repeat this process until all disjoint regions are discovered.
				var hasPopulatedAbstractCellForLayer = false;
				while (accessibleCells.Count > 0)
				{
					var src = accessibleCells.First();
					using (var search = GetLocalPathSearch(
						null, new[] { src }, src, null, null, BlockedByActor.None, false, grid, 100))
					{
						var localCellsInRegion = search.ExpandAll();
						var abstractCell = AbstractCellForLocalCells(localCellsInRegion, gridLayer);
						accessibleCells.ExceptWith(localCellsInRegion);

						// PERF: If there is only one distinct region of cells,
						// we can represent this grid with one abstract cell.
						// We don't need to remember how to map back from a local cell to an abstract cell.
						if (!hasPopulatedAbstractCellForLayer && accessibleCells.Count == 0)
							singleAbstractCellForLayer[gridLayer] = abstractCell;
						else
						{
							// When there is more than one region within the grid
							// (imagine a wall or stream separating the grid into disjoint areas)
							// then we will remember a mapping from local cells to each of their abstract cells.
							hasPopulatedAbstractCellForLayer = true;
							foreach (var localCell in localCellsInRegion)
								localCellToAbstractCell.Add(localCell, abstractCell);
						}
					}
				}
			}

			return new GridInfo(singleAbstractCellForLayer, localCellToAbstractCell);
		}

		/// <summary>
		/// Builds the abstract graph in entirety. The abstract graph contains edges between all the abstract nodes
		/// that represent the costs to move between them.
		/// </summary>
		void BuildCostTable()
		{
			abstractGraph = new Dictionary<CPos, List<GraphConnection>>(gridXs * gridYs);
			var customMovementLayers = world.GetCustomMovementLayers();
			for (var gridX = mapBounds.TopLeft.X; gridX < mapBounds.BottomRight.X; gridX += GridSize)
				for (var gridY = mapBounds.TopLeft.Y; gridY < mapBounds.BottomRight.Y; gridY += GridSize)
					foreach (var edges in GetAbstractEdgesForGrid(gridX, gridY, customMovementLayers))
						abstractGraph.Add(edges.Key, edges.Value);
		}

		/// <summary>
		/// For a given grid, determines the edges between the abstract nodes within the grid and the abstract nodes
		/// within adjacent grids on the same layer. Also determines any edges available to grids on other layers via
		/// custom movement layers.
		/// </summary>
		IEnumerable<KeyValuePair<CPos, List<GraphConnection>>> GetAbstractEdgesForGrid(int gridX, int gridY, ICustomMovementLayer[] customMovementLayers)
		{
			var abstractEdges = new HashSet<(CPos Src, CPos Dst)>();
			for (byte gridLayer = 0; gridLayer < customMovementLayers.Length; gridLayer++)
			{
				if (gridLayer != 0 &&
					(customMovementLayers[gridLayer] == null ||
					!customMovementLayers[gridLayer].EnabledForLocomotor(locomotor.Info)))
					continue;

				// Searches along edges of all grids within a layer.
				// Checks for the local edge cell if we can traverse to any of the three adjacent cells in the next grid.
				// Builds connections in the abstract graph when any local cells have connections.
				void AddAbstractEdges(int xIncrement, int yIncrement, CVec adjacentVec, int2 offset)
				{
					var startY = gridY + offset.Y;
					var startX = gridX + offset.X;
					for (var y = startY; y < startY + GridSize; y += yIncrement)
					{
						for (var x = startX; x < startX + GridSize; x += xIncrement)
						{
							var cell = new CPos(x, y, gridLayer);
							if (locomotor.MovementCostForCell(cell) == PathGraph.MovementCostForUnreachableCell)
								continue;

							var adjacentCell = cell + adjacentVec;
							for (var i = -1; i <= 1; i++)
							{
								var candidateCell = adjacentCell + i * new CVec(adjacentVec.Y, adjacentVec.X);
								if (locomotor.MovementCostToEnterCell(null, cell, candidateCell, BlockedByActor.None, null) !=
									PathGraph.MovementCostForUnreachableCell)
								{
									var gridInfo = gridInfos[GridIndex(cell)];
									var abstractCell = gridInfo.AbstractCellForLocalCell(cell);
									if (abstractCell == null)
										continue;

									var gridInfoAdjacent = gridInfos[GridIndex(candidateCell)];
									var abstractCellAdjacent = gridInfoAdjacent.AbstractCellForLocalCell(candidateCell);
									if (abstractCellAdjacent == null)
										continue;

									abstractEdges.Add((abstractCell.Value, abstractCellAdjacent.Value));
								}
							}
						}
					}
				}

				// Searches all cells within a layer.
				// Checks for the local cell if we can traverse from/to a custom movement layer.
				// Builds connections in the abstract graph when any local cells have connections.
				void AddAbstractCustomLayerEdges()
				{
					var gridCml = customMovementLayers[gridLayer];
					for (byte candidateLayer = 0; candidateLayer < customMovementLayers.Length; candidateLayer++)
					{
						if (gridLayer == candidateLayer)
							continue;

						var candidateCml = customMovementLayers[candidateLayer];
						if (candidateLayer != 0 && (candidateCml == null || !candidateCml.EnabledForLocomotor(locomotor.Info)))
							continue;

						for (var y = gridY; y < gridY + GridSize; y++)
						{
							for (var x = gridX; x < gridX + GridSize; x++)
							{
								var cell = new CPos(x, y, gridLayer);
								if (locomotor.MovementCostForCell(cell) == PathGraph.MovementCostForUnreachableCell)
									continue;

								CPos candidateCell;
								if (gridLayer == 0)
								{
									candidateCell = new CPos(cell.X, cell.Y, candidateLayer);
									if (candidateCml.EntryMovementCost(locomotor.Info, candidateCell) == PathGraph.MovementCostForUnreachableCell)
										continue;
								}
								else
								{
									candidateCell = new CPos(cell.X, cell.Y, 0);
									if (gridCml.ExitMovementCost(locomotor.Info, candidateCell) == PathGraph.MovementCostForUnreachableCell)
										continue;
								}

								if (locomotor.MovementCostToEnterCell(null, cell, candidateCell, BlockedByActor.None, null) ==
									PathGraph.MovementCostForUnreachableCell)
									continue;

								var gridInfo = gridInfos[GridIndex(cell)];
								var abstractCell = gridInfo.AbstractCellForLocalCell(cell);
								if (abstractCell == null)
									continue;

								var gridInfoAdjacent = gridInfos[GridIndex(candidateCell)];
								var abstractCellAdjacent = gridInfoAdjacent.AbstractCellForLocalCell(candidateCell);
								if (abstractCellAdjacent == null)
									continue;

								abstractEdges.Add((abstractCell.Value, abstractCellAdjacent.Value));
							}
						}
					}
				}

				// Top, Left, Bottom, Right
				AddAbstractEdges(1, GridSize, new CVec(0, -1), new int2(0, 0));
				AddAbstractEdges(GridSize, 1, new CVec(-1, 0), new int2(0, 0));
				AddAbstractEdges(1, GridSize, new CVec(0, 1), new int2(0, GridSize - 1));
				AddAbstractEdges(GridSize, 1, new CVec(1, 0), new int2(GridSize - 1, 0));

				AddAbstractCustomLayerEdges();
			}

			return abstractEdges
				.GroupBy(edge => edge.Src)
				.Select(group => KeyValuePair.Create(
					group.Key,
					group.Select(edge => new GraphConnection(edge.Dst, costEstimator(edge.Src, edge.Dst))).ToList()));
		}

		/// <summary>
		/// When reachability changes for a cell, marks the grid it belongs to as out of date.
		/// </summary>
		void RequireCostRefreshInCell(CPos cell, short oldCost, short newCost)
		{
			// We don't care about the specific cost of the cell, just whether it is reachable or not.
			// This is good because updating the table is expensive, so only having to update it when
			// the reachability changes rather than for all costs changes saves us a lot of time.
			if (oldCost == PathGraph.MovementCostForUnreachableCell ^ newCost == PathGraph.MovementCostForUnreachableCell)
				dirtyGridIndexes.Add(GridIndex(cell));
		}

		int GridIndex(CPos cellInGrid)
		{
			return
				(cellInGrid.Y - mapBounds.TopLeft.Y) / GridSize * gridXs +
				(cellInGrid.X - mapBounds.TopLeft.X) / GridSize;
		}

		CPos GetGridTopLeft(int gridIndex, byte layer)
		{
			return new CPos(
				gridIndex % gridXs * GridSize + mapBounds.TopLeft.X,
				gridIndex / gridXs * GridSize + mapBounds.TopLeft.Y,
				layer);
		}

		static CPos GetGridTopLeft(CPos cellInGrid, Grid mapBounds)
		{
			return new CPos(
				((cellInGrid.X - mapBounds.TopLeft.X) / GridSize * GridSize) + mapBounds.TopLeft.X,
				((cellInGrid.Y - mapBounds.TopLeft.Y) / GridSize * GridSize) + mapBounds.TopLeft.Y,
				cellInGrid.Layer);
		}

		static Grid GetGrid(CPos cellInGrid, Grid mapBounds)
		{
			var gridTopLeft = GetGridTopLeft(cellInGrid, mapBounds);
			var width = Math.Min(mapBounds.BottomRight.X - gridTopLeft.X, GridSize);
			var height = Math.Min(mapBounds.BottomRight.Y - gridTopLeft.Y, GridSize);

			return new Grid(
				gridTopLeft,
				gridTopLeft + new CVec(width, height),
				true);
		}

		/// <summary>
		/// Calculates a path for the actor from multiple possible sources to target, using a unidirectional search.
		/// Returned path is *reversed* and given target to source.
		/// The actor must use the same <see cref="Locomotor"/> as this <see cref="HierarchicalPathFinder"/>.
		/// </summary>
		public List<CPos> FindPath(Actor self, IReadOnlyCollection<CPos> sources, CPos target,
			BlockedByActor check, int heuristicWeightPercentage, Func<CPos, int> customCost,
			Actor ignoreActor, bool laneBias, PathFinderOverlay pathFinderOverlay)
		{
			if (costEstimator == null)
				return PathFinder.NoPath;

			pathFinderOverlay?.NewRecording(self, sources, target);

			RebuildDirtyGrids();

			var targetAbstractCell = AbstractCellForLocalCell(target);
			if (targetAbstractCell == null)
				return PathFinder.NoPath;

			var sourcesWithReachableNodes = new List<CPos>(sources.Count);
			var sourceEdges = new List<GraphEdge>(sources.Count);
			foreach (var source in sources)
			{
				var sourceAbstractCell = AbstractCellForLocalCell(source);
				if (sourceAbstractCell == null)
					continue;

				sourcesWithReachableNodes.Add(source);
				var sourceEdge = EdgeFromLocalToAbstract(source, sourceAbstractCell.Value);
				if (sourceEdge != null)
					sourceEdges.Add(sourceEdge.Value);
			}

			if (sourcesWithReachableNodes.Count == 0)
				return PathFinder.NoPath;

			var targetEdge = EdgeFromLocalToAbstract(target, targetAbstractCell.Value);

			// The new edges will be treated as bi-directional.
			var fullGraph = new AbstractGraphWithInsertedEdges(abstractGraph, sourceEdges, targetEdge, costEstimator);

			// Determine an abstract path to all sources, for use in a unidirectional search.
			var estimatedSearchSize = (abstractGraph.Count + 2) / 8;
			using (var reverseAbstractSearch = PathSearch.ToTargetCellOverGraph(
				fullGraph.GetConnections, locomotor, target, target, estimatedSearchSize, pathFinderOverlay?.RecordAbstractEdges(self)))
			{
				var sourcesWithPathableNodes = new List<CPos>(sourcesWithReachableNodes.Count);
				foreach (var source in sourcesWithReachableNodes)
				{
					// Check if we have already found a route to this node before we attempt to expand the search.
					var sourceStatus = reverseAbstractSearch.Graph[source];
					if (sourceStatus.Status == CellStatus.Closed)
					{
						if (sourceStatus.CostSoFar != PathGraph.PathCostForInvalidPath)
							sourcesWithPathableNodes.Add(source);
					}
					else
					{
						reverseAbstractSearch.TargetPredicate = cell => cell == source;
						if (reverseAbstractSearch.ExpandToTarget())
							sourcesWithPathableNodes.Add(source);
					}
				}

				if (sourcesWithPathableNodes.Count == 0)
					return PathFinder.NoPath;

				using (var fromSrc = GetLocalPathSearch(
					self, sourcesWithPathableNodes, target, customCost, ignoreActor, check, laneBias, null, heuristicWeightPercentage,
					heuristic: Heuristic(reverseAbstractSearch, estimatedSearchSize),
					recorder: pathFinderOverlay?.RecordLocalEdges(self)))
					return fromSrc.FindPath();
			}
		}

		/// <summary>
		/// Calculates a path for the actor from source to target, using a bidirectional search.
		/// Returned path is *reversed* and given target to source.
		/// The actor must use the same <see cref="Locomotor"/> as this <see cref="HierarchicalPathFinder"/>.
		/// </summary>
		public List<CPos> FindPath(Actor self, CPos source, CPos target,
			BlockedByActor check, int heuristicWeightPercentage, Func<CPos, int> customCost,
			Actor ignoreActor, bool laneBias, PathFinderOverlay pathFinderOverlay)
		{
			if (costEstimator == null)
				return PathFinder.NoPath;

			// If the source and target are close, see if they can be reached locally.
			// This avoids the cost of an abstract search unless we need one.
			const int CloseGridDistance = 2;
			if ((target - source).LengthSquared < GridSize * GridSize * CloseGridDistance * CloseGridDistance && source.Layer == target.Layer)
			{
				var gridToSearch = new Grid(
					new CPos(
						Math.Min(source.X, target.X) - GridSize / 2,
						Math.Min(source.Y, target.Y) - GridSize / 2,
						source.Layer),
					new CPos(
						Math.Max(source.X, target.X) + GridSize / 2,
						Math.Max(source.Y, target.Y) + GridSize / 2,
						source.Layer),
					false);

				pathFinderOverlay?.NewRecording(self, new[] { source }, target);

				List<CPos> localPath;
				using (var search = GetLocalPathSearch(
					self, new[] { source }, target, customCost, ignoreActor, check, laneBias, gridToSearch, heuristicWeightPercentage,
					recorder: pathFinderOverlay?.RecordLocalEdges(self)))
					localPath = search.FindPath();

				if (localPath.Count > 0)
					return localPath;
			}

			pathFinderOverlay?.NewRecording(self, new[] { source }, target);

			RebuildDirtyGrids();

			var targetAbstractCell = AbstractCellForLocalCell(target);
			if (targetAbstractCell == null)
				return PathFinder.NoPath;
			var sourceAbstractCell = AbstractCellForLocalCell(source);
			if (sourceAbstractCell == null)
				return PathFinder.NoPath;
			var targetEdge = EdgeFromLocalToAbstract(target, targetAbstractCell.Value);
			var sourceEdge = EdgeFromLocalToAbstract(source, sourceAbstractCell.Value);

			// The new edges will be treated as bi-directional.
			var fullGraph = new AbstractGraphWithInsertedEdges(
				abstractGraph, sourceEdge != null ? new[] { sourceEdge.Value } : Array.Empty<GraphEdge>(), targetEdge, costEstimator);

			// Determine an abstract path in both directions, for use in a bidirectional search.
			var estimatedSearchSize = (abstractGraph.Count + 2) / 8;
			using (var forwardAbstractSearch = PathSearch.ToTargetCellOverGraph(
				fullGraph.GetConnections, locomotor, source, target, estimatedSearchSize, pathFinderOverlay?.RecordAbstractEdges(self)))
			{
				if (!forwardAbstractSearch.ExpandToTarget())
					return PathFinder.NoPath;

				using (var reverseAbstractSearch = PathSearch.ToTargetCellOverGraph(
					fullGraph.GetConnections, locomotor, target, source, estimatedSearchSize, pathFinderOverlay?.RecordAbstractEdges(self)))
				{
					reverseAbstractSearch.ExpandToTarget();

					using (var fromSrc = GetLocalPathSearch(
						self, new[] { source }, target, customCost, ignoreActor, check, laneBias, null, heuristicWeightPercentage,
						heuristic: Heuristic(reverseAbstractSearch, estimatedSearchSize),
						recorder: pathFinderOverlay?.RecordLocalEdges(self)))
					using (var fromDest = GetLocalPathSearch(
						self, new[] { target }, source, customCost, ignoreActor, check, laneBias, null, heuristicWeightPercentage,
						heuristic: Heuristic(forwardAbstractSearch, estimatedSearchSize),
						recorder: pathFinderOverlay?.RecordLocalEdges(self),
						inReverse: true))
						return PathSearch.FindBidiPath(fromDest, fromSrc);
				}
			}
		}

		/// <summary>
		/// Determines if a path exists between source and target.
		/// Only terrain is taken into account, i.e. as if <see cref="BlockedByActor.None"/> was given.
		/// This would apply for any actor using the same <see cref="Locomotor"/> as this <see cref="HierarchicalPathFinder"/>.
		/// </summary>
		public bool PathExists(CPos source, CPos target)
		{
			if (costEstimator == null)
				return false;

			if (!world.Map.Contains(source) || !world.Map.Contains(target))
				return false;

			RebuildDomains();

			var sourceGridInfo = gridInfos[GridIndex(source)];
			var targetGridInfo = gridInfos[GridIndex(target)];
			var abstractSource = sourceGridInfo.AbstractCellForLocalCell(source);
			if (abstractSource == null)
				return false;
			var abstractTarget = targetGridInfo.AbstractCellForLocalCell(target);
			if (abstractTarget == null)
				return false;
			var sourceDomain = abstractDomains[abstractSource.Value];
			var targetDomain = abstractDomains[abstractTarget.Value];
			return sourceDomain == targetDomain;
		}

		/// <summary>
		/// The abstract graph can become out of date when reachability costs for terrain change.
		/// When this occurs, we must rebuild any affected parts of the abstract graph so it remains correct.
		/// </summary>
		void RebuildDirtyGrids()
		{
			if (dirtyGridIndexes.Count == 0)
				return;

			// An empty domain indicates it is out of date and will require rebuilding when next accessed.
			abstractDomains.Clear();

			var customMovementLayers = world.GetCustomMovementLayers();
			foreach (var gridIndex in dirtyGridIndexes)
			{
				var oldGrid = gridInfos[gridIndex];
				var gridTopLeft = GetGridTopLeft(gridIndex, 0);
				gridInfos[gridIndex] = BuildGrid(gridTopLeft.X, gridTopLeft.Y, customMovementLayers);
				RebuildCostTable(gridTopLeft.X, gridTopLeft.Y, oldGrid, customMovementLayers);
			}

			dirtyGridIndexes.Clear();
		}

		/// <summary>
		/// Updates the abstract graph to account for changes in a specific grid. Any nodes and edges related to that
		/// grid will be removed, new nodes and edges will be determined and then inserted into the graph.
		/// </summary>
		void RebuildCostTable(int gridX, int gridY, GridInfo oldGrid, ICustomMovementLayer[] customMovementLayers)
		{
			// For this grid, it is possible the abstract nodes have changed.
			// Remove the old abstract nodes for this grid.
			// This is important as GetAbstractEdgesForGrid will look at the *current* grids.
			// So it won't be aware of any nodes that disappeared before we updated the grid.
			var abstractNodes = new HashSet<CPos>();
			oldGrid.CopyAbstractCellsInto(abstractNodes);
			foreach (var oldAbstractNode in abstractNodes)
				abstractGraph.Remove(oldAbstractNode);
			abstractNodes.Clear();

			// Add new abstract edges for this grid, since we cleared out the old nodes everything should be new.
			foreach (var edges in GetAbstractEdgesForGrid(gridX, gridY, customMovementLayers))
				abstractGraph.Add(edges.Key, edges.Value);

			foreach (var direction in CVec.Directions)
			{
				var adjacentGrid = new CPos(gridX, gridY) + GridSize * direction;
				if (!mapBounds.Contains(adjacentGrid))
					continue;

				// For all adjacent grids, their abstract nodes will not have changed, but the connections may have done.
				// Update the connections, and keep track of which nodes we have updated.
				gridInfos[GridIndex(adjacentGrid)].CopyAbstractCellsInto(abstractNodes);
				foreach (var edges in GetAbstractEdgesForGrid(adjacentGrid.X, adjacentGrid.Y, customMovementLayers))
				{
					abstractGraph[edges.Key] = edges.Value;
					abstractNodes.Remove(edges.Key);
				}

				// If any nodes were left over they have no connections now, so we can remove them from the graph.
				foreach (var unconnectedNode in abstractNodes)
					abstractGraph.Remove(unconnectedNode);
				abstractNodes.Clear();
			}
		}

		/// <summary>
		/// The abstract domains can become out of date when the abstract graph changes.
		/// When this occurs, we must rebuild the domain cache.
		/// </summary>
		void RebuildDomains()
		{
			// First, rebuild the abstract graph if it is out of date.
			RebuildDirtyGrids();

			// Check if our domain cache is empty, if so this indicates it is out-of-date and needs rebuilding.
			if (abstractDomains.Count != 0)
				return;

			List<GraphConnection> AbstractEdge(CPos abstractCell)
			{
				if (abstractGraph.TryGetValue(abstractCell, out var abstractEdge))
					return abstractEdge;
				return null;
			}

			// As in BuildGrid, flood fill the search graph until all disjoint domains are discovered.
			var domain = 0u;
			var abstractCells = new HashSet<CPos>(abstractGraph.Count);
			foreach (var grid in gridInfos)
				grid.CopyAbstractCellsInto(abstractCells);
			while (abstractCells.Count > 0)
			{
				var searchCell = abstractCells.First();
				var search = PathSearch.ToTargetCellOverGraph(
					AbstractEdge,
					locomotor,
					searchCell,
					searchCell,
					abstractGraph.Count / 8);
				var searched = search.ExpandAll();
				foreach (var abstractCell in searched)
					abstractDomains.Add(abstractCell, domain);
				abstractCells.ExceptWith(searched);
				domain++;
			}
		}

		/// <summary>
		/// Maps a local cell to a abstract node in the graph.
		/// Returns null when the local cell is unreachable.
		/// </summary>
		CPos? AbstractCellForLocalCell(CPos localCell) =>
			gridInfos[GridIndex(localCell)].AbstractCellForLocalCell(localCell);

		/// <summary>
		/// Creates a <see cref="GraphEdge"/> from the <paramref name="localCell"/> to the <paramref name="abstractCell"/>.
		/// Return null when no edge is required, because the cells match.
		/// </summary>
		GraphEdge? EdgeFromLocalToAbstract(CPos localCell, CPos abstractCell)
		{
			if (localCell == abstractCell)
				return null;

			return new GraphEdge(localCell, abstractCell, costEstimator(localCell, abstractCell));
		}

		/// <summary>
		/// Uses the provided abstract search to provide an estimate of the distance remaining to the target
		/// (the heuristic) for a local path search. The abstract search must run in the opposite direction to the
		/// local search. So when searching from source to target, the abstract search must be from target to source.
		/// </summary>
		Func<CPos, int> Heuristic(PathSearch abstractSearch, int estimatedSearchSize)
		{
			var nodeForCostLookup = new Dictionary<CPos, CPos>(estimatedSearchSize);
			var graph = (SparsePathGraph)abstractSearch.Graph;
			return cell =>
			{
				// All cells searched by the heuristic are guaranteed to be reachable.
				// So we don't need to handle an abstract cell lookup failing, or the search failing to expand.
				// Cells added as initial starting points for the search are filtered out if they aren't reachable.
				// The search only explores accessible cells from then on.
				var gridInfo = gridInfos[GridIndex(cell)];
				var abstractCell = gridInfo.AbstractCellForLocalCell(cell).Value;
				var info = graph[abstractCell];

				// Expand the abstract search only if we have yet to get a route to the abstract cell.
				if (info.Status != CellStatus.Closed)
				{
					abstractSearch.TargetPredicate = c => c == abstractCell;
					if (!abstractSearch.ExpandToTarget())
						throw new Exception("The abstract path should never be searched for an unreachable point.");
					info = graph[abstractCell];
				}

				var abstractNode = info.PreviousNode;

				// When transitioning between layers, the XY will be the same and only the layer changes.
				// If we have transitioned layers then we need the next node
				// along otherwise we're measuring from our current location.
				if (abstractCell.Layer != abstractNode.Layer)
					abstractNode = graph[abstractNode].PreviousNode;

				// Now we have an abstract node to target, determine if there is one further along the path we can use.
				// This will provide a better estimate. Cache these results as they are expensive to calculate.
				if (!nodeForCostLookup.TryGetValue(abstractNode, out var abstractNodeForCost))
				{
					abstractNodeForCost = AbstractNodeForCost(graph, abstractCell, abstractNode);
					nodeForCostLookup.Add(abstractNode, abstractNodeForCost);
				}

				var cost = graph[abstractNodeForCost].CostSoFar + costEstimator(cell, abstractNodeForCost);
				return cost;
			};
		}

		/// <summary>
		/// Determines an abstract node further along the path which can be reached directly without deviating from the
		/// abstract path from the abstract cell of the source location.
		/// As this node can be reached directly we can target it instead
		/// of the original node to provide a better cost estimate.
		/// </summary>
		CPos AbstractNodeForCost(SparsePathGraph graph, CPos abstractCell, CPos abstractNode)
		{
			// We currently have the next abstract node along our path.
			// This means we can create a distance estimate for our current
			// cell to this next abstract node, plus then the cost provided by the abstract path.
			// | AC --- AN ---------------------------- D |
			// This means the lowest cost estimate follows the abstract path.
			// The abstract path becomes a sort of highway, units want to
			// move to it as soon as possible and follow it.
			// This causes issues when a unit can move in a straight line to the destination.
			// Instead of moving in a straight line, they move to join the highway, travel the highway,
			// then leave the highway at the other end.
			// We can combat this by delaying them from joining the highway until the abstract paths turns.
			// | AC ----------------- AN -------------- D |
			// This means the unit will move in a direct path whilst it can, and only "join the highway"
			// when the highway is going to navigate it around an obstacle.
			// When the heuristic weight is >100%, this greatly improves the resulting path.
			// As when the weight is higher we may never get a chance to check for the shorter direct path.
			var abstractNodesAlongPath = new List<CPos>();
			while (true)
			{
				var previousAbstractNode = graph[abstractNode].PreviousNode;

				// The whole abstract path has been travelled, can't go further.
				if (previousAbstractNode == abstractNode)
					break;

				// Check if we can move directly to the new node whilst staying
				// within the boundary of the abstract path so far.
				var intersectsAllNodes = true;
				abstractNodesAlongPath.Add(abstractNode);
				foreach (var node in abstractNodesAlongPath)
				{
					if (!GetGrid(node, mapBounds).IntersectsLine(abstractCell, previousAbstractNode))
					{
						intersectsAllNodes = false;
						break;
					}
				}

				if (!intersectsAllNodes)
					break;

				abstractNode = previousAbstractNode;
			}

			return abstractNode;
		}

		PathSearch GetLocalPathSearch(
			Actor self, IEnumerable<CPos> srcs, CPos dst, Func<CPos, int> customCost,
			Actor ignoreActor, BlockedByActor check, bool laneBias, Grid? grid, int heuristicWeightPercentage,
			Func<CPos, int> heuristic = null,
			bool inReverse = false,
			PathSearch.IRecorder recorder = null)
		{
			return PathSearch.ToTargetCell(
				world, locomotor, self, srcs, dst, check, heuristicWeightPercentage,
				customCost, ignoreActor, laneBias, inReverse, heuristic, grid, recorder);
		}
	}
}
