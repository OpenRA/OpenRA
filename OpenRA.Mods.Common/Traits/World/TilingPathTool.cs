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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	[IncludeStaticFluentReferences(typeof(TilingPathTool))]
	public sealed class TilingPathToolInfo : TraitInfo
	{
		[Desc("The preferred defaults for the start type.")]
		public readonly ImmutableArray<string> DefaultStart = [];
		[Desc("The preferred defaults for the inner type.")]
		public readonly ImmutableArray<string> DefaultInner = [];
		[Desc("The preferred defaults for the end type.")]
		public readonly ImmutableArray<string> DefaultEnd = [];

		public override object Create(ActorInitializer init)
		{
			return new TilingPathTool(init.Self, this);
		}
	}

	public sealed class TilingPathTool : IEditorTool, IRenderAnnotations, INotifyActorDisposing, IWorldLoaded
	{
		[FluentReference]
		const string Label = "label-tool-tiling-path";

		[Desc("The widget tree to open when the tool is selected.")]
		const string PanelWidget = "TILING_PATH_TOOL_PANEL";

		public bool IsEnabled { get; }

		string IEditorTool.Label => Label;
		string IEditorTool.PanelWidget => PanelWidget;
		public TraitInfo TraitInfo { get; }

		/// <summary>
		/// Holds the shape of a path being planned out in the map editor.
		/// </summary>
		public sealed class PathPlan
		{
			public readonly Direction Start;
			public readonly Direction End;
			public readonly bool Loop;
			public readonly ImmutableArray<CPos> Rallies;
			public Direction AutoStart
			{
				get
				{
					if (Start != Direction.None)
					{
						return Start;
					}
					else
					{
						if (Rallies.Length >= 2)
							return DirectionExts.ClosestFromCVec(Rallies[1] - Rallies[0]);
						else
							return Direction.None;
					}
				}
			}

			public Direction AutoEnd
			{
				get
				{
					if (End != Direction.None)
					{
						return End;
					}
					else if (Loop)
					{
						return AutoStart;
					}
					else
					{
						if (Rallies.Length >= 2)
							return DirectionExts.ClosestFromCVec(Rallies[^1] - Rallies[^2]);
						else
							return Direction.None;
					}
				}
			}

			public CPos FirstPoint => Rallies[0];
			public CPos LastPoint => Loop ? Rallies[0] : Rallies[^1];

			PathPlan(Direction start, Direction end, bool loop, ImmutableArray<CPos> rallies)
			{
				if (rallies == null || rallies.Length == 0)
					throw new ArgumentException("rallies must have at least one point");

				Start = start;
				End = end;
				Loop = loop && rallies.Length >= 3;
				Rallies = rallies;
			}

			/// <summary>Start a new path with a given first point.</summary>
			public PathPlan(CPos first)
			{
				Start = Direction.None;
				End = Direction.None;
				Loop = false;
				Rallies = [first];
			}

			/// <summary>Return a copy, modifying the start direction.</summary>
			public PathPlan WithStart(Direction start)
			{
				return new PathPlan(start, End, Loop, Rallies);
			}

			/// <summary>Return a copy, modifying the end direction.</summary>
			public PathPlan WithEnd(Direction end)
			{
				return new PathPlan(Start, end, Loop, Rallies);
			}

			/// <summary>Return a copy, modifying whether the path is looped.</summary>
			public PathPlan WithLoop(bool loop)
			{
				return new PathPlan(Start, End, loop, Rallies);
			}

			/// <summary>Return a copy, with a rally appended.</summary>
			public PathPlan WithRallyAppended(CPos cpos)
			{
				return new PathPlan(Start, Direction.None, Loop, [.. Rallies, cpos]);
			}

			/// <summary>Return a copy, with the rally at index removed.</summary>
			public PathPlan WithRallyRemoved(int index)
			{
				if (Rallies.Length == 1)
					return null;

				return new PathPlan(
					index != 0 ? Start : Direction.None,
					index != Rallies.Length - 1 ? End : Direction.None,
					Loop,
					[.. Rallies[..index], .. Rallies[(index + 1)..]]);
			}

			/// <summary>Return a copy, with the rally at index replace/moved.</summary>
			public PathPlan WithRallyReplaced(int index, CPos cpos)
			{
				return new PathPlan(Start, End, Loop, [.. Rallies[..index], cpos, .. Rallies[(index + 1)..]]);
			}

			/// <summary>Return a copy, with a rally inserted before index.</summary>
			public PathPlan WithRallyInserted(int index, CPos cpos)
			{
				return new PathPlan(Start, End, Loop, [.. Rallies[..index], cpos, .. Rallies[index..]]);
			}

			/// <summary>Return a copy, with everything translated by offset.</summary>
			public PathPlan Moved(CVec offset)
			{
				var rallies = Rallies.Select(r => r + offset).ToImmutableArray();
				return new PathPlan(Start, End, Loop, rallies);
			}

			/// <summary>Return a copy, with rallies reversed and directions swapped.</summary>
			public PathPlan Reversed()
			{
				if (Loop)
				{
					var reversedStart = End;
					var reversedEnd = Start;
					if (Start != Direction.None && End == Direction.None)
					{
						reversedStart = AutoEnd;
						reversedEnd = Direction.None;
					}

					return new PathPlan(
						reversedStart.Reverse(),
						reversedEnd.Reverse(),
						Loop,
						Rallies.Skip(1).Append(Rallies[0]).Reverse().ToImmutableArray());
				}
				else
				{
					return new PathPlan(
						End.Reverse(),
						Start.Reverse(),
						Loop,
						Rallies.Reverse().ToImmutableArray());
				}
			}

			/// <summary>
			/// Convert the rally points into a sequence of unit-space CPos points, suitable for
			/// processing with TilingPath.
			/// </summary>
			public CPos[] Points()
			{
				return PointsWithRallyIndex().Select(pair => pair.CPos).ToArray();
			}

			/// <summary>
			/// Convert the rally points into a sequence of unit-space CPos points and their
			/// associated latest rally index. For loops, the last rally index is the number of the
			/// rallies.
			/// </summary>
			public (CPos CPos, int RallyIndex)[] PointsWithRallyIndex()
			{
				if (Rallies.Length == 1)
					return [(Rallies[0], 0)];

				var points = new List<(CPos CPos, int RallyIndex)>();
				var cpos = Rallies[0];
				points.Add((cpos, 0));
				var inertia = AutoStart.ToCVec();
				if (inertia.X != 0 && inertia.Y != 0)
					inertia = new CVec(inertia.X, 0);

				void AddPointsUpTo(CPos target, int i)
				{
					if (cpos == target)
						throw new InvalidOperationException("there are duplicate rally points");

					var offset = target - cpos;
					var xStep = Math.Sign(offset.X);
					var yStep = Math.Sign(offset.Y);

					var axisAligned = xStep == 0 || yStep == 0;

					if (axisAligned)
					{
						while (cpos != target)
						{
							inertia = new CVec(xStep, yStep);
							cpos += inertia;
							points.Add((cpos, i));
						}
					}
					else
					{
						var xUnderModulo = Math.Abs(offset.Y);
						var yUnderModulo = Math.Abs(offset.X);

						// Technically, these range from 0 inclusive to modulo inclusive!
						var xModulo = xUnderModulo * 2;
						var yModulo = yUnderModulo * 2;

						if (xUnderModulo < yUnderModulo)
							inertia = new CVec(xStep, 0);
						else if (yUnderModulo > xUnderModulo)
							inertia = new CVec(0, yStep);
						else
							inertia =
								DirectionExts.FromCVecNonDiagonal(
									inertia + new CVec(xStep * 2, yStep * 2))
										.ToCVec();

						while (cpos != target)
						{
							if (xUnderModulo < yUnderModulo)
							{
								yUnderModulo -= xUnderModulo;
								xUnderModulo = xModulo;
								inertia = new CVec(xStep, 0);
							}
							else if (xUnderModulo > yUnderModulo)
							{
								xUnderModulo -= yUnderModulo;
								yUnderModulo = yModulo;
								inertia = new CVec(0, yStep);
							}
							else if (inertia.X != 0)
							{
								xUnderModulo = xModulo;
								yUnderModulo = 0;
							}
							else
							{
								yUnderModulo = yModulo;
								xUnderModulo = 0;
							}

							cpos += inertia;
							points.Add((cpos, i));
						}
					}
				}

				for (var i = 1; i < Rallies.Length; i++)
					AddPointsUpTo(Rallies[i], i);

				if (Loop)
					AddPointsUpTo(Rallies[0], Rallies.Length);

				return points.ToArray();
			}
		}

		public readonly World World;
		public WorldRenderer WorldRenderer = null;
		public readonly ImmutableArray<MultiBrush> SegmentedBrushes;
		readonly ImmutableArray<string> startTypes;
		public readonly ImmutableArray<string> InnerTypes;
		readonly ImmutableArray<string> endTypes;
		public Dictionary<string, ImmutableArray<string>> StartTypesByInner = [];
		public Dictionary<string, ImmutableArray<string>> EndTypesByInner = [];

		public PathPlan Plan { get; private set; } = null;
		public string StartType { get; private set; } = null;
		public string InnerType { get; private set; } = null;
		public string EndType { get; private set; } = null;
		public bool ClosedLoops { get; private set; } = true;
		public int RandomSeed { get; private set; } = 0;
		public int MaxDeviation { get; private set; } = 5;
		public EditorBlitSource? EditorBlitSource { get; private set; } = null;

		bool disposed;

		public TilingPathTool(Actor self, TilingPathToolInfo info)
		{
			World = self.World;
			TraitInfo = info;

			var templatedTerrainInfo = World.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			SegmentedBrushes =
				templatedTerrainInfo.MultiBrushCollections.Keys
					.Order()
					.SelectMany(name => MultiBrush.LoadCollection(World.Map, name))
					.Where(multiBrush => multiBrush.Segment != null)
					.ToImmutableArray();

			IsEnabled = SegmentedBrushes.Length > 0;
			if (!IsEnabled)
				return;

			InnerTypes = SegmentedBrushes
				.Where(b => b.Segment != null)
				.SelectMany<MultiBrush, string>(b =>
					b.Segment.Inner != null
						? [b.Segment.Inner.Split('.')[0]]
						: [b.Segment.Start.Split('.')[0], b.Segment.End.Split('.')[0]])
				.Distinct()
				.Order()
				.ToImmutableArray();

			foreach (var innerType in InnerTypes)
			{
				StartTypesByInner[innerType] = SegmentedBrushes
					.Where(b => b.Segment != null
						&& b.Segment.Inner != null
						? b.Segment.Inner.Split('.')[0] == innerType : (b.Segment.Start.Split('.')[0] == innerType || b.Segment.End.Split('.')[0] == innerType))
					.Select(b => string.Join(".", b.Segment.Start.Split('.').SkipLast(1)))
					.Distinct()
					.Order()
					.ToImmutableArray();

				EndTypesByInner[innerType] = SegmentedBrushes
					.Where(b => b.Segment != null
						&& b.Segment.Inner != null
						? b.Segment.Inner.Split('.')[0] == innerType : (b.Segment.Start.Split('.')[0] == innerType || b.Segment.End.Split('.')[0] == innerType))
					.Select(b => string.Join(".", b.Segment.End.Split('.').SkipLast(1)))
					.Distinct()
					.Order()
					.ToImmutableArray();
			}

			startTypes = StartTypesByInner
				.SelectMany(kvp => kvp.Value)
				.Distinct()
				.Order()
				.ToImmutableArray();

			endTypes = EndTypesByInner
				.SelectMany(kvp => kvp.Value)
				.Distinct()
				.Order()
				.ToImmutableArray();

			InnerType = info.DefaultInner
				.FirstOrDefault(InnerTypes.Contains, InnerTypes[0]);

			VerifyTypes(InnerType);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			WorldRenderer = wr;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			disposed = true;
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			yield break;
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;

		EditorBlitSource? TilePlan(PathPlan plan)
		{
			if (WorldRenderer == null)
				return null;

			if (plan == null || plan.Rallies.Length < 2)
				return null;

			var points = plan.Points();
			if (points == null)
				return null;

			(string Start, string End)[] terminalTypes = [(StartType, EndType)];
			if (ClosedLoops && plan.Loop)
			{
				terminalTypes = startTypes.Concat(endTypes)
					.Distinct()
					.Where(t => t.Split('.')[0] == InnerType)
					.Select(t => (t, t))
					.ToArray();
			}

			var map = World.Map;
			var permittedTemplates =
				TilingPath.PermittedSegments.FromTypes(
					SegmentedBrushes,
					terminalTypes.Select(t => t.Start),
					[InnerType],
					terminalTypes.Select(t => t.End));

			foreach (var (startType, endType) in terminalTypes)
			{
				var random = new MersenneTwister(RandomSeed);
				var tilingPath = new TilingPath(
					map,
					points,
					MaxDeviation,
					startType,
					endType,
					permittedTemplates);
				tilingPath.Start.Direction = plan.AutoStart;
				tilingPath.End.Direction = plan.AutoEnd;
				var result = tilingPath.Tile(random);
				if (result != null)
					return result.ToEditorBlitSource(WorldRenderer, random);
			}

			return null;
		}

		public void VerifyTypes(string innerType)
		{
			var startChoices = StartTypesByInner[innerType];
			if (startChoices.Length == 0)
				StartType = "";
			else if (string.IsNullOrEmpty(StartType) || !startChoices.Contains(StartType))
				StartType = startChoices[0];

			var endChoices = EndTypesByInner[innerType];
			if (endChoices.Length == 0)
				EndType = "";
			else if (string.IsNullOrEmpty(EndType) || !endChoices.Contains(EndType))
				EndType = endChoices[0];

			if (string.IsNullOrEmpty(innerType))
				InnerType = InnerTypes[0];
		}

		void Update()
		{
			EditorBlitSource = TilePlan(Plan);
		}

		public void SetPlan(PathPlan value)
		{
			Plan = value;
			Update();
		}

		public void SetStartType(string value)
		{
			StartType = value;
			Update();
		}

		public void SetInnerType(string value)
		{
			InnerType = value;
			VerifyTypes(value);
			Update();
		}

		public void SetEndType(string value)
		{
			EndType = value;
			Update();
		}

		public void SetClosedLoops(bool value)
		{
			ClosedLoops = value;
			Update();
		}

		public void SetRandomSeed(int value)
		{
			RandomSeed = value;
			Update();
		}

		public void SetMaxDeviation(int value)
		{
			MaxDeviation = value;
			Update();
		}
	}
}
