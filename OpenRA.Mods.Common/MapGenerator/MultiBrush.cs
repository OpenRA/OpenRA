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
using System.Text.RegularExpressions;
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.MapGenerator
{
	/// <summary>
	/// MiniYaml-loaded definition of a MultiBrush. Can be loaded into a MultiBrush once a map is
	/// available.
	/// </summary>
	public sealed class MultiBrushInfo
	{
		public sealed class ActorInfo
		{
			[FieldLoader.Ignore]
			public readonly string Type;
			public readonly WVec Offset = WVec.Zero;

			public ActorInfo(string type)
			{
				Type = type;
			}

			public ActorInfo(MiniYaml my)
			{
				if (string.IsNullOrEmpty(my.Value))
					throw new YamlException("Missing actor type");

				Type = my.Value;
				FieldLoader.Load(this, my);
			}
		}

		public sealed class TemplateInfo
		{
			[FieldLoader.Ignore]
			public readonly ushort Type;
			public readonly CVec Offset = CVec.Zero;

			public TemplateInfo(ushort type)
			{
				Type = type;
			}

			public TemplateInfo(MiniYaml my)
			{
				if (string.IsNullOrEmpty(my.Value))
					throw new YamlException("Missing template type");

				if (!Exts.TryParseUshortInvariant(my.Value, out Type))
					throw new YamlException($"Invalid MultiBrush Template `${my.Value}`");

				FieldLoader.Load(this, my);
			}
		}

		public sealed class TileInfo
		{
			[FieldLoader.Ignore]
			public readonly TerrainTile Type;
			public readonly CVec Offset = CVec.Zero;

			public TileInfo(MiniYaml my)
			{
				if (string.IsNullOrEmpty(my.Value))
					throw new YamlException("Missing tile type");

				if (!TerrainTile.TryParse(my.Value, out Type))
					throw new YamlException($"Invalid MultiBrush Tile `${my.Value}`");

				FieldLoader.Load(this, my);
			}
		}

		public readonly int Weight;

		public readonly ImmutableArray<ActorInfo> Actors;
		public readonly TerrainTile? BackingTile;
		public readonly ImmutableArray<TemplateInfo> Templates;
		public readonly ImmutableArray<TileInfo> Tiles;
		public readonly MultiBrushSegment Segment;

		public MultiBrushInfo(
			MiniYaml my = null,
			int weight = MultiBrush.DefaultWeight,
			IEnumerable<ActorInfo> actors = null,
			TerrainTile? backingTile = null,
			IEnumerable<TemplateInfo> templates = null,
			IEnumerable<TileInfo> tiles = null,
			MultiBrushSegment segment = null)
		{
			Weight = weight;
			var actorsAcc = (actors ?? []).ToList();
			BackingTile = backingTile;
			var templatesAcc = (templates ?? []).ToList();
			var tilesAcc = (tiles ?? []).ToList();
			Segment = segment;
			foreach (var node in my?.Nodes ?? [])
				switch (node.Key.Split('@')[0])
				{
					case "Weight":
						if (!Exts.TryParseInt32Invariant(node.Value.Value, out Weight))
							throw new YamlException($"Invalid MultiBrush Weight `{node.Value.Value}`");
						break;
					case "Actor":
						actorsAcc.Add(new ActorInfo(node.Value));
						break;
					case "BackingTile":
						if (TerrainTile.TryParse(node.Value.Value, out var bt))
							BackingTile = bt;
						else
							throw new YamlException($"Invalid MultiBrush BackingTile `{node.Value.Value}`");
						break;
					case "Template":
						templatesAcc.Add(new TemplateInfo(node.Value));
						break;
					case "Tile":
						tilesAcc.Add(new TileInfo(node.Value));
						break;
					case "Segment":
						if (Segment != null)
							throw new YamlException("Multiple MultiBrush Segment definitions");
						Segment = new MultiBrushSegment(node.Value);
						break;
					default:
						throw new YamlException($"Unrecognized MultiBrush key {node.Key.Split('@')[0]}");
				}

			Actors = [.. actorsAcc];
			Templates = [.. templatesAcc];
			Tiles = [.. tilesAcc];
		}

		public static ImmutableArray<MultiBrushInfo> ParseCollection(MiniYaml my)
		{
			var brushes = new List<MultiBrushInfo>();
			foreach (var node in my.Nodes)
			{
				switch (node.Key.Split('@')[0])
				{
					case "MultiBrush":
						brushes.Add(new MultiBrushInfo(node.Value));
						break;
					case "FromTemplates":
						foreach (var template in FieldLoader.GetValue<List<ushort>>(node.Key, node.Value.Value))
							brushes.Add(new MultiBrushInfo(
								my: node.Value,
								templates: [new TemplateInfo(template)]));

						break;
					case "FromActors":
						foreach (var actor in FieldLoader.GetValue<List<string>>(node.Key, node.Value.Value))
							brushes.Add(new MultiBrushInfo(
								my: node.Value,
								actors: [new ActorInfo(actor)]));

						break;
					default:
						throw new YamlException($"Invalid MultiBrush collection key `{node.Key}`");
				}
			}

			return brushes.ToImmutableArray();
		}
	}

	/// <summary>
	/// Information about how certain MultiBrushes (like cliffs, beaches, roads) link together.
	/// </summary>
	public sealed class MultiBrushSegment
	{
		/// <summary>Start type, including a direction. E.g. "Cliff.R".</summary>
		[FieldLoader.Require]
		public readonly string Start;

		/// <summary>
		/// Inner type. Does not include a direction. E.g. "Cliff".
		/// A null (absent) inner type implies that both the start and end types can be considered
		/// valid inner types.
		/// </summary>
		public readonly string Inner = null;

		/// <summary>End type, including a direction. E.g. "Cliff.R".</summary>
		[FieldLoader.Require]
		public readonly string End;

		/// <summary>
		/// Point sequence, where points are -X-Y corners of template tiles.
		/// </summary>
		[FieldLoader.Ignore]
		public readonly ImmutableArray<CVec> Points;

		/// <summary>
		/// Create a Segment from a point sequence and given start, inner, and end types.
		/// </summary>
		public MultiBrushSegment(string start, string inner, string end, ImmutableArray<CVec> points)
		{
			Start = start;
			Inner = inner;
			End = end;
			Points = points;
		}

		public MultiBrushSegment(MiniYaml my)
		{
			FieldLoader.Load(this, my);
			{
				// Unlike FieldLoader.ParseInt2Array, whitespace is ignored.
				var value = my.NodeWithKey("Points").Value.Value;
				var parts = Regex.Replace(value, @"\s+", string.Empty)
					.Split(',', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length % 2 != 0)
					FieldLoader.InvalidValueAction(value, typeof(int2[]), "Points");

				var points = new CVec[parts.Length / 2];
				for (var i = 0; i < points.Length; i++)
				{
					points[i] = new CVec(Exts.ParseInt32Invariant(parts[2 * i]), Exts.ParseInt32Invariant(parts[2 * i + 1]));
					if (i > 0)
					{
						var step = points[i] - points[i - 1];
						if (Math.Abs(step.X) + Math.Abs(step.Y) != 1)
							throw new YamlException($"Points sequence {value} has non-unit steps");
					}
				}

				Points = [.. points];
			}
		}

		public static bool MatchesType(string type, string matcher)
		{
			if (type == matcher)
				return true;

			return type.StartsWith($"{matcher}.", StringComparison.InvariantCulture);
		}

		public bool HasStartType(string matcher)
			=> MatchesType(Start, matcher);
		public bool HasInnerType(string matcher)
			=> Inner != null
				? MatchesType(Inner, matcher)
				: (MatchesType(Start, matcher) || MatchesType(End, matcher));
		public bool HasEndType(string matcher)
			=> MatchesType(End, matcher);

		public static Direction TypeDirection(string type)
		{
			if (!Enum.TryParse(type.Split('.')[^1], out Direction direction))
				throw new InvalidOperationException("MultiBrushSegment has invalid direction");
			return direction;
		}

		public Direction StartDirection
			=> TypeDirection(Start);
		public Direction EndDirection
			=> TypeDirection(End);
	}

	/// <summary>A super template that can be used to paint both tiles and actors.</summary>
	public sealed class MultiBrush
	{
		public const int DefaultWeight = 1000;

		[Flags]
		public enum Replaceability
		{
			None = 0,
			Tile = 1,
			Actor = 2,
			SubCellActor = 4,
			Any = Tile | Actor | SubCellActor,
		}

		[Flags]
		public enum ActorSubCell : byte
		{
			Any = 0,
			FullCell = byte.MaxValue,
		}

		/// <summary>
		/// Create a bitmask where all subcell bits are set to 1.
		/// </summary>
		public static ActorSubCell FullSubCell(MapGrid grid)
		{
			return (ActorSubCell)((1 << grid.SubCellOffsets.Length - 1) - 1);
		}

		/// <summary>Convert a SubCell to an ActorSubCell.</summary>
		public static ActorSubCell ToActorSubCell(SubCell sub)
		{
			if (sub == SubCell.FullCell)
				return ActorSubCell.FullCell;
			if (sub == SubCell.Any)
				return ActorSubCell.Any;
			if (sub == SubCell.Invalid)
				throw new ArgumentOutOfRangeException(nameof(sub), sub, null);

			return (ActorSubCell)(1 << ((int)sub - 1));
		}

		/// <summary>
		/// Generate a random set of sub-cell bits based on the given <paramref name="frequency"/>.
		/// Frequency is the chance (0-1000) of each sub-cell being included.
		/// </summary>
		static ActorSubCell RandomSubCellBits(MapGrid grid, int frequency, MersenneTwister random)
		{
			var result = ActorSubCell.Any;
			for (var i = 0; i < grid.SubCellOffsets.Length; i++)
				if (random.Next(1000) < frequency)
					result |= (ActorSubCell)(1 << i);

			return result;
		}

		/// <summary>
		/// Get the first free sub-cell in the <paramref name="search"/> bitmask.
		/// </summary>
		public static SubCell FreeSubCell(MapGrid grid, ActorSubCell search)
		{
			for (var i = 0; i < grid.SubCellOffsets.Length; i++)
				if (((byte)search & (1 << i)) == 0)
					return (SubCell)(i + 1);

			throw new ArgumentOutOfRangeException(nameof(search), search, "No free sub-cell found");
		}

		readonly struct TileRange
		{
			public readonly ushort Type;
			public readonly byte MinIndex;
			public readonly byte MaxIndex;

			// Height is relative, so can be negative.
			public readonly short HeightOffset;
			public readonly byte Ramp;

			public TileRange(ushort type, byte minIndex, byte maxIndex, short heightOffset, byte ramp)
			{
				Type = type;
				MinIndex = minIndex;
				MaxIndex = maxIndex;
				HeightOffset = heightOffset;
				Ramp = ramp;
			}

			public TileRange(ushort type, byte index, short heightOffset, byte ramp)
				: this(type, index, index, heightOffset, ramp) { }

			public TileRange(TerrainTile tile, short heightOffset, byte ramp)
				: this(tile.Type, tile.Index, heightOffset, ramp) { }

			/// <summary>Pick a non-randomized tile.</summary>
			public TerrainTile DefaultTile => new(Type, MinIndex);

			/// <summary>
			/// Pick a (possibly randomized) tile. random can be null to fall back to DefaultTile.
			/// </summary>
			public TerrainTile Pick(MersenneTwister random)
			{
				if (random == null)
					return DefaultTile;

				return new TerrainTile(Type, (byte)random.Next(MinIndex, MaxIndex + 1));
			}

			/// <summary>Create a copy of this TileRange, adding an additional heightOffset.</summary>
			public TileRange WithHeightOffset(short heightOffset)
			{
				return new(Type, MinIndex, MaxIndex, (short)(HeightOffset + heightOffset), Ramp);
			}
		}

		public int Weight;
		readonly List<(CVec XY, TileRange TileRange)> tiles;
		readonly List<ActorPlan> actorPlans;
		public MultiBrushSegment Segment { get; private set; }

		// A cache for the shape/footprint of the brush.
		// Null means the shape is dirty and must be recomputed.
		(CVec, SubCell)[] shape;

		public bool HasTiles => tiles.Count != 0;
		public bool HasActors => actorPlans.Count != 0;
		public IEnumerable<(CVec Vec, SubCell SubCell)> Shape => GetShape();

		/// <summary>Total area covered by the MultiBrush.</summary>
		public int Area => GetShape().Length;

		/// <summary>
		/// The CVec of the first cell covered by the MultiBrush. This is the left-most cell in the
		/// top-row. Note that this does not necessarily correspond to the top-left corner of the
		/// rectangular bounds of the MultiBrush.
		/// </summary>
		public (CVec FirstCell, SubCell SubCell) FirstCell => GetShape()[0];

		public IEnumerable<(CVec XY, short Height, byte Ramp)> GetHeightsAndRamps()
		{
			return tiles.Select(t => (t.XY, t.TileRange.HeightOffset, t.TileRange.Ramp));
		}

		public Replaceability Contract()
		{
			var replacability = Replaceability.None;
			if (HasActors)
			{
				if (Shape.Any(xy => xy.SubCell != SubCell.FullCell))
					replacability |= Replaceability.SubCellActor;
				else
					replacability |= Replaceability.Actor;
			}

			if (HasTiles)
				replacability |= Replaceability.Tile;

			return replacability;
		}

		/// <summary>
		/// Create a new empty MultiBrush with a default weight of 1.0.
		/// </summary>
		public MultiBrush()
		{
			Weight = DefaultWeight;
			tiles = [];
			actorPlans = [];
			Segment = null;
			shape = null;
		}

		MultiBrush(MultiBrush other)
		{
			Weight = other.Weight;
			tiles = [.. other.tiles];
			actorPlans = [.. other.actorPlans];
			Segment = null;
			shape = [.. other.shape];
		}

		public MultiBrush(Map map, MultiBrushInfo info)
			: this()
		{
			WithWeight(info.Weight);
			foreach (var actorInfo in info.Actors)
			{
				var actor = new ActorPlan(map, actorInfo.Type)
				{
					WPosLocation = WPos.Zero + actorInfo.Offset
				};

				WithActor(actor);
			}

			if (info.BackingTile != null)
				WithBackingTile((TerrainTile)info.BackingTile);

			foreach (var templateInfo in info.Templates)
				WithTemplate(map, templateInfo.Type, templateInfo.Offset);

			foreach (var tileInfo in info.Tiles)
				WithTile(tileInfo.Type, tileInfo.Offset);

			ReplaceSegment(info.Segment);
		}

		/// <summary>Load a named MultiBrush collection from a map's tileset.</summary>
		public static ImmutableArray<MultiBrush> LoadCollection(Map map, string name)
		{
			var templatedTerrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			return templatedTerrainInfo.MultiBrushCollections[name]
				.Select(info => new MultiBrush(map, info))
				.ToImmutableArray();
		}

		/// <summary>
		/// Clone the brush. Note that this does not deep clone any ActorPlans.
		/// </summary>
		public MultiBrush Clone()
		{
			return new MultiBrush(this);
		}

		void UpdateShape()
		{
			var xys = new HashSet<(CVec, SubCell)>();

			foreach (var (xy, _) in tiles)
				xys.Add((xy, SubCell.FullCell));

			foreach (var actorPlan in actorPlans)
				foreach (var (cpos, subCell) in actorPlan.Footprint())
					xys.Add((new CVec(cpos.X, cpos.Y), subCell));

			if (xys.Count != 0)
				shape = xys.OrderBy(xy => (xy.Item1.Y, xy.Item1.X)).ToArray();
			else
				shape = [(new CVec(0, 0), SubCell.FullCell)];
		}

		(CVec Vec, SubCell SubCell)[] GetShape()
		{
			if (shape == null)
				UpdateShape();

			return shape;
		}

		/// <summary>
		/// Add tiles from a template, optionally with a given offset. By
		/// default, it will be auto-offset such that the first tile is
		/// under (0, 0).
		/// </summary>
		public MultiBrush WithTemplate(Map map, ushort templateId, CVec offset, short heightOffset = 0)
		{
			var itti = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			return WithTemplate(itti, templateId, offset, heightOffset);
		}

		public MultiBrush WithTemplate(ITemplatedTerrainInfo itti, ushort templateId, CVec offset, short heightOffset = 0)
		{
			if (!itti.Templates.TryGetValue(templateId, out var templateInfo))
				throw new ArgumentException($"Tileset does not contain template with ID {templateId}.");
			return WithTemplate(templateInfo, offset, heightOffset);
		}

		public MultiBrush WithTemplate(TerrainTemplateInfo templateInfo, CVec offset, short heightOffset = 0)
		{
			if (templateInfo.PickAny)
			{
				// Assume that single tiles have equal height.
				tiles.Add((
					offset,
					new(
						templateInfo.Id,
						0,
						(byte)(templateInfo.TilesCount - 1),
						(short)(templateInfo[0].Height + heightOffset),
						templateInfo[0].RampType)));
			}
			else
			{
				for (var y = 0; y < templateInfo.Size.Y; y++)
					for (var x = 0; x < templateInfo.Size.X; x++)
					{
						var i = y * templateInfo.Size.X + x;
						if (templateInfo[i] != null)
							tiles.Add((
								new CVec(x, y) + offset,
								new(
									templateInfo.Id,
									(byte)i,
									(short)(templateInfo[i].Height + heightOffset),
									templateInfo[i].RampType)));
					}
			}

			shape = null;
			return this;
		}

		/// <summary>
		/// Add a single tile, optionally with a given offset. By default, it
		/// will be positioned under (0, 0).
		/// </summary>
		public MultiBrush WithTile(TerrainTile tile, CVec offset, short heightOffset = 0, byte ramp = 0)
		{
			tiles.Add((offset, new(tile, heightOffset, ramp)));
			shape = null;
			return this;
		}

		/// <summary>Add an actor (using the ActorPlan's location as an offset).</summary>
		public MultiBrush WithActor(ActorPlan actor)
		{
			actorPlans.Add(actor);
			shape = null;
			return this;
		}

		/// <summary>
		/// <para>For all spaces occupied by the brush, add the given tile.</para>
		/// <para>This is useful for adding a backing tile for actors.</para>
		/// </summary>
		public MultiBrush WithBackingTile(TerrainTile tile)
		{
			if (Area == 0)
				throw new InvalidOperationException("No area");
			foreach (var (xy, _) in GetShape())
				tiles.Add((xy, new(tile, 0, 0)));

			return this;
		}

		/// <summary>
		/// Adds a Segment to this MultiBrush for later use with TilingPath.
		/// </summary>
		public MultiBrush ReplaceSegment(MultiBrushSegment segment)
		{
			Segment = segment;
			return this;
		}

		/// <summary>Update the weight.</summary>
		public MultiBrush WithWeight(int weight)
		{
			if (weight <= 0)
				throw new ArgumentException("Weight was not > 0");
			Weight = weight;
			return this;
		}

		/// <summary>
		/// Add the tiles and actors from another MultiBrush into this one at a given offset.
		/// (Does not copy segments.)
		/// </summary>
		public void MergeFrom(MultiBrush other, CVec at, MapGridType mapGridType, short heightOffset = 0)
		{
			foreach (var original in other.actorPlans)
			{
				var actorPlan = original.Clone();
				actorPlan.WPosLocation += CellLayerUtils.CVecToWVec(at, mapGridType);
				actorPlans.Add(actorPlan);
			}

			foreach (var (xy, tile) in other.tiles)
				tiles.Add((xy + at, tile.WithHeightOffset(heightOffset)));

			shape = null;
		}

		/// <summary>
		/// Paint the tiles onto the <paramref name="map"/> at the given position.
		/// </summary>
		public void PaintTilesOntoMap(
			Map map,
			CPos paintAt,
			short? heightOffset,
			Action<MPos> paint,
			MersenneTwister random)
		{
			short finalHeightOffset = 0;
			if (heightOffset.HasValue)
			{
				finalHeightOffset = heightOffset.Value;
			}
			else
			{
				foreach (var (cpos, _) in GetShape())
				{
					if (map.Height.Contains(paintAt + cpos))
					{
						finalHeightOffset = map.Height[paintAt + cpos];
						break;
					}
				}
			}

			foreach (var (xy, tile) in tiles)
			{
				var mpos = (paintAt + xy).ToMPos(map);
				if (map.Tiles.Contains(mpos))
				{
					// map.Ramp does not need to be updated here.
					map.Tiles[mpos] = tile.Pick(random);
					map.Height[mpos] = (byte)Math.Clamp(tile.HeightOffset + finalHeightOffset, byte.MinValue, byte.MaxValue);
					paint(mpos);
				}
			}
		}

		/// <summary>
		/// Paint the tiles onto the <paramref name="target"/> brush at the given position.
		/// </summary>
		public void PaintTilesOntoBrush(
			MultiBrush target,
			CPos paintAt,
			Action<CPos> paint,
			MersenneTwister random)
		{
			foreach (var (xy, tile) in tiles)
			{
				var pos = paintAt + xy;
				target.tiles.Add((new CVec(pos.X, pos.Y), tile));
				paint(pos);
			}

			target.shape = null;
		}

		/// <summary>
		/// Clone actors onto the <paramref name="actorPlans"/> list.
		/// </summary>
		public void PaintActorsOntoList(
			List<ActorPlan> actorPlans,
			CPos paintAt,
			Action<ActorPlan> actorPlanModifier,
			string actorOwner = null)
		{
			foreach (var actorPlan in this.actorPlans)
			{
				var plan = actorPlan.Clone();
				var offset = plan.Location;
				plan.Location = paintAt + new CVec(offset.X, offset.Y);
				if (actorOwner != null)
					plan.Owner = actorOwner;

				actorPlanModifier(plan);
				actorPlans.Add(plan);
			}
		}

		/// <summary>
		/// Clone actors onto the <paramref name="target"/> brush.
		/// </summary>
		public void PaintActorsOntoBrush(
			MultiBrush target,
			CPos paintAt,
			Action<ActorPlan> actorPlanModifier,
			string actorOwner = null)
		{
			PaintActorsOntoList(target.actorPlans, paintAt, actorPlanModifier, actorOwner);
			target.shape = null;
		}

		/// <summary>
		/// Pick a random brush from the available brushes.
		/// </summary>
		/// <remarks>
		/// Allocates an array to hold the weights of the available brushes.
		/// </remarks>
		public static MultiBrush PickRandomBrush(IReadOnlyList<MultiBrush> availableBrushes, MersenneTwister random)
		{
			if (availableBrushes == null || availableBrushes.Count == 0)
				throw new ArgumentException("No available brushes to pick from.");

			var weights = availableBrushes.Select(b => b.Weight).ToArray();
			return availableBrushes[random.PickWeighted(weights)];
		}

		/// <summary>
		/// Return the validity of adding <paramref name="brush"/> onto the <paramref name="targetLayer"/> <see cref="CellLayer{ActorSubCell}"/>.
		/// </summary>
		public static Replaceability ValidateBrushPlacement(
			MultiBrush brush,
			CPos offset,
			CellLayer<Replaceability> mask,
			CellLayer<ActorSubCell> targetLayer,
			ActorSubCell fullSubcell)
		{
			var shape = brush.Shape;
			var shapeContract = brush.Contract();
			foreach (var (cvec, subCell) in shape)
			{
				var cpos = offset + cvec;
				if (!mask.Contains(cpos))
				{
					// Can't reserve - not in replace layer.
					return Replaceability.None;
				}

				var r = targetLayer[cpos];
				if (r.HasFlag(fullSubcell) || (r != ActorSubCell.Any && subCell == SubCell.FullCell))
				{
					// Can't reserve - not the right subcell.
					return Replaceability.None;
				}

				shapeContract &= mask[cpos];
				if (shapeContract == Replaceability.None)
				{
					// Can't reserve - obstruction choice doesn't comply
					// with replaceability of original tiles.
					return Replaceability.None;
				}
			}

			return shapeContract;
		}

		/// <summary>
		/// Transfer the shape subcells from the <paramref name="sourceBrush"/> to the <paramref name="target"/> actor plan.
		/// </summary>
		public static void TransferShapeSubCells(MultiBrush sourceBrush, ActorPlan target)
		{
			foreach (var (_, subCell) in sourceBrush.Shape)
			{
				if (subCell != SubCell.FullCell)
					target.SubCell = subCell;
			}
		}

		/// <summary>
		/// Paint the <paramref name="sourceBrush"/> onto the <paramref name="targetLayer"/>,
		/// finding free subcells and applying it to the <paramref name="target"/> actor plan.
		/// </summary>
		static void PaintOntoFreeSubCells(
			MultiBrush sourceBrush,
			ActorPlan target,
			CPos offset,
			CellLayer<ActorSubCell> targetLayer,
			MapGrid grid)
		{
			foreach (var (cell, subCell) in sourceBrush.Shape)
			{
				var pos = cell + offset;
				if (subCell == SubCell.FullCell)
					targetLayer[pos] = ActorSubCell.FullCell;
				else
				{
					var current = targetLayer[pos];
					var freeSubCell = FreeSubCell(grid, current);
					targetLayer[pos] = current | ToActorSubCell(freeSubCell);
					target.SubCell = freeSubCell;
				}
			}
		}

		/// <summary>
		/// Paint an area masked by <paramref name="mask"/> onto the map and <paramref name="actorPlans"/> using <paramref name="availableBrushes"/>.
		/// </summary>
		/// <remarks>
		/// Painting is biased towards smaller features in several ways.
		/// 1. it's more likely to find a suitable position for a small feature.
		/// 2. we do a second pass, trying to fill in 1x1 gaps with actors.
		/// 3. sparsity is the applied first, meaning it can negate space for larger features.
		/// </remarks>
		public static void PaintArea(
			Map map,
			List<ActorPlan> actorPlans,
			CellLayer<Replaceability> mask,
			IReadOnlyList<MultiBrush> availableBrushes,
			MersenneTwister random,
			bool alwaysPreferLargerBrushes = false)
		{
			var brushesByAreaDict = new Dictionary<int, List<MultiBrush>>();
			foreach (var brush in availableBrushes)
			{
				if (!brushesByAreaDict.ContainsKey(brush.Area))
					brushesByAreaDict.Add(brush.Area, []);
				brushesByAreaDict[brush.Area].Add(brush);
			}

			var brushesByArea = brushesByAreaDict
				.OrderBy(kv => -kv.Key)
				.ToList();
			var brushTotalArea = availableBrushes.Sum(t => t.Area);
			var brushTotalWeight = availableBrushes.Sum(t => t.Weight);
			var subCellBrushes = availableBrushes.Where(b => b.Contract().HasFlag(Replaceability.SubCellActor)).ToArray();
			var brushSubcellWeights = subCellBrushes.Select(o => o.Weight).ToArray();

			var fullSubcell = FullSubCell(map.Grid);

			// Give 1-by-1 actors the final pass, as they are most flexible.
			brushesByArea.Add(
				new KeyValuePair<int, List<MultiBrush>>(
					1,
					availableBrushes.Where(o => o.HasActors && o.Area == 1).ToList()));
			var size = map.MapSize;
			var replaceMposes = new List<MPos>();
			var remaining = new CellLayer<ActorSubCell>(map);
			for (var v = 0; v < size.Height; v++)
			{
				for (var u = 0; u < size.Width; u++)
				{
					var mpos = new MPos(u, v);
					if (mask[mpos] != Replaceability.None)
					{
						remaining[mpos] = ActorSubCell.Any;
						replaceMposes.Add(mpos);
					}
					else
						remaining[mpos] = ActorSubCell.FullCell;
				}
			}

			if (replaceMposes.Count == 0)
				return;

			var mposes = new MPos[replaceMposes.Count];
			int mposCount;
			void RefreshIndices()
			{
				mposCount = 0;
				foreach (var mpos in replaceMposes)
				{
					if (!remaining[mpos].HasFlag(fullSubcell))
					{
						mposes[mposCount] = mpos;
						mposCount++;
					}
				}

				random.ShuffleInPlace(mposes.AsSpan(), 0, mposCount);
			}

			foreach (var brushesKv in brushesByArea)
			{
				var brushes = brushesKv.Value;
				if (brushes.Count == 0)
					continue;

				var brushArea = brushes[0].Area;
				var brushWeights = brushes.Select(o => o.Weight).ToArray();
				var brushWeightForArea = brushWeights.Sum();
				var remainingQuota =
					(brushArea == 1 || alwaysPreferLargerBrushes)
						? int.MaxValue
						: (int)(((long)replaceMposes.Count * brushWeightForArea + brushTotalWeight - 1) / brushTotalWeight);
				RefreshIndices();
				for (var i = 0; i < mposCount; i++)
				{
					var mpos = mposes[i];
					var brush = brushes[random.PickWeighted(brushWeights)];
					var paintAt = mpos.ToCPos(map) - brush.FirstCell.FirstCell;
					var contract = ValidateBrushPlacement(brush, paintAt, mask, remaining, fullSubcell);

					if (contract.HasFlag(Replaceability.Tile))
						brush.PaintTilesOntoMap(map, paintAt, 0, c => remaining[c] = ActorSubCell.FullCell, random);

					void PaintSubCells(ActorPlan actorPlan) => PaintOntoFreeSubCells(brush, actorPlan, paintAt, remaining, map.Grid);

					if (contract.HasFlag(Replaceability.SubCellActor))
					{
						brush.PaintActorsOntoList(actorPlans, paintAt, PaintSubCells);
						for (var ii = 1; ii < map.Grid.SubCellOffsets.Length; ii++)
						{
							brush = subCellBrushes[random.PickWeighted(brushSubcellWeights)];
							if (ValidateBrushPlacement(brush, paintAt, mask, remaining, fullSubcell).HasFlag(Replaceability.SubCellActor))
								brush.PaintActorsOntoList(actorPlans, paintAt, PaintSubCells);
						}
					}
					else if (contract.HasFlag(Replaceability.Actor))
						brush.PaintActorsOntoList(actorPlans, paintAt, PaintSubCells);

					remainingQuota -= brushArea;
					if (remainingQuota <= 0)
						break;
				}
			}
		}

		/// <summary>
		/// Paint an area masked by <paramref name="mask"/> onto <paramref name="resultBrush"/> using <paramref name="availableBrushes"/>.
		/// </summary>
		/// <remarks>
		/// Painting is biased towards smaller features in several ways.
		/// 1. it's more likely to find a suitable position for a small feature.
		/// 2. we do a second pass, trying to fill in 1x1 gaps with actors.
		/// 3. sparsity is the applied first, meaning it can negate space for larger features.
		/// </remarks>
		public static void PaintAreaBrush(
			MultiBrush resultBrush,
			Map map,
			CellLayer<Replaceability> mask,
			IReadOnlyList<MultiBrush> availableBrushes,
			int sparsity,
			MersenneTwister random,
			bool alwaysPreferLargerBrushes = false,
			string actorOwner = null)
		{
			var brushesByAreaDict = new Dictionary<int, List<MultiBrush>>();
			foreach (var brush in availableBrushes)
			{
				if (!brushesByAreaDict.ContainsKey(brush.Area))
					brushesByAreaDict.Add(brush.Area, []);
				brushesByAreaDict[brush.Area].Add(brush);
			}

			var brushesByArea = brushesByAreaDict
				.OrderBy(kv => -kv.Key)
				.ToList();
			var brushTotalArea = availableBrushes.Sum(t => t.Area);
			var brushTotalWeight = availableBrushes.Sum(t => t.Weight);
			var subCellBrushes = availableBrushes.Where(b => b.Contract().HasFlag(Replaceability.SubCellActor)).ToArray();
			var brushSubcellProportion = subCellBrushes.Length * 1000 / availableBrushes.Count;
			var brushSubcellWeights = subCellBrushes.Select(o => o.Weight).ToArray();

			var fullSubcell = FullSubCell(map.Grid);

			// Give 1-by-1 actors the final pass, as they are most flexible.
			brushesByArea.Add(
				new KeyValuePair<int, List<MultiBrush>>(
					1,
					availableBrushes.Where(o => o.HasActors && o.Area == 1).ToList()));
			var size = mask.Size;
			var replaceMposes = new List<MPos>();
			var remaining = new CellLayer<ActorSubCell>(map.Grid.Type, size);
			for (var v = 0; v < size.Height; v++)
			{
				for (var u = 0; u < size.Width; u++)
				{
					var mpos = new MPos(u, v);
					var replaceability = mask[mpos];
					if (replaceability != Replaceability.None)
					{
						ActorSubCell chosen;
						if (sparsity == 0)
							chosen = ActorSubCell.Any;
						else if (brushSubcellProportion > 0 && replaceability.HasFlag(Replaceability.SubCellActor) && random.Next(1000) < brushSubcellProportion)
							chosen = RandomSubCellBits(map.Grid, sparsity, random);
						else if (random.Next(1000) >= sparsity)
							chosen = ActorSubCell.Any;
						else
							chosen = ActorSubCell.FullCell;

						remaining[mpos] = chosen;

						if (!chosen.HasFlag(fullSubcell))
							replaceMposes.Add(mpos);
					}
					else
						remaining[mpos] = ActorSubCell.FullCell;
				}
			}

			if (replaceMposes.Count == 0)
				return;

			var mposes = new MPos[replaceMposes.Count];
			int mposCount;
			void RefreshIndices()
			{
				mposCount = 0;
				foreach (var mpos in replaceMposes)
				{
					if (!remaining[mpos].HasFlag(fullSubcell))
					{
						mposes[mposCount] = mpos;
						mposCount++;
					}
				}

				random.ShuffleInPlace(mposes.AsSpan(), 0, mposCount);
			}

			foreach (var brushesKv in brushesByArea)
			{
				var brushes = brushesKv.Value;
				if (brushes.Count == 0)
					continue;

				var brushArea = brushes[0].Area;
				var brushWeights = brushes.Select(o => o.Weight).ToArray();
				var brushWeightForArea = brushWeights.Sum();
				var remainingQuota =
					(brushArea == 1 || alwaysPreferLargerBrushes)
						? int.MaxValue
						: (int)(((long)replaceMposes.Count * brushWeightForArea + brushTotalWeight - 1) / brushTotalWeight);
				RefreshIndices();
				for (var i = 0; i < mposCount; i++)
				{
					var mpos = mposes[i];
					var brush = brushes[random.PickWeighted(brushWeights)];
					var paintAt = mpos.ToCPos(map) - brush.FirstCell.FirstCell;
					var contract = ValidateBrushPlacement(brush, paintAt, mask, remaining, fullSubcell);
					if (contract.HasFlag(Replaceability.Tile))
						brush.PaintTilesOntoBrush(resultBrush, paintAt, c => remaining[c] = ActorSubCell.FullCell, random);

					void PaintSubCells(ActorPlan actorPlan) => PaintOntoFreeSubCells(brush, actorPlan, paintAt, remaining, map.Grid);

					if (contract.HasFlag(Replaceability.SubCellActor))
					{
						brush.PaintActorsOntoBrush(resultBrush, paintAt, PaintSubCells, actorOwner);
						for (var ii = 1; ii < map.Grid.SubCellOffsets.Length; ii++)
						{
							brush = subCellBrushes[random.PickWeighted(brushSubcellWeights)];
							if (ValidateBrushPlacement(brush, paintAt, mask, remaining, fullSubcell).HasFlag(Replaceability.SubCellActor))
								brush.PaintActorsOntoBrush(resultBrush, paintAt, PaintSubCells, actorOwner);
						}
					}
					else if (contract.HasFlag(Replaceability.Actor))
						brush.PaintActorsOntoBrush(resultBrush, paintAt, PaintSubCells, actorOwner);

					remainingQuota -= brushArea;
					if (remainingQuota <= 0)
						break;
				}
			}

			resultBrush.shape = null;
		}

		/// <summary>
		/// Create a sparse EditorBlitSource from this MultiBrush. The EditorBlitSource will have
		/// the minimum bounding CellRegion fully containing all content. An optional
		/// MersenneTwister can be provided to vary randomizable elements. For actors without a
		/// preconfigured owner, a default owner can be specified or derived automatically.
		/// </summary>
		public EditorBlitSource ToEditorBlitSource(
			WorldRenderer worldRenderer,
			MersenneTwister random,
			CellCoordsRegion region,
			PlayerReference defaultActorOwner = null,
			short heightOffset = 0)
		{
			var world = worldRenderer.World;
			var map = world.Map;

			if (defaultActorOwner == null)
			{
				var editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
				if (editorActorLayer != null)
					defaultActorOwner = editorActorLayer.Players.Players.Values.First();
			}

			var players = world.Players.ToDictionary(
				player => player.InternalName,
				player => player.PlayerReference);

			var actorPreviews = new Dictionary<string, EditorActorPreview>();
			for (var i = 0; i < actorPlans.Count; i++)
			{
				// A (non-revert) EditorBlitSource's actors' names are generally unimportant beyond
				// needing to be distinct. They will get renamed when blitting.
				var name = $"Actor{i}";
				var actorReference = actorPlans[i].Reference.Clone();
				var ownerInit = actorReference.Get<OwnerInit>();
				if (!players.TryGetValue(ownerInit.InternalName, out var owner))
					owner = defaultActorOwner;

				if (owner == null)
					throw new InvalidOperationException("MultiBrush actor has invalid (or no) owner and no default available.");

				actorPreviews[name] = new EditorActorPreview(
					worldRenderer,
					name,
					actorReference,
					owner);
			}

			var terrainInfo = world.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			var blitTiles =
				tiles
					.Where(t => map.Tiles.Contains(CPos.Zero + t.XY))
					.DistinctBy(t => t.XY)
					.Select(t => (t.XY, Tile: t.TileRange.Pick(random), t.TileRange.HeightOffset))
					.ToDictionary(
						t => CPos.Zero + t.XY,
						t => new BlitTile(t.Tile, default, null, (byte)Math.Clamp(heightOffset + t.HeightOffset, byte.MinValue, byte.MaxValue)));

			return new EditorBlitSource(
				region,
				actorPreviews,
				blitTiles);
		}

		/// <summary>All possible tiles that may be painted by this <see cref="MultiBrush"/>.</summary>
		public HashSet<TerrainTile> PossibleTiles()
		{
			var possible = new HashSet<TerrainTile>();
			foreach (var (_, tileRange) in tiles)
				for (int i = tileRange.MinIndex; i <= tileRange.MaxIndex; i++)
					possible.Add(new(tileRange.Type, (byte)i));
			return possible;
		}

		/// <summary>Pick a random brush from a list, respecting brush weights.</summary>
		public static MultiBrush PickAny(IReadOnlyList<MultiBrush> brushes, MersenneTwister random)
		{
			if (brushes.Count == 0)
				throw new ArgumentException("brushes was empty");

			if (brushes.Count == 1)
				return brushes[0];

			var weights = new int[brushes.Count];
			for (var i = 0; i < weights.Length; i++)
				weights[i] = brushes[i].Weight;

			return brushes[random.PickWeighted(weights)];
		}

		/// <summary> Returns a <see cref="CellCoordsRegion"/> that contains all cells covered by the <see cref="MultiBrush"/>. </summary>
		public CellCoordsRegion GetCellCoordsRegion()
		{
			var topLeft = new CPos(
				Shape.Min(tuple => tuple.Vec.X),
				Shape.Min(tuple => tuple.Vec.Y));
			var bottomRight = new CPos(
				Shape.Max(tuple => tuple.Vec.X),
				Shape.Max(tuple => tuple.Vec.Y));

			return new CellCoordsRegion(topLeft, bottomRight);
		}

		/// <summary> Adds an <paramref name="offset"/> to all tiles and actor plans in the <see cref="MultiBrush"/>. </summary>
		public void AddOffset(CVec offset)
		{
			for (var i = 0; i < tiles.Count; i++)
			{
				var (xy, tileRange) = tiles[i];
				tiles[i] = (xy + offset, tileRange);
			}

			for (var i = 0; i < actorPlans.Count; i++)
				actorPlans[i].Location += offset;

			shape = null;
		}

		/// <summary> Removes the offset of the <see cref="MultiBrush"/>, resetting it to the top-left corner of the bounding region. </summary>
		public void RemoveOffset()
		{
			var offset = GetCellCoordsRegion().TopLeft - CPos.Zero;
			AddOffset(-offset);
		}

		/// <summary>Updates the owner of all actors in the <see cref="MultiBrush"/>.</summary>
		public void UpdateOwner(string owner)
		{
			for (var i = 0; i < actorPlans.Count; i++)
			{
				var actorPlan = actorPlans[i];
				actorPlan.Owner = owner;
			}
		}

		/// <summary>
		/// Get the highest cell height in a MultiBrush collection. Does not consider ramps.
		/// </summary>
		public static byte MaxHeightOfBrushes(IEnumerable<MultiBrush> brushes)
		{
			return brushes
				.SelectMany(b => b.GetHeightsAndRamps())
				.Max(v => (byte)v.Height);
		}

		/// <summary>
		/// Get the highest cell height in a MultiBrush collection filtered by segment inner type.
		/// Does not consider ramps.
		/// </summary>
		public static byte MaxHeightOfSegmentType(string type, IEnumerable<MultiBrush> brushes)
		{
			return MaxHeightOfBrushes(
				brushes.Where(b => b.Segment?.HasInnerType(type) ?? false));
		}
	}
}
