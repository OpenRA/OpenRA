#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.Common.MapGenerator
{
	/// <summary>Path to be tiled onto a map using MultiBrushSegments.</summary>
	public sealed class TilingPath
	{
		/// <summary>Describes the type and direction of the start or end of a TilingPath.</summary>
		public struct Terminal
		{
			public string Type;

			/// <summary>
			/// Direction to use for this terminal.
			/// If the direction here is null, it will be determined automatically later.
			/// </summary>
			public int? Direction;

			/// <summary>
			/// A string which can match the format used by MultiBrushSegment's Start or End.
			/// </summary>
			public readonly string SegmentType
			{
				get
				{
					var direction =
						Direction ?? throw new InvalidOperationException("Direction is null");
					return $"{Type}.{MapGenerator.Direction.ToString(direction)}";
				}
			}

			public Terminal(string type, int? direction)
			{
				Type = type;
				Direction = direction;
			}
		}

		/// <summary>
		/// Describes the permitted start, middle, and end segments/MultiBrushes that can be used
		/// to tile a path.
		/// </summary>
		public sealed class PermittedSegments
		{
			public readonly ImmutableArray<MultiBrush> Start;
			public readonly ImmutableArray<MultiBrush> Inner;
			public readonly ImmutableArray<MultiBrush> End;
			public IEnumerable<MultiBrush> All => Start.Union(Inner).Union(End);

			public PermittedSegments(
				IEnumerable<MultiBrush> start,
				IEnumerable<MultiBrush> inner,
				IEnumerable<MultiBrush> end)
			{
				Start = start.ToImmutableArray();
				Inner = inner.ToImmutableArray();
				End = end.ToImmutableArray();
			}

			public PermittedSegments(
				IReadOnlyList<MultiBrush> multiBrushes,
				IEnumerable<MultiBrush> all)
			{
				var array = all.ToImmutableArray();
				Start = array;
				Inner = array;
				End = array;
			}

			/// <summary>
			/// Creates a PermittedSegments using only the given types.
			/// </summary>
			public static PermittedSegments FromType(
				IReadOnlyList<MultiBrush> multiBrushes,
				IEnumerable<string> types)
				=> new(multiBrushes, FindSegments(multiBrushes, types));

			/// <summary>
			/// Creates a PermittedSegments suitable for a path with given inner and terminal types
			/// at the start and end.
			/// </summary>
			public static PermittedSegments FromInnerAndTerminalTypes(
				IReadOnlyList<MultiBrush> multiBrushes,
				IEnumerable<string> innerTypes,
				IEnumerable<string> terminalTypes)
			{
				var innerTypesArray = innerTypes.ToImmutableArray();
				var terminalTypesArray = terminalTypes.ToImmutableArray();
				return new(
					FindSegments(multiBrushes, terminalTypesArray, innerTypesArray, innerTypesArray),
					FindSegments(multiBrushes, innerTypesArray),
					FindSegments(multiBrushes, innerTypesArray, innerTypesArray, terminalTypesArray));
			}

			/// <summary>
			/// Equivalent to FindSegments(multiBrushes, types, types, types).
			/// </summary>
			public static IEnumerable<MultiBrush> FindSegments(
				IReadOnlyList<MultiBrush> multiBrushes,
				IEnumerable<string> types)
			{
				var array = types.ToImmutableArray();
				return FindSegments(multiBrushes, array, array, array);
			}

			/// <summary>
			/// Filter MultiBrushes to segments that use the given start, inner, and end types.
			/// </summary>
			public static IEnumerable<MultiBrush> FindSegments(
				IReadOnlyList<MultiBrush> multiBrushes,
				IEnumerable<string> startTypes,
				IEnumerable<string> innerTypes,
				IEnumerable<string> endTypes)
			{
				var filtered = new List<MultiBrush>();
				var startTypesArray = startTypes.ToImmutableArray();
				var innerTypesArray = innerTypes.ToImmutableArray();
				var endTypesArray = endTypes.ToImmutableArray();
				foreach (var multiBrush in multiBrushes)
					if (startTypesArray.Any(multiBrush.Segment.HasStartType) &&
						innerTypesArray.Any(multiBrush.Segment.HasInnerType) &&
						endTypesArray.Any(multiBrush.Segment.HasEndType))
					{
						filtered.Add(multiBrush);
					}

				return [.. filtered];
			}
		}

		public Map Map;

		/// <summary>
		/// <para>
		/// Target point sequence to fit MultiBrushSegments to. Whether these CPos positions
		/// represent cell corners or cell centers is dependent on the system used by the path's
		/// PermittedSegments' MultiBrushSegments.
		/// </para>
		/// <para>
		/// If null, Tiling will be a no-op. If non-null, must have at least two points.
		/// </para>
		/// <para>
		/// A loop must have the start and end points equal.
		/// </para>
		/// </summary>
		public CPos[] Points;

		/// <summary>
		/// Maximum permitted Chebyshev distance that layed MultiBrushSegments may be from the
		/// specified points.
		/// </summary>
		public int MaxDeviation;

		/// <summary>
		/// Determines how much corner-cutting is allowed.
		/// A value of zero will result in a value being derived from MaxDeviation.
		/// </summary>
		public int MaxSkip;

		/// <summary>
		/// Increases separation between permitted tiling regions of different parts of the path.
		/// </summary>
		public int MinSeparation;

		/// <summary>
		/// If the path cannot be tiled exactly, the resulting tiling is allowed to deviate from
		/// target end point by this Chebychev distance. Ignored for loops. This will be capped to
		/// MaxDeviation at tiling time.
		/// </summary>
		public int MaxEndDeviation;

		/// <summary>
		/// Stores start type and direction.
		/// </summary>
		public Terminal Start;

		/// <summary>
		/// Stores end type and direction.
		/// </summary>
		public Terminal End;
		public PermittedSegments Brushes;

		/// <summary>Whether the start and end points are the same.</summary>
		public bool IsLoop
		{
			get => Points != null && Points[0] == Points[^1];
		}

		public TilingPath(
			Map map,
			CPos[] points,
			int maxDeviation,
			string startType,
			string endType,
			PermittedSegments permittedTemplates)
		{
			Map = map;
			Points = points;
			MaxDeviation = maxDeviation;
			MaxSkip = 0;
			MinSeparation = 0;
			MaxEndDeviation = 0;
			Start = new Terminal(startType, null);
			End = new Terminal(endType, null);
			Brushes = permittedTemplates;
		}

		sealed class TilingSegment
		{
			public readonly MultiBrush MultiBrush;
			public readonly int StartTypeId;
			public readonly int EndTypeId;
			public readonly CVec Offset;
			public readonly CVec Moves;
			public readonly CVec[] RelativePoints;
			public readonly int[] Directions;
			public readonly int[] DirectionMasks;
			public readonly int[] ReverseDirectionMasks;

			public TilingSegment(MultiBrush multiBrush, int startId, int endId)
			{
				MultiBrush = multiBrush;
				StartTypeId = startId;
				EndTypeId = endId;
				Offset = multiBrush.Segment.Points[0];
				Moves = multiBrush.Segment.Points[^1] - Offset;
				RelativePoints = multiBrush.Segment.Points
					.Select(p => p - multiBrush.Segment.Points[0])
					.ToArray();

				Directions = new int[RelativePoints.Length];
				DirectionMasks = new int[RelativePoints.Length];
				ReverseDirectionMasks = new int[RelativePoints.Length];

				// Last point has no direction.
				Directions[^1] = Direction.None;
				DirectionMasks[^1] = 0;
				ReverseDirectionMasks[^1] = 0;
				for (var i = 0; i < RelativePoints.Length - 1; i++)
				{
					var direction = Direction.FromCVec(RelativePoints[i + 1] - RelativePoints[i]);
					if (direction == Direction.None)
						throw new ArgumentException("MultiBrushSegment has duplicate points in sequence");
					Directions[i] = direction;
					DirectionMasks[i] = 1 << direction;
					ReverseDirectionMasks[i] = 1 << Direction.Reverse(direction);
				}
			}
		}

		/// <summary>
		/// <para>
		/// Attempt to tile the given path, producing a new MultiBrush if the path could be tiled,
		/// or null if the path could not be tiled within constraints.
		/// </para>
		/// <para>
		/// The resulting MultiBrush is created from stitching MultiBrushes from the
		/// PermittedSegments together, and will contain a segment that represents the stitched
		/// segments of the constituent MultiBrushes.
		/// </para>
		/// </summary>
		public MultiBrush Tile(MersenneTwister random)
		{
			// This is essentially a Dijkstra's algorithm best-first search.
			//
			// The search is performed over a 3-dimensional space: (x, y, connection type).
			// Connection types correspond to the .Start or .End values of MultiBrushSegments.
			//
			// The best found costs of the nodes in this space are stored as an array of matrices.
			// There is a matrix for each possible connection type, and each matrix stores the
			// (current) best costs at the (x, y) locations for that given connection type.
			//
			// The directed edges between the nodes of this 3-dimensional space are defined by the
			// MultiBrushSegments within the permitted set of segments. For example, a segment
			// defined as
			//
			//   Segment:
			//       Start: Beach.L
			//       End: Beach.D
			//       Points: 3,1, 2,1, 2,2, 2,3
			//
			// may connect a node from (10, 10) in the "Beach.L" matrix to node (9, 12) in the
			// "Beach.D" matrix. (The overall point displacement is (2,3) - (3,1) = (-1, +2))
			//
			// The cost of a transition/link/edge between nodes is defined by how well the
			// MultiBrushSegment fits the path (how little "deviation" is accumulates). However, in
			// order for a transition to be allowed at all, it must satisfy some constraints:
			//
			// - It must not regress backward along the path (but no immediate progress is OK).
			// - It must not deviate at any point in the segment beyond MaxDeviation from the path.
			// - It must not skip to much later path points (which may be within MaxDeviation).
			//
			// Progress is measured as a combo of both the earliest and latest closest path points.
			//
			// The search is conducted from the path start node until the best possible cost of
			// the end node is confirmed. This also populates possible intermediate nodes' costs.
			//
			// If the original target end node is unreachable (at MaxCost), a nearby node may be
			// selected as a fallback end point, provided the target path isn't a loop.
			//
			// Then, from the end node, it works backwards. It finds any (random) suitable
			// segment which connects back to a previous node where the difference in cost is
			// that of the segment's cost, implying that the previous node is on an
			// optimal path towards the end node. This process repeats until the start node is
			// reached, merging MultiBrushes into a result along the way.
			//
			// Note that this algorithm makes a few (reasonable) assumptions about the shapes of
			// MultiBrushes, such as that they don't individually snake around too much. The actual
			// tiles of a MultiBrush are ignored during the search, with only the segment being
			// used to calculate transition cost and validity.
			if (Points == null)
				return null;

			var start = Start;
			var end = End;
			start.Direction ??= Direction.FromCVec(Points[1] - Points[0]);
			end.Direction ??= Direction.FromCVec(IsLoop ? Points[1] - Points[0] : Points[^1] - Points[^2]);

			var maxSkip = MaxSkip > 0 ? MaxSkip : (2 * MaxDeviation + 1);

			var scanRange = MaxDeviation + MinSeparation;
			var minPoint = new CPos(
				Points.Min(p => p.X) - scanRange,
				Points.Min(p => p.Y) - scanRange);
			var maxPoint = new CPos(
				Points.Max(p => p.X) + scanRange,
				Points.Max(p => p.Y) + scanRange);
			var points = Points
				.Select(point => point - minPoint)
				.ToArray();

			var isLoop = IsLoop;

			// grid points (not squares), so these are offset 0.5 from tile centers.
			var size = new int2(1 + maxPoint.X - minPoint.X, 1 + maxPoint.Y - minPoint.Y);
			var sizeXY = size.X * size.Y;

			const int OverDeviation = int.MaxValue;
			const int InvalidProgress = int.MaxValue;

			// How far away from the path this point is.
			var deviations = new Matrix<int>(size).Fill(OverDeviation);

			var lowProgress = new Matrix<int>(size).Fill(InvalidProgress);
			var highProgress = new Matrix<int>(size).Fill(InvalidProgress);

			var progressModulus = IsLoop ? points.Length - 1 : points.Length;

			// The following only apply to looped paths
			var forwardProgressLimit = (progressModulus + 1) / 2;
			var backwardProgressLimit = progressModulus / 2;

			// MinValue essentially means "never match me".
			var oppositeProgress =
				(IsLoop && forwardProgressLimit == backwardProgressLimit)
					? forwardProgressLimit
					: int.MinValue;

			// Find the progress difference of two progress values. For loops high progress values
			// wrap around to low ones. (Think of loops' progress like a 24 hour clock,
			// where 22 -> 2 is a difference of 4, and 2 -> 22 is a difference of -4).
			int Progress(int from, int to)
			{
				if (IsLoop)
				{
					var progress = (progressModulus + to - from) % progressModulus;
					if (progress < forwardProgressLimit)
						return progress;
					else if (progress > backwardProgressLimit)
						return progress - progressModulus;
					else
						return oppositeProgress;
				}
				else
				{
					return to - from;
				}
			}

			{
				var progressSeeds = new List<(int2, int)>();
				for (var pointI = 0; pointI < progressModulus; pointI++)
				{
					var point = points[pointI];
					lowProgress[point.X, point.Y] = pointI;
					highProgress[point.X, point.Y] = pointI;
					progressSeeds.Add((new int2(point.X, point.Y), 0));
				}

				(int Low, int High) FindLowAndHigh(List<int> values)
				{
					if (values.Count == 0)
						return (InvalidProgress, InvalidProgress);

					if (values.Count == 1)
						return (values[0], values[0]);

					if (IsLoop)
					{
						// For loops, with a list of 2+ sorted progress values, there are 2 cases:
						// - The values are spatially grouped, such that the values are contained
						//   in under a half of the progress range, and the largest gap between
						//   values is more than half of the progress range. This means that going
						//   from before the gap to after it is an overall negative progress
						//   change. (It must be the only negative progress change one as there can
						//   only be one gap that is over half of the progress range.) In this
						//   case, there is an obvious start and end to the group, with an overall
						//   positive progress change.
						// - The values are dispersed such that there is no obvious start or end.
						if (Progress(values[^1], values[0]) < 0)
							return (values[0], values[^1]);
						for (var i = 0; i < values.Count - 1; i++)
							if (Progress(values[i], values[i + 1]) < 0)
								return (values[i + 1], values[i]);

						return (InvalidProgress, InvalidProgress);
					}
					else
					{
						return (values[0], values[^1]);
					}
				}

				var lows = new List<int>(8);
				var highs = new List<int>(8);
				int? ProgressFiller(int2 xy, int deviation)
				{
					if (deviations[xy] != OverDeviation)
						return null;

					deviations[xy] = deviation;

					// low and high progress is preset for 0-deviation.
					if (deviation == 0)
						return 1;

					lows.Clear();
					highs.Clear();
					foreach (var offset in Direction.Spread8)
					{
						var neighbor = xy + offset;
						if (!deviations.ContainsXY(neighbor) ||
							deviations[neighbor] >= deviation ||
							lowProgress[neighbor] == InvalidProgress ||
							highProgress[neighbor] == InvalidProgress)
						{
							continue;
						}

						lows.Add(lowProgress[neighbor]);
						highs.Add(highProgress[neighbor]);
					}

					lows.Sort();
					highs.Sort();
					(lowProgress[xy], _) = FindLowAndHigh(lows);
					(_, highProgress[xy]) = FindLowAndHigh(highs);

					if (deviation == scanRange)
						return null;

					return deviation + 1;
				}

				MatrixUtils.FloodFill(
					size,
					progressSeeds,
					ProgressFiller,
					Direction.Spread8);

				var separationSeeds = new List<(int2, int)>();

				for (var y = 0; y < size.Y; y++)
					for (var x = 0; x < size.X; x++)
					{
						var xy = new int2(x, y);
						var low = lowProgress[xy];
						var high = highProgress[xy];
						if (low == InvalidProgress ||
							high == InvalidProgress)
						{
							separationSeeds.Add((xy, MinSeparation));
							continue;
						}

						if (MinSeparation > 0)
						{
							foreach (var offset in Direction.Spread8)
							{
								var neighbor = xy + offset;
								if (!deviations.ContainsXY(neighbor) ||
									Math.Abs(Progress(low, lowProgress[neighbor])) > maxSkip ||
									Math.Abs(Progress(high, highProgress[neighbor])) > maxSkip)
								{
									separationSeeds.Add((xy, MinSeparation - 1));
									break;
								}
							}

							// Last so that any greater range seeds take priority.
							if (deviations[xy] > MaxDeviation)
								separationSeeds.Add((xy, 0));
						}
					}

				int? SeparationFiller(int2 xy, int range)
				{
					if (deviations[xy] == 0 || deviations[xy] == OverDeviation)
						return null;
					deviations[xy] = OverDeviation;
					if (range == 0)
						return null;
					return range - 1;
				}

				MatrixUtils.FloodFill(
					size,
					separationSeeds,
					SeparationFiller,
					Direction.Spread8);
			}

			var pathStart = points[0];
			var pathEnd = points[^1];
			var orderedPermittedBrushes = Brushes.All.ToImmutableArray();
			var permittedBrushes = orderedPermittedBrushes.ToImmutableHashSet();
			var permittedStartBrushes = Brushes.Start.ToImmutableHashSet();
			var permittedInnerBrushes = Brushes.Inner.ToImmutableHashSet();
			var permittedEndBrushes = Brushes.End.ToImmutableHashSet();

			const int MaxCost = int.MaxValue;
			var segmentTypeToId = new Dictionary<string, int>();
			var segmentsByStart = new List<List<(TilingSegment Segment, bool CanStart, bool CanInner, bool CanEnd)>>();
			var segmentsByEnd = new List<List<(TilingSegment Segment, bool CanStart, bool CanInner, bool CanEnd)>>();

			// We store the end costs of valid end segments separately to inner costs.
			//
			// Note also that:
			// - The start cost is always zero and only applies to a single node.
			// - Permitted end and inner segments may be distinct, but the end terminal could exist
			//   in the permitted inner segments and shouldn't be a valid intermediate cost.
			// - Avoids confusing start, inner, and end costs when processing looped paths.
			// - We may be interested in multiple end costs if MaxEndDeviation is non-zero.
			var endCosts = new Matrix<int>(size).Fill(MaxCost);
			var innerCosts = new List<Matrix<int>>();
			{
				void RegisterSegmentType(string type)
				{
					if (segmentTypeToId.ContainsKey(type))
						return;
					var newId = segmentTypeToId.Count;
					segmentTypeToId.Add(type, newId);
					segmentsByStart.Add([]);
					segmentsByEnd.Add([]);
					innerCosts.Add(new Matrix<int>(size).Fill(MaxCost));
				}

				foreach (var multiBrush in orderedPermittedBrushes)
				{
					var segment = multiBrush.Segment;
					RegisterSegmentType(segment.Start);
					RegisterSegmentType(segment.End);
					var startTypeId = segmentTypeToId[segment.Start];
					var endTypeId = segmentTypeToId[segment.End];
					var tilePathSegment = new TilingSegment(multiBrush, startTypeId, endTypeId);
					var tuple = (
						tilePathSegment,
						permittedStartBrushes.Contains(multiBrush) && segment.Start == start.SegmentType,
						permittedInnerBrushes.Contains(multiBrush),
						permittedEndBrushes.Contains(multiBrush) && segment.End == end.SegmentType);
					segmentsByStart[startTypeId].Add(tuple);
					segmentsByEnd[endTypeId].Add(tuple);
				}
			}

			var totalTypeIds = segmentTypeToId.Count;

			var priorities = new PriorityArray<int>(totalTypeIds * size.X * size.Y, MaxCost);
			void SetPriorityAt(int typeId, CVec pos, int priority)
				=> priorities[typeId * sizeXY + pos.Y * size.X + pos.X] = priority;
			(int TypeId, CVec Pos, int Priority) GetNextPriority()
			{
				var index = priorities.GetMinIndex();
				var priority = priorities[index];
				var typeId = index / sizeXY;
				var xy = index % sizeXY;
				return (typeId, new CVec(xy % size.X, xy / size.X), priority);
			}

			var pathStartTypeId = segmentTypeToId[start.SegmentType];
			var pathEndTypeId = segmentTypeToId[end.SegmentType];

			// Lower (closer to zero) costs are better matches.
			// MaxScore means totally unacceptable.
			int ScoreSegment(TilingSegment segment, CVec from)
			{
				var to = from + segment.Moves;

				if (isLoop && to != pathEnd && lowProgress[from.X, from.Y] > highProgress[to.X, to.Y] && highProgress[to.X, to.Y] != 0)
				{
					// We've missed the start/end of the loop and have potentially gone past it
					// (as far as low and high progress are concerned).
					return MaxCost;
				}

				var deviationAcc = 0;
				var lowProgressionAcc = 0;
				var highProgressionAcc = 0;
				var lastPointI = segment.RelativePoints.Length - 1;
				for (var pointI = 0; pointI <= lastPointI; pointI++)
				{
					var point = from + segment.RelativePoints[pointI];
					if (!deviations.ContainsXY(point.X, point.Y) || deviations[point.X, point.Y] == OverDeviation)
					{
						// Point escapes bounds or is in an excluded position.
						return MaxCost;
					}

					if (pointI < lastPointI)
					{
						var pointNext = from + segment.RelativePoints[pointI + 1];
						if (!deviations.ContainsXY(pointNext.X, pointNext.Y) || deviations[pointNext.X, pointNext.Y] == OverDeviation)
						{
							// Next point escapes bounds or is in an excluded position.
							return MaxCost;
						}

						var lowProgression = Progress(lowProgress[point.X, point.Y], lowProgress[pointNext.X, pointNext.Y]);
						var highProgression = Progress(highProgress[point.X, point.Y], highProgress[pointNext.X, pointNext.Y]);
						if (Math.Abs(lowProgression) > maxSkip ||
							Math.Abs(highProgression) > maxSkip)
						{
							// Fails skip rule.
							return MaxCost;
						}

						lowProgressionAcc += lowProgression;
						highProgressionAcc += highProgression;
					}

					// pointI > 0 is needed to avoid double-counting the segments's start with the
					// previous one's end.
					if (pointI > 0)
						deviationAcc += deviations[point.X, point.Y];
				}

				if (lowProgressionAcc < 0 || highProgressionAcc < 0)
				{
					// Fails progression rule.
					return MaxCost;
				}

				// Satisfies all requirements.
				return deviationAcc;
			}

			void UpdateFrom(CVec from, int fromTypeId, bool isForStart)
			{
				var fromCost = isForStart ? 0 : innerCosts[fromTypeId][from.X, from.Y];

				foreach (var (segment, canStart, canInner, canEnd) in segmentsByStart[fromTypeId])
				{
					if (isForStart)
					{
						if (!canStart)
							continue;
					}
					else
					{
						if (!(canEnd || canInner))
							continue;
					}

					var to = from + segment.Moves;
					if (to.X < 0 || to.X >= size.X || to.Y < 0 || to.Y >= size.Y)
						continue;

					// Most likely to fail. Check first.
					if (deviations[to.X, to.Y] == OverDeviation)
					{
						// End escapes bounds.
						continue;
					}

					var segmentCost = ScoreSegment(segment, from);
					if (segmentCost == MaxCost)
						continue;

					var toCost = fromCost + segmentCost;
					var toTypeId = segment.EndTypeId;

					if ((canStart || canInner) && toCost < innerCosts[toTypeId][to.X, to.Y])
					{
						innerCosts[toTypeId][to.X, to.Y] = toCost;
						SetPriorityAt(toTypeId, to, toCost);
					}

					if (canEnd && toCost < endCosts[to.X, to.Y])
						endCosts[to.X, to.Y] = toCost;
				}

				SetPriorityAt(fromTypeId, from, MaxCost);
			}

			UpdateFrom(pathStart, pathStartTypeId, true);

			while (true)
			{
				var (fromTypeId, from, priority) = GetNextPriority();

				if (priority == MaxCost)
					break;

				UpdateFrom(from, fromTypeId, false);
			}

			// Trace back and update tiles
			var resultPoints = new List<CVec>();

			var compositeBrush = new MultiBrush();

			(CVec From, int FromTypeId) TraceBackStep(CVec to, int toTypeId, bool isForEnd)
			{
				var toCost = isForEnd ? endCosts[to.X, to.Y] : innerCosts[toTypeId][to.X, to.Y];
				var candidates = new List<TilingSegment>();
				foreach (var (segment, canStart, canInner, canEnd) in segmentsByEnd[toTypeId])
				{
					if (isForEnd)
					{
						if (!canEnd)
							continue;
					}
					else
					{
						if (!(canStart || canInner))
							continue;
					}

					var from = to - segment.Moves;
					var mustStart =
						from == pathStart && segment.StartTypeId == pathStartTypeId;

					if (mustStart && !canStart)
						continue;

					if (from.X < 0 || from.X >= size.X || from.Y < 0 || from.Y >= size.Y)
						continue;

					// Most likely to fail. Check first.
					if (deviations[from.X, from.Y] == OverDeviation)
					{
						// Start escapes bounds.
						continue;
					}

					var segmentCost = ScoreSegment(segment, from);
					if (segmentCost == MaxCost)
						continue;

					var fromCost = toCost - segmentCost;
					var requiredFromCost =
						mustStart ? 0 : innerCosts[segment.StartTypeId][from.X, from.Y];
					if (fromCost == requiredFromCost)
						candidates.Add(segment);
				}

				Debug.Assert(candidates.Count >= 1, "TraceBack didn't find an original route");
				var chosenSegment = candidates[random.Next(candidates.Count)];
				var chosenFrom = to - chosenSegment.Moves;
				compositeBrush.MergeFrom(
					chosenSegment.MultiBrush,
					chosenFrom - chosenSegment.Offset + minPoint - CPos.Zero,
					Map.Grid.Type);

				// Skip end point as it is recorded in the previous segment.
				for (var i = chosenSegment.RelativePoints.Length - 2; i >= 0; i--)
				{
					var point = chosenFrom + chosenSegment.RelativePoints[i] + minPoint - CPos.Zero;
					resultPoints.Add(point);
				}

				return (chosenFrom, chosenSegment.StartTypeId);
			}

			{
				var toTypeId = pathEndTypeId;

				if (endCosts[pathEnd.X, pathEnd.Y] == MaxCost)
				{
					// There isn't a tiling solution to the exact target end point. If enabled,
					// search for an alternative, nearby end point.
					var maxEndDeviation = Math.Min(MaxEndDeviation, MaxDeviation);
					if (maxEndDeviation == 0 || isLoop)
						return null;

					// Find the closest points which are near the original target end point and
					// have a tiling solution.
					const int Unreached = int.MaxValue;
					const int Unsolved = int.MaxValue - 1;
					var fallbackDistances =
						new Matrix<int>(maxEndDeviation * 2 + 1, maxEndDeviation * 2 + 1)
							.Fill(Unreached);

					int? FallbacksFiller(int2 xy, int distance)
					{
						if (fallbackDistances[xy] != Unreached)
							return null;

						var p = new int2(pathEnd.X - maxEndDeviation, pathEnd.Y - maxEndDeviation) + xy;

						if (!deviations.ContainsXY(p.X, p.Y) || deviations[p.X, p.Y] == OverDeviation)
						{
							fallbackDistances[xy] = Unsolved;
							return null;
						}

						fallbackDistances[xy] =
							endCosts[p.X, p.Y] != MaxCost ? distance : Unsolved;
						return distance + 1;
					}

					MatrixUtils.FloodFill(
						fallbackDistances.Size,
						[(new int2(maxEndDeviation, maxEndDeviation), 0)],
						FallbacksFiller,
						Direction.Spread4);

					var bestDistance = fallbackDistances.Data.Min();
					if (bestDistance == Unreached || bestDistance == Unsolved)
						return null;

					// Find the lowest cost candidate end point.
					var fallbackCosts = new Matrix<int>(maxEndDeviation * 2 + 1, maxEndDeviation * 2 + 1);
					for (var y = -maxEndDeviation; y <= maxEndDeviation; y++)
						for (var x = -maxEndDeviation; x <= maxEndDeviation; x++)
						{
							var fallbackXy = new int2(x + maxEndDeviation, y + maxEndDeviation);
							var p = new int2(x + pathEnd.X, y + pathEnd.Y);
							fallbackCosts[fallbackXy] =
								(fallbackDistances[fallbackXy] == bestDistance) ? endCosts[p] : MaxCost;
						}

					var (chosenXy, _) = MatrixUtils.FindRandomBest(
						fallbackCosts,
						random,
						(a, b) => b.CompareTo(a));

					pathEnd = new CVec(chosenXy.X - maxEndDeviation, chosenXy.Y - maxEndDeviation) + pathEnd;
				}

				var to = pathEnd;

				resultPoints.Add(new(to.X + minPoint.X, to.Y + minPoint.Y));

				(to, toTypeId) = TraceBackStep(to, toTypeId, true);

				// No need to check direction. If that is an issue, I have bigger problems to worry about.
				while (to != pathStart)
					(to, toTypeId) = TraceBackStep(to, toTypeId, false);
			}

			// Traced back in reverse, so reverse the reversal.
			resultPoints.Reverse();
			var compositeSegment = new MultiBrushSegment(
				start.SegmentType,
				"(Tiled Path)",
				end.SegmentType,
				[.. resultPoints]);

			compositeBrush.ReplaceSegment(compositeSegment);

			return compositeBrush;
		}

		/// <summary>
		/// <para>
		/// Extend the start and end of a path by extensionLength points. The directions of the
		/// extensions are based on the overall direction of the outermost inertialRange points.
		/// </para>
		/// <para>
		/// Returns the object being called on.
		/// </para>
		/// </summary>
		public TilingPath InertiallyExtend(int extensionLength, int inertialRange)
		{
			Points = InertiallyExtendPathPoints(Points, extensionLength, inertialRange);
			return this;
		}

		/// <summary>
		/// Extend the start and end of a path by extensionLength points. The directions of the
		/// extensions are based on the overall direction of the outermost inertialRange points.
		/// Loops are left unmodified.
		/// </summary>
		public static CPos[] InertiallyExtendPathPoints(CPos[] points, int extensionLength, int inertialRange)
		{
			if (points == null)
				return null;

			if (points[0] == points[^1])
			{
				// Is a loop.
				return points;
			}

			if (inertialRange > points.Length - 1)
				inertialRange = points.Length - 1;
			var sd = Direction.FromCVecNonDiagonal(points[inertialRange] - points[0]);
			var ed = Direction.FromCVecNonDiagonal(points[^1] - points[^(inertialRange + 1)]);
			var newPoints = new CPos[points.Length + extensionLength * 2];

			for (var i = 0; i < extensionLength; i++)
				newPoints[i] = points[0] - Direction.ToCVec(sd) * (extensionLength - i);

			Array.Copy(points, 0, newPoints, extensionLength, points.Length);

			for (var i = 0; i < extensionLength; i++)
				newPoints[extensionLength + points.Length + i] = points[^1] + Direction.ToCVec(ed) * (i + 1);

			return newPoints;
		}

		/// <summary>
		/// <para>
		/// For map edge-connected (non-loop) starts/ends, the path is extended beyond the edge.
		/// For loops or paths which don't connect to the map edge, no change is applied.
		/// </para>
		/// <para>
		/// For the purposes of this function, the map edges are defined as the borders of a
		/// minimal CPos-aligned rectangle covering the entire map. These are not the true edges of
		/// a RectangularIsometric map.
		/// </para>
		/// <para>
		/// Starts/ends which are corner-connected or already extend beyond the edge are unaltered.
		/// </para>
		/// <para>
		/// Returns the object being called on.
		/// </para>
		/// </summary>
		public TilingPath ExtendEdge(int extensionLength)
		{
			Points = ExtendEdgePathPoints(Points, CellLayerUtils.CellBounds(Map), extensionLength);
			return this;
		}

		/// <summary>
		/// <para>
		/// For bounds edge-connected (non-loop) starts/ends, the path is extended beyond the edge.
		/// For loops or paths which don't connect to the edges, the input points are returned
		/// unaltered.
		/// </para>
		/// <para>
		/// Starts/ends which are corner-connected or already extend beyond the edge are unaltered.
		/// </para>
		/// </summary>
		public static CPos[] ExtendEdgePathPoints(CPos[] points, Rectangle bounds, int extensionLength)
		{
			if (points == null)
				return null;

			if (points[0] == points[^1])
			{
				// Is a loop.
				return points;
			}

			var left = bounds.Left;
			var top = bounds.Top;
			var right = bounds.Right;
			var bottom = bounds.Bottom;

			CPos[] Extend(CPos point)
			{
				var ox = (point.X == left) ? -1
					: (point.X == right) ? 1
					: 0;
				var oy = (point.Y == top) ? -1
					: (point.Y == bottom) ? 1
					: 0;
				if (ox == oy)
				{
					// We're either not on an edge or we're at a corner, so don't extend.
					return [];
				}

				var offset = new CVec(ox, oy);

				var extension = new CPos[extensionLength];
				var newPoint = point;
				for (var i = 0; i < extensionLength; i++)
				{
					newPoint += offset;
					extension[i] = newPoint;
				}

				return extension;
			}

			// Open paths. Extend if beyond edges.
			var startExt = Extend(points[0]).Reverse().ToArray();
			var endExt = Extend(points[^1]);

			// [...startExt, ...points, ...endExt];
			var tweaked = new CPos[points.Length + startExt.Length + endExt.Length];
			Array.Copy(startExt, 0, tweaked, 0, startExt.Length);
			Array.Copy(points, 0, tweaked, startExt.Length, points.Length);
			Array.Copy(endExt, 0, tweaked, points.Length + startExt.Length, endExt.Length);
			return tweaked;
		}

		/// <summary>
		/// <para>
		/// For loops, points are rotated such that the start/end reside in the longest straight.
		/// For non-loops, the input points are returned unaltered.
		/// </para>
		/// <para>
		/// Returns the object being called on.
		/// </para>
		/// </summary>
		public TilingPath OptimizeLoop()
		{
			Points = OptimizeLoopPathPoints(Points);
			return this;
		}

		/// <summary>
		/// For loops, points are rotated such that the start/end reside in the longest straight.
		/// For non-loops, the input points are returned unaltered.
		/// </summary>
		public static CPos[] OptimizeLoopPathPoints(CPos[] points)
		{
			if (points == null)
				return null;

			if (points[0] == points[^1])
			{
				// Closed loop. Find the longest straight
				// (nrlen excludes the repeated point at the end.)
				var nrlen = points.Length - 1;
				var prevDim = -1;
				var scanStart = -1;
				var bestScore = -1;
				var bestBend = -1;
				var prevBend = -1;
				var prevI = 0;
				for (var i = 1; ; i++)
				{
					if (i == nrlen)
						i = 0;
					var dim = points[i].X == points[prevI].X ? 1 : 0;
					if (prevDim != -1 && prevDim != dim)
					{
						if (scanStart == -1)
						{
							// This is technically just after the bend. But that's fine.
							scanStart = i;
						}
						else
						{
							var score = prevI - prevBend;
							if (score < 0)
								score += nrlen;

							if (score > bestScore)
							{
								bestBend = prevBend;
								bestScore = score;
							}

							if (i == scanStart)
								break;
						}

						prevBend = prevI;
					}

					prevDim = dim;
					prevI = i;
				}

				var favouritePoint = (bestBend + (bestScore >> 1)) % nrlen;

				// Repeat the start at the end.
				// [...points.slice(favouritePoint, nrlen), ...points.slice(0, favouritePoint + 1)];
				var tweaked = new CPos[points.Length];
				Array.Copy(points, favouritePoint, tweaked, 0, nrlen - favouritePoint);
				Array.Copy(points, 0, tweaked, nrlen - favouritePoint, favouritePoint + 1);
				return tweaked;
			}
			else
			{
				return points;
			}
		}

		/// <summary>
		/// <para>
		/// Shrink a path by a given amount at both ends. If the number of points in the path drops
		/// below minimumLength, the path is nullified.
		/// </para>
		/// <para>
		/// If a loop is provided, the path is not shrunk, but the minimumLength requirement still
		/// holds.
		/// </para>
		/// <para>
		/// Returns the object being called on.
		/// </para>
		/// </summary>
		public TilingPath Shrink(int shrinkBy, int minimumLength)
		{
			Points = ShrinkPathPoints(Points, shrinkBy, minimumLength);
			return this;
		}

		/// <summary>
		/// <para>
		/// Shrink a path by a given amount at both ends. If the number of points in the path drops
		/// below minimumLength, null is returned.
		/// </para>
		/// <para>
		/// If a loop is provided, the path is not shrunk, but the minimumLength requirement still
		/// holds.
		/// </para>
		/// </summary>
		public static CPos[] ShrinkPathPoints(CPos[] points, int shrinkBy, int minimumLength)
		{
			if (points == null)
				return null;

			if (minimumLength <= 1)
				throw new ArgumentException("minimumLength must be greater than 1");

			if (points[0] == points[^1])
			{
				// Loop.
				if (points.Length < minimumLength)
					return null;
				return points[0..^0];
			}

			if (points.Length < shrinkBy * 2 + minimumLength)
				return null;
			return points[shrinkBy..(points.Length - shrinkBy)];
		}

		/// <summary>
		/// <para>
		/// Takes a path and normalizes its progression direction around the map center.
		/// Normalized but opposing paths rotate around the center in the same direction.
		/// </para>
		/// <para>
		/// The measureFromCenter function must convert CVec positions to WVec offsets from the map
		/// center.
		/// </para>
		/// </summary>
		public TilingPath ChirallyNormalize(Func<CPos, WVec> measureFromCenter)
		{
			Points = ChirallyNormalizePathPoints(Points, measureFromCenter);
			return this;
		}

		/// <summary>
		/// <para>
		/// Takes a path and normalizes its progression direction around the map center.
		/// Normalized but opposing paths rotate around the center in the same direction.
		/// </para>
		/// <para>
		/// Loops are normalized to rotate in a consistent direction, regardless of position.
		/// </para>
		/// <para>
		/// The measureFromCenter function must convert CVec positions to WVec offsets from the map
		/// center.
		/// </para>
		/// </summary>
		public static CPos[] ChirallyNormalizePathPoints(CPos[] points, Func<CPos, WVec> measureFromCenter)
		{
			if (points == null || points.Length < 2)
				return points;

			var normalized = (CPos[])points.Clone();
			var start = points[0];
			var end = points[^1];

			if (start == end)
			{
				// Is a loop.
				// Find the top-left-most corner point (on the convex hull) and
				// sample which way the points are bending.
				var topLeftIndex = 0;
				var topLeftPoint = points[0];
				for (var i = 1; i < points.Length; i++)
				{
					var point = points[i];
					if (point.Y < topLeftPoint.Y || (point.Y == topLeftPoint.Y && point.X < topLeftPoint.X))
					{
						topLeftIndex = i;
						topLeftPoint = point;
					}
				}

				var inOffset = points[topLeftIndex] - points[(topLeftIndex + points.Length - 1) % points.Length];
				var outOffset = points[(topLeftIndex + points.Length + 1) % points.Length] - points[topLeftIndex];
				var crossProd = inOffset.X * outOffset.Y - inOffset.Y * outOffset.X;

				// crossProd should never be 0 for a valid input.
				if (crossProd < 0)
					Array.Reverse(normalized);
			}
			else
			{
				// Is not a loop.
				bool ShouldReverse(CPos start, CPos end)
				{
					var v1 = measureFromCenter(start);
					var v2 = measureFromCenter(end);

					// Rotation around center?
					var crossProd = v1.X * v2.Y - v2.X * v1.Y;
					if (crossProd != 0)
						return crossProd < 0;

					// Distance from center?
					var r1 = v1.X * v1.X + v1.Y * v1.Y;
					var r2 = v2.X * v2.X + v2.Y * v2.Y;
					if (r1 != r2)
						return r1 < r2;

					// Absolute angle
					return v1.Y == v2.Y ? v1.X > v2.X : v1.Y > v2.Y;
				}

				if (ShouldReverse(start, end))
					Array.Reverse(normalized);
			}

			return normalized;
		}

		/// <summary>
		/// <para>
		/// Retains paths which have no points in common with earlier (previous and retained) paths
		/// from the input.
		/// </para>
		/// <para>
		/// The underlying point sequences are NOT cloned.
		/// </para>
		/// <para>
		/// All input sequences must be non-null.
		/// </para>
		/// </summary>
		public static CPos[][] RetainDisjointPaths(IEnumerable<CPos[]> inputs)
		{
			var outputs = new List<CPos[]>();
			var lookup = new HashSet<CPos>();
			foreach (var points in inputs)
			{
				var retain = true;
				foreach (var point in points)
				{
					if (lookup.Contains(point))
					{
						retain = false;
						break;
					}
				}

				if (retain)
				{
					outputs.Add(points);
					foreach (var point in points)
						lookup.Add(point);
				}
			}

			return outputs.ToArray();
		}

		/// <summary>Nullify the path's points if they aren't suitable for tiling.</summary>
		public TilingPath RetainIfValid()
		{
			if (!ValidatePathPoints(Points))
				Points = null;

			return this;
		}

		public static bool ValidatePathPoints(CPos[] points)
		{
			if (points == null || points.Length == 0)
				return false;

			var isLoop = points[0] == points[^1];

			if (points.Length < (isLoop ? 3 : 2))
				return false;

			// Duplicate points check
			if (points.Distinct().Count() != points.Length - (isLoop ? 1 : 0))
				return false;

			// All steps must be (non-diagonal) unit offsets.
			var lastPoint = points[0];
			for (var i = 1; i < points.Length; i++)
			{
				var offset = lastPoint - points[i];
				if (Direction.ToCVec(Direction.FromCVecNonDiagonal(offset)) != offset)
					return false;

				lastPoint = points[i];
			}

			return true;
		}

		/// <summary>Set MaxEndDeviation.</summary>
		public TilingPath SetMaxEndDeviation(int maxEndDeviation)
		{
			MaxEndDeviation = maxEndDeviation;
			return this;
		}

		/// <summary>Allow end point deviation as far as MaxDeviation will allow.</summary>
		public TilingPath SetAutoEndDeviation()
		{
			MaxEndDeviation = int.MaxValue;
			return this;
		}
	}
}
