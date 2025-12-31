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

		public enum Replaceability
		{
			/// <summary>Area cannot be replaced by a tile or obstructing actor.</summary>
			None = 0,

			/// <summary>Area must be replaced by a different tile, and may optionally be given an actor.</summary>
			Tile = 1,

			/// <summary>Area must be given an actor, but the underlying tile must not change.</summary>
			Actor = 2,

			/// <summary>Area can be replaced by a tile and/or actor.</summary>
			Any = 3,
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
		CVec[] shape;

		public bool HasTiles => tiles.Count != 0;
		public bool HasActors => actorPlans.Count != 0;
		public IEnumerable<CVec> Shape => GetShape();

		/// <summary>Total area covered by the MultiBrush.</summary>
		public int Area => GetShape().Length;

		/// <summary>
		/// The CVec of the first cell covered by the MultiBrush. This is the left-most cell in the
		/// top-row. Note that this does not necessarily correspond to the top-left corner of the
		/// rectangular bounds of the MultiBrush.
		/// </summary>
		public CVec FirstCell => GetShape()[0];

		public IEnumerable<(CVec XY, short Height, byte Ramp)> GetHeightsAndRamps()
		{
			return tiles.Select(t => (t.XY, t.TileRange.HeightOffset, t.TileRange.Ramp));
		}

		public Replaceability Contract()
		{
			var hasTiles = tiles.Count != 0;
			var hasActorPlans = actorPlans.Count != 0;
			if (hasTiles && hasActorPlans)
				return Replaceability.Any;
			else if (hasTiles && !hasActorPlans)
				return Replaceability.Tile;
			else if (!hasTiles && hasActorPlans)
				return Replaceability.Actor;
			else
				return Replaceability.None;
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
			var xys = new HashSet<CVec>();

			foreach (var (xy, _) in tiles)
				xys.Add(xy);

			foreach (var actorPlan in actorPlans)
				foreach (var cpos in actorPlan.Footprint().Keys)
					xys.Add(new CVec(cpos.X, cpos.Y));

			if (xys.Count != 0)
				shape = xys.OrderBy(xy => (xy.Y, xy.X)).ToArray();
			else
				shape = [new CVec(0, 0)];
		}

		CVec[] GetShape()
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
			foreach (var xy in shape)
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
		/// <para>Paint tiles onto the map and/or add actors to actorPlans at the given location.</para>
		/// <para>A specific height offset can be supplied, else one will be assumed from the map.</para>
		/// <para>contract specifies whether tiles or actors are allowed to be painted.</para>
		/// <para>An optional MersenneTwister can be provided to vary randomizable elements.</para>
		/// <para>If nothing could be painted, throws ArgumentException.</para>
		/// </summary>
		public void Paint(
			Map map,
			List<ActorPlan> actorPlans,
			CPos paintAt,
			short? heightOffset,
			Replaceability contract,
			MersenneTwister random)
		{
			short finalHeightOffset = 0;
			if (heightOffset.HasValue)
			{
				finalHeightOffset = heightOffset.Value;
			}
			else
			{
				foreach (var cpos in Shape)
				{
					if (map.Height.Contains(paintAt + cpos))
					{
						finalHeightOffset = map.Height[paintAt + cpos];
						break;
					}
				}
			}

			switch (contract)
			{
				case Replaceability.None:
					throw new ArgumentException("Cannot paint: Replaceability.None");
				case Replaceability.Any:
					PaintTiles(map, paintAt, finalHeightOffset, random);
					PaintActors(map, actorPlans, paintAt);

					break;
				case Replaceability.Tile:
					if (tiles.Count == 0)
						throw new ArgumentException("Cannot paint: no tiles");

					PaintTiles(map, paintAt, finalHeightOffset, random);
					PaintActors(map, actorPlans, paintAt);
					break;
				case Replaceability.Actor:
					if (this.actorPlans.Count == 0)
						throw new ArgumentException("Cannot paint: no actors");

					PaintActors(map, actorPlans, paintAt);
					break;
			}
		}

		void PaintTiles(Map map, CPos paintAt, short heightOffset, MersenneTwister random)
		{
			foreach (var (xy, tile) in tiles)
			{
				var mpos = (paintAt + xy).ToMPos(map);
				if (map.Tiles.Contains(mpos))
				{
					// map.Ramp does not need to be updated here.
					map.Tiles[mpos] = tile.Pick(random);
					map.Height[mpos] = (byte)Math.Clamp(tile.HeightOffset + heightOffset, byte.MinValue, byte.MaxValue);
				}
			}
		}

		void PaintActors(Map map, List<ActorPlan> actorPlans, CPos paintAt)
		{
			foreach (var actorPlan in this.actorPlans)
			{
				if (map != actorPlan.Map)
					throw new ArgumentException("ActorPlan is for a different map");
				var plan = actorPlan.Clone();
				var offset = plan.Location;
				plan.Location = paintAt + new CVec(offset.X, offset.Y);
				actorPlans.Add(plan);
			}
		}

		/// <summary>
		/// Paint an area defined by replace onto map and actorPlans using availableBrushes.
		/// </summary>
		public static void PaintArea(
			Map map,
			List<ActorPlan> actorPlans,
			CellLayer<Replaceability> replace,
			IReadOnlyList<MultiBrush> availableBrushes,
			MersenneTwister random,
			bool alwaysPreferLargerBrushes = false,
			short? heightOffset = null)
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

			// Give 1-by-1 actors the final pass, as they are most flexible.
			brushesByArea.Add(
				new KeyValuePair<int, List<MultiBrush>>(
					1,
					availableBrushes.Where(o => o.HasActors && o.Area == 1).ToList()));
			var size = map.MapSize;
			var replaceMposes = new List<MPos>();
			var remaining = new CellLayer<bool>(map);
			for (var v = 0; v < size.Height; v++)
			{
				for (var u = 0; u < size.Width; u++)
				{
					var mpos = new MPos(u, v);
					if (replace[mpos] != Replaceability.None)
					{
						remaining[mpos] = true;
						replaceMposes.Add(mpos);
					}
					else
					{
						remaining[mpos] = false;
					}
				}
			}

			var mposes = new MPos[size.Width * size.Height];
			int mposCount;

			void RefreshIndices()
			{
				mposCount = 0;
				foreach (var mpos in replaceMposes)
					if (remaining[mpos])
					{
						mposes[mposCount] = mpos;
						mposCount++;
					}

				random.ShuffleInPlace(mposes.AsSpan(), 0, mposCount);
			}

			Replaceability ReserveShape(CPos paintAt, IEnumerable<CVec> shape, Replaceability contract)
			{
				foreach (var cvec in shape)
				{
					var cpos = paintAt + cvec;
					if (!replace.Contains(cpos))
						continue;
					if (!remaining[cpos])
					{
						// Can't reserve - not the right shape
						return Replaceability.None;
					}

					contract &= replace[cpos];
					if (contract == Replaceability.None)
					{
						// Can't reserve - obstruction choice doesn't comply
						// with replaceability of original tiles.
						return Replaceability.None;
					}
				}

				// Can reserve. Commit.
				foreach (var cvec in shape)
				{
					var cpos = paintAt + cvec;
					if (!replace.Contains(cpos))
						continue;

					remaining[cpos] = false;
				}

				return contract;
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
				foreach (var mpos in mposes)
				{
					var brush = brushes[random.PickWeighted(brushWeights)];
					var paintAt = mpos.ToCPos(map) - brush.FirstCell;
					var contract = ReserveShape(paintAt, brush.Shape, brush.Contract());
					if (contract != Replaceability.None)
						brush.Paint(map, actorPlans, paintAt, heightOffset, contract, random);

					remainingQuota -= brushArea;
					if (remainingQuota <= 0)
						break;
				}
			}
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

			var topLeft = new CPos(
				Shape.Min(cvec => cvec.X),
				Shape.Min(cvec => cvec.Y));
			var bottomRight = new CPos(
				Shape.Max(cvec => cvec.X),
				Shape.Max(cvec => cvec.Y));
			var cellRegion = new CellRegion(map.Grid.Type, topLeft, bottomRight);

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

			var blitTiles =
				tiles
					.Where(t => map.Tiles.Contains(CPos.Zero + t.XY))
					.DistinctBy(t => t.XY)
					.Select(t => (t.XY, Tile: t.TileRange.Pick(random), t.TileRange.HeightOffset))
					.ToDictionary(
						t => CPos.Zero + t.XY,
						t => new BlitTile(t.Tile, default, null, (byte)Math.Clamp(heightOffset + t.HeightOffset, byte.MinValue, byte.MaxValue)));

			return new EditorBlitSource(
				cellRegion,
				actorPreviews,
				blitTiles);
		}

		/// <summary>All possible tiles that may be painted by this MultiBrush.</summary>
		public HashSet<TerrainTile> PossibleTiles()
		{
			var possible = new HashSet<TerrainTile>();
			foreach (var (_, tileRange) in tiles)
				for (int i = tileRange.MinIndex; i <= tileRange.MaxIndex; i++)
					possible.Add(new(tileRange.Type, (byte)i));
			return possible;
		}
	}
}
