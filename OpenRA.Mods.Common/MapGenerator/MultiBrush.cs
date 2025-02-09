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
using OpenRA.Mods.Common.Terrain;
using OpenRA.Support;

namespace OpenRA.Mods.Common.MapGenerator
{
	/// <summary>
	/// MiniYaml-loaded definition of a MultiBrush. Can be loaded into a MultiBrush once a map is
	/// available.
	/// </summary>
	public sealed class MultiBrushInfo
	{
		public readonly int Weight;
		public readonly ImmutableArray<string> Actors;
		public readonly TerrainTile? BackingTile;
		public readonly ImmutableArray<ushort> Templates;
		public readonly ImmutableArray<TerrainTile> Tiles;

		// Currently doesn't support specifying offsets. Add this capability if/when needed.
		public MultiBrushInfo(MiniYaml my)
		{
			Weight = MultiBrush.DefaultWeight;
			var actors = new List<string>();
			var templates = new List<ushort>();
			var tiles = new List<TerrainTile>();
			foreach (var node in my.Nodes)
				switch (node.Key.Split('@')[0])
				{
					case "Weight":
						if (!Exts.TryParseInt32Invariant(node.Value.Value, out Weight))
							throw new YamlException($"Invalid MultiBrush Weight `${node.Value.Value}`");
						break;
					case "Actor":
						actors.Add(node.Value.Value);
						break;
					case "BackingTile":
						if (TerrainTile.TryParse(node.Value.Value, out var backingTile))
							BackingTile = backingTile;
						else
							throw new YamlException($"Invalid MultiBrush BackingTile `${node.Value.Value}`");
						break;
					case "Template":
						if (Exts.TryParseUshortInvariant(node.Value.Value, out var template))
							templates.Add(template);
						else
							throw new YamlException($"Invalid MultiBrush Template `${node.Value.Value}`");
						break;
					case "Tile":
						if (TerrainTile.TryParse(node.Value.Value, out var tile))
							Tiles.Add(tile);
						else
							throw new YamlException($"Invalid MultiBrush Tile `${node.Value.Value}`");
						break;
					default:
						throw new YamlException($"Unrecognized MultiBrush key {node.Key.Split('@')[0]}");
				}

			Actors = actors.ToImmutableArray();
			Templates = templates.ToImmutableArray();
			Tiles = tiles.ToImmutableArray();
		}

		public static ImmutableArray<MultiBrushInfo> ParseCollection(MiniYaml my)
		{
			var brushes = new List<MultiBrushInfo>();
			foreach (var node in my.Nodes)
				if (node.Key.Split('@')[0] == "MultiBrush")
					brushes.Add(new MultiBrushInfo(node.Value));
				else
					throw new YamlException($"Expected `MultiBrush@*` but got `{node.Key}`");
			return brushes.ToImmutableArray();
		}
	}

	/// <summary>A super template that can be used to paint both tiles and actors.</summary>
	sealed class MultiBrush
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

		public int Weight;
		readonly List<(CVec, TerrainTile)> tiles;
		readonly List<ActorPlan> actorPlans;
		CVec[] shape;

		public IEnumerable<(CVec XY, TerrainTile Tile)> Tiles => tiles;
		public IEnumerable<ActorPlan> ActorPlans => actorPlans;
		public bool HasTiles => tiles.Count != 0;
		public bool HasActors => actorPlans.Count != 0;
		public IEnumerable<CVec> Shape => shape;
		public int Area => shape.Length;
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
			tiles = new List<(CVec, TerrainTile)>();
			actorPlans = new List<ActorPlan>();
			shape = Array.Empty<CVec>();
		}

		MultiBrush(MultiBrush other)
		{
			Weight = other.Weight;
			tiles = new List<(CVec, TerrainTile)>(other.tiles);
			actorPlans = new List<ActorPlan>(other.actorPlans);
			shape = other.shape.ToArray();
		}

		public MultiBrush(Map map, MultiBrushInfo info)
			: this()
		{
			WithWeight(info.Weight);
			foreach (var actor in info.Actors)
				WithActor(new ActorPlan(map, actor).AlignFootprint());
			if (info.BackingTile != null)
				WithBackingTile((TerrainTile)info.BackingTile);
			foreach (var template in info.Templates)
				WithTemplate(map, template);
			foreach (var tile in info.Tiles)
				WithTile(tile);
		}

		/// <summary>Load a named MultiBrush collection from a map's tileset.</summary>
		public static ImmutableArray<MultiBrush> LoadCollection(Map map, string name)
		{
			var templatedTerrainInfo = map.Rules.TerrainInfo as ITemplatedTerrainInfo;
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
				shape = new[] { new CVec(0, 0) };
		}

		/// <summary>
		/// Add tiles from a template, optionally with a given offset. By
		/// default, it will be auto-offset such that the first tile is
		/// under (0, 0).
		/// </summary>
		public MultiBrush WithTemplate(Map map, ushort templateId, CVec? offset = null)
		{
			var tileset = map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			if (!tileset.Templates.TryGetValue(templateId, out var templateInfo))
				throw new ArgumentException($"Map's tileset does not contain template with ID {templateId}.");
			return WithTemplate(templateInfo, offset);
		}

		public MultiBrush WithTemplate(TerrainTemplateInfo templateInfo, CVec? offset = null)
		{
			if (templateInfo.PickAny)
				throw new ArgumentException("PickAny not supported - create separate MultiBrushes using WithTile instead.");
			for (var y = 0; y < templateInfo.Size.Y; y++)
				for (var x = 0; x < templateInfo.Size.X; x++)
				{
					var i = y * templateInfo.Size.X + x;
					if (templateInfo[i] != null)
					{
						if (offset == null)
							offset = new CVec(-x, -y);
						var tile = new TerrainTile(templateInfo.Id, (byte)i);
						tiles.Add((new CVec(x, y) + (CVec)offset, tile));
					}
				}

			UpdateShape();
			return this;
		}

		/// <summary>
		/// Add a single tile, optionally with a given offset. By default, it
		/// will be positioned under (0, 0).
		/// </summary>
		public MultiBrush WithTile(TerrainTile tile, CVec? offset = null)
		{
			tiles.Add((offset ?? new CVec(0, 0), tile));
			UpdateShape();
			return this;
		}

		/// <summary>Add an actor (using the ActorPlan's location as an offset).</summary>
		public MultiBrush WithActor(ActorPlan actor)
		{
			actorPlans.Add(actor);
			UpdateShape();
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
				tiles.Add((xy, tile));

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
		/// <para>Paint tiles onto the map and/or add actors to actorPlans at the given location.</para>
		/// <para>contract specifies whether tiles or actors are allowed to be painted.</para>
		/// <para>If nothing could be painted, throws ArgumentException.</para>
		/// </summary>
		public void Paint(Map map, List<ActorPlan> actorPlans, CPos paintAt, Replaceability contract)
		{
			switch (contract)
			{
				case Replaceability.None:
					throw new ArgumentException("Cannot paint: Replaceability.None");
				case Replaceability.Any:
					if (this.actorPlans.Count > 0)
						PaintActors(map, actorPlans, paintAt);
					else if (tiles.Count > 0)
						PaintTiles(map, paintAt);
					else
						throw new ArgumentException("Cannot paint: no tiles or actors");
					break;
				case Replaceability.Tile:
					if (tiles.Count == 0)
						throw new ArgumentException("Cannot paint: no tiles");
					PaintTiles(map, paintAt);
					PaintActors(map, actorPlans, paintAt);
					break;
				case Replaceability.Actor:
					if (this.actorPlans.Count == 0)
						throw new ArgumentException("Cannot paint: no actors");
					PaintActors(map, actorPlans, paintAt);
					break;
			}
		}

		void PaintTiles(Map map, CPos paintAt)
		{
			foreach (var (xy, tile) in tiles)
			{
				var mpos = (paintAt + xy).ToMPos(map);
				if (map.Tiles.Contains(mpos))
					map.Tiles[mpos] = tile;
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
			bool alwaysPreferLargerBrushes = false)
		{
			var brushesByAreaDict = new Dictionary<int, List<MultiBrush>>();
			foreach (var brush in availableBrushes)
			{
				if (!brushesByAreaDict.ContainsKey(brush.Area))
					brushesByAreaDict.Add(brush.Area, new List<MultiBrush>());
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
			for (var v = 0; v < size.Y; v++)
				for (var u = 0; u < size.X; u++)
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

			var mposes = new MPos[size.X * size.Y];
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

				random.ShuffleInPlace(mposes, 0, mposCount);
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
					var paintAt = mpos.ToCPos(map);
					var contract = ReserveShape(paintAt, brush.Shape, brush.Contract());
					if (contract != Replaceability.None)
						brush.Paint(map, actorPlans, paintAt, contract);

					remainingQuota -= brushArea;
					if (remainingQuota <= 0)
						break;
				}
			}
		}
	}
}
