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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using static OpenRA.Mods.Common.Traits.ResourceLayerInfo;

namespace OpenRA.Mods.Common.MapGenerator
{
	/// <summary>Collection of high-level map generation utilities.</summary>
	public class Terraformer
	{
		/// <summary>Common denominator for fractional arguments.</summary>
		public const int FractionMax = 1000;

		/// <summary>Biases or excludes resources at a location during resource planning.</summary>
		public sealed class ResourceBias
		{
			/// <summary>The location of the bias.</summary>
			public WPos WPos;

			/// <summary>Resources will not be placed within this distance of the actor.</summary>
			public WDist? ExclusionRadius = null;

			/// <summary>Resources will be biased within this radius.</summary>
			public WDist? BiasRadius = null;

			/// <summary>
			/// Biasing function, applied either to all resources or the specific ResourceType.
			/// Maps the original value and the squared-WDist-from-CPos to a new value.
			/// </summary>
			public Func<int, long, int> Bias = null;

			/// <summary>If non-null, encourages resources to become this type.</summary>
			public ResourceTypeInfo ResourceType = null;

			/// <summary>Create a bias at a location.</summary>
			public ResourceBias(WPos wpos)
			{
				WPos = wpos;
			}

			/// <summary>Create a bias at an actor's location.</summary>
			public ResourceBias(ActorPlan actorPlan)
				: this(actorPlan.WPosCenterLocation)
			{ }
		}

		/// <summary>
		/// Metadata for the values in a CellLayer matching an ID.
		/// </summary>
		public sealed class Region
		{
			public const int NullId = -1;

			/// <summary>Region ID.</summary>
			public int Id;

			/// <summary>Area of the region.</summary>
			public int Area;
		}

		public sealed class PathPartitionZone
		{
			public bool ShouldTile = true;
			public bool RequiredSomewhere = false;
			public string SegmentType = null;
			public int MinimumLength = 1;
			public int MaximumDeviation = 0;
		}

		public enum Side : sbyte
		{
			Out = -1,
			None = 0,
			In = 1,
		}

		public static (T[] Types, U[] Weights) SplitDictionary<T, U>(IReadOnlyDictionary<T, U> typeWeights)
		{
			var types = typeWeights
				.Select(kv => kv.Key)
				.Order()
				.ToArray();
			var weights = types
				.Select(type => typeWeights[type])
				.ToArray();
			return (types, weights);
		}

		public readonly MapGenerationArgs MapGenerationArgs;
		public readonly Map Map;
		public readonly ModData ModData;
		public readonly List<ActorPlan> ActorPlans;
		public readonly Symmetry.Mirror Mirror;
		public readonly int Rotations;

		readonly ITerrainInfo terrainInfo;

		// Will be null if terrainInfo isn't a ITemplatedTerrainInfo. Some methods assume that the
		// terrainInfo is an ITemplatedTerrainInfo.
		readonly ITemplatedTerrainInfo templatedTerrainInfo;
		readonly Lazy<CellLayer<int>> lazyProjectionSpacing;

		public Terraformer(
			MapGenerationArgs mapGenerationArgs,
			Map map,
			ModData modData,
			List<ActorPlan> actorPlans,
			Symmetry.Mirror mirror,
			int rotations)
		{
			MapGenerationArgs = mapGenerationArgs;
			Map = map;
			ModData = modData;
			ActorPlans = actorPlans;
			Mirror = mirror;
			Rotations = rotations;

			terrainInfo = modData.DefaultTerrainInfo[map.Tileset];
			templatedTerrainInfo = terrainInfo as ITemplatedTerrainInfo;

			lazyProjectionSpacing = new(ProjectionSpacing);
		}

		public void CheckHasMapShapeOrNull<T>(CellLayer<T> layer)
		{
			if (layer != null)
				CheckHasMapShape(layer);
		}

		public void CheckHasMapShapeOrNull<T>(Matrix<T> layer)
		{
			if (layer != null)
				CheckHasMapShape(layer);
		}

		public void CheckHasMapShape<T>(CellLayer<T> layer)
		{
			if (!CellLayerUtils.AreSameShape(layer, Map.Tiles))
				throw new ArgumentException("CellLayer has different shape to map");
		}

		public void CheckHasMapShape<T>(Matrix<T> matrix)
		{
			var cellBounds = CellLayerUtils.CellBounds(Map);
			var size = cellBounds.Size.ToInt2();
			if (matrix.Size != size)
				throw new ArgumentException("Matrix has different shape to map");
		}

		/// <summary>
		/// Enumerates through all current ActorPlans of the given type.
		/// </summary>
		public IEnumerable<ActorPlan> ActorsOfType(string type)
		{
			return ActorPlans.Where(a => a.Reference.Type == type);
		}

		/// <summary>Perform some basic initialization of a map.</summary>
		public void InitMap()
		{
			var maxTerrainHeight = Map.Grid.MaximumTerrainHeight;
			var tl = new PPos(1, 1 + maxTerrainHeight);
			var br = new PPos(Map.MapSize.Width - 2, Map.MapSize.Height + maxTerrainHeight - 2);
			Map.SetBounds(tl, br);
			Map.Title = MapGenerationArgs.Title;
			Map.Author = MapGenerationArgs.Author;
			Map.RequiresMod = ModData.Manifest.Id;
		}

		/// <summary>
		/// Commits draft data to the map, such as player and actor definitions.
		/// </summary>
		public void BakeMap()
		{
			var playerCount = ActorsOfType("mpspawn").Count();
			Map.PlayerDefinitions = new MapPlayers(Map.Rules, playerCount).ToMiniYaml();
			Map.ActorDefinitions = ActorPlans
				.Select((plan, i) => new MiniYamlNode($"Actor{i}", plan.Reference.Save()))
				.ToImmutableArray();
		}

		/// <summary>
		/// Return a new CellLayer produced by aggregating projected cells from an input CellLayer.
		/// The input does not need to have the same shape as the map.
		/// </summary>
		public CellLayer<T> ImproveSymmetry<T>(
			CellLayer<T> layer,
			T outsideValue,
			Func<T, T, T> aggregator)
		{
			var newLayer = new CellLayer<T>(layer.GridType, layer.Size);
			Symmetry.RotateAndMirrorOverCPos(
				layer,
				Rotations,
				Mirror,
				(sources, destination)
					=> newLayer[destination] = sources
						.Select(source => layer.TryGetValue(source, out var value) ? value : outsideValue)
						.Aggregate(aggregator));
			return newLayer;
		}

		/// <summary>
		/// Subtract an actor's footprint from zoneable. Optionally, a circle with a given dezone
		/// radius from the actor center can also be subtracted from zoneable.
		/// </summary>
		public void DezoneActor(
			ActorPlan actorPlan,
			CellLayer<bool> zoneable,
			WDist? dezoneRadius = null)
		{
			CheckHasMapShape(zoneable);

			foreach (var (cpos, _) in actorPlan.Footprint())
				if (zoneable.Contains(cpos))
					zoneable[cpos] = false;

			if (dezoneRadius.HasValue)
			{
				CellLayerUtils.OverCircle(
					cellLayer: zoneable,
					wCenter: actorPlan.WPosCenterLocation,
					wRadius: dezoneRadius.Value,
					outside: false,
					action: (mpos, _, _, _) => zoneable[mpos] = false);
			}
		}

		/// <summary>Sets all zoneable cells where the map has actor footprints to false.</summary>
		public void ZoneFromActors<T>(CellLayer<T> zoneable, T value)
		{
			foreach (var actorPlan in ActorPlans)
				foreach (var (cpos, _) in actorPlan.Footprint())
					if (zoneable.Contains(cpos))
						zoneable[cpos] = value;
		}

		/// <summary>Sets all zoneable cells where the map has resources to false.</summary>
		public void ZoneFromResources<T>(CellLayer<T> zoneable, T value)
		{
			CheckHasMapShape(zoneable);

			foreach (var mpos in Map.AllCells.MapCoords)
				if (Map.Resources[mpos].Type != 0)
					zoneable[mpos] = value;
		}

		public void ZoneFromOutOfBounds<T>(CellLayer<T> zoneable, T value)
		{
			foreach (var mpos in Map.AllCells.MapCoords)
				if (!Map.Contains(mpos))
					zoneable[mpos] = value;
		}

		/// <summary>
		/// Returns a CellLayer describing whether the space in a map satisfies given terrain types
		/// (if allowedTerrain is non-null), is free of actors, and/or is free of resources.
		/// </summary>
		public CellLayer<bool> CheckSpace(
			IReadOnlySet<byte> allowedTerrain,
			bool checkActors = false,
			bool checkResources = false,
			bool checkBounds = false)
		{
			var space = new CellLayer<bool>(Map);
			if (allowedTerrain != null)
			{
				foreach (var mpos in Map.AllCells.MapCoords)
					space[mpos] = allowedTerrain.Contains(terrainInfo.GetTerrainIndex(Map.Tiles[mpos]));
			}
			else
			{
				space.Clear(true);
			}

			if (checkActors)
				ZoneFromActors(space, false);

			if (checkResources)
				ZoneFromResources(space, false);

			if (checkBounds)
				ZoneFromOutOfBounds(space, false);

			return space;
		}

		/// <summary>
		/// Returns a CellLayer describing whether the space in a map has the given tile type and
		/// is free of actors and/or resources.
		/// </summary>
		public CellLayer<bool> CheckSpace(
			ushort requiredTile,
			bool checkActors = false,
			bool checkResources = false,
			bool checkBounds = false)
		{
			var space = new CellLayer<bool>(Map);
			foreach (var mpos in Map.AllCells.MapCoords)
				space[mpos] = Map.Tiles[mpos].Type == requiredTile;

			if (checkActors)
				ZoneFromActors(space, false);

			if (checkResources)
				ZoneFromResources(space, false);

			if (checkBounds)
				ZoneFromOutOfBounds(space, false);

			return space;
		}

		/// <summary>
		/// Shrink zoneable areas by a given thickness in cells. Zones will be shrunk even if they
		/// border the edge of the map.
		/// </summary>
		public CellLayer<bool> ErodeZones(CellLayer<bool> zoneable, int amount)
		{
			CheckHasMapShape(zoneable);
			var roominess = new CellLayer<int>(Map);
			CellLayerUtils.ChebyshevRoom(roominess, zoneable, false);
			return CellLayerUtils.Map(roominess, r => r > amount);
		}

		/// <summary>
		/// Derives a CellLayer identifying the space in a map available for various actors,
		/// resources, decorations, etc. A mask (usually playable space) can be used to further
		/// limit the zoneable area.
		/// </summary>
		public CellLayer<bool> GetZoneable(
			IReadOnlySet<byte> zoneableTerrain,
			CellLayer<bool> mask = null)
		{
			CheckHasMapShapeOrNull(mask);

			var zoneable = CheckSpace(zoneableTerrain, true, true, true);
			if (mask != null)
				zoneable = CellLayerUtils.Intersect([zoneable, mask]);

			if (Rotations > 1 || Mirror != Symmetry.Mirror.None)
			{
				// Reserve the center of the map - otherwise it will mess with symmetries
				CellLayerUtils.OverCircle(
					cellLayer: zoneable,
					wCenter: CellLayerUtils.Center(Map),
					wRadius: new WDist(1024),
					outside: false,
					action: (mpos, _, _, _) => zoneable[mpos] = false);
			}

			zoneable = ImproveSymmetry(zoneable, false, (a, b) => a && b);

			return zoneable;
		}

		/// <summary>Create map-shaped CellLayer preinitialized with a circle.</summary>
		public CellLayer<T> CenteredCircle<T>(T inside, T outside, WDist radius)
		{
			var circle = new CellLayer<T>(Map);
			circle.Clear(outside);
			CellLayerUtils.OverCircle(
				cellLayer: circle,
				wCenter: CellLayerUtils.Center(Map),
				wRadius: radius,
				outside: false,
				action: (mpos, _, _, _) => circle[mpos] = inside);
			return circle;
		}

		/// <summary>
		/// Return a CellLayer where each cell is half the minimum distances to one of its symmetry
		/// projections. Can be used to avoid placing actors too close to their own projections.
		/// </summary>
		public CellLayer<int> ProjectionSpacing()
		{
			var projectionSpacing = new CellLayer<int>(Map);
			Symmetry.RotateAndMirrorOverCPos(
				projectionSpacing,
				Rotations,
				Mirror,
				(projections, cpos) =>
					projectionSpacing[cpos] = Symmetry.ProjectionProximity(projections) / 2);
			return projectionSpacing;
		}

		/// <summary>
		/// Produce a cell layer which identifies assymetries in the map.
		/// Cells that are considered recessive but that have dominant projections are marked as
		/// true in the resulting CellLayer.
		/// </summary>
		/// <param name="dominantTerrain">Cells matching these terrain types are consided dominant.</param>
		/// <param name="dominantActors">If true, cells covered by actors are considered dominant.</param>
		/// <param name="strictTerrainTypes">
		/// Also mark as true any cells where the terrain types don't match with projections, even
		/// if they are also all recessive.
		/// </param>
		public CellLayer<bool> FindAsymmetries(
			IReadOnlySet<byte> dominantTerrain,
			bool dominantActors,
			bool strictTerrainTypes)
		{
			var terrainTypes = CellLayerUtils.Create(Map, (MPos mpos) =>
				terrainInfo.GetTerrainIndex(Map.Tiles[mpos]));
			var dominant = CellLayerUtils.Map(terrainTypes, dominantTerrain.Contains);
			if (dominantActors)
				ZoneFromActors(dominant, true);

			var incompatibilities = new CellLayer<bool>(Map);
			Symmetry.RotateAndMirrorOverCPos(
				incompatibilities,
				Rotations,
				Mirror,
				(CPos[] sources, CPos destination) =>
				{
					if (!dominant[destination])
						incompatibilities[destination] = sources
							.Where(incompatibilities.Contains)
							.Any(source => dominant[source] || (strictTerrainTypes && terrainTypes[destination] != terrainTypes[source]));
				});
			return incompatibilities;
		}

		/// <summary>
		/// Given a space CellLayer, identifies the separate true regions. Cells are part of the
		/// same region if they are connected by an offset in spread.
		/// </summary>
		public (Region[] Regions, CellLayer<int> RegionMap) FindRegions(
			CellLayer<bool> space,
			ImmutableArray<CVec> spread)
		{
			CheckHasMapShape(space);

			var regions = new List<Region>();
			var regionMap = new CellLayer<int>(Map);
			regionMap.Clear(Region.NullId);

			void Fill(Region region, CPos start)
			{
				bool? Filler(CPos cpos, bool _)
				{
					var mpos = cpos.ToMPos(Map);
					if (regionMap[mpos] == Region.NullId && space[mpos])
					{
						regionMap[mpos] = region.Id;
						region.Area++;
						return true;
					}

					return null;
				}

				CellLayerUtils.FloodFill(
					space,
					[(start, true)],
					Filler,
					spread);
			}

			foreach (var mpos in Map.AllCells.MapCoords)
				if (regionMap[mpos] == Region.NullId && space[mpos])
				{
					var region = new Region()
					{
						Id = regions.Count,
						Area = 0,
					};

					regions.Add(region);
					var cpos = mpos.ToCPos(Map);
					Fill(region, cpos);
				}

			return (regions.ToArray(), regionMap);
		}

		/// <summary>
		/// Finds the largest, symmetrical, unpoisoned playable region on the map.
		/// Returns a CellLayer describing the playable region, or null if there is no suitable
		/// playable region.
		/// </summary>
		/// <param name="playable">Whether given cells are playable.</param>
		/// <param name="poison">Any regions with a poisoned cell are disqualified. Can be null.</param>
		public CellLayer<bool> ChoosePlayableRegion(
			CellLayer<bool> playable,
			CellLayer<bool> poison = null)
		{
			CheckHasMapShapeOrNull(poison);

			var (regions, regionMask) = FindRegions(playable, DirectionExts.Spread8CVec);
			var disqualifications = new HashSet<int>();

			if (poison != null)
				foreach (var mpos in Map.AllCells.MapCoords)
					if (poison[mpos]
							&& regionMask[mpos] != Region.NullId
							&& playable[mpos])
						disqualifications.Add(regionMask[mpos]);

			// Disqualify regions that violate any symmetry requirements.
			{
				var symmetryScore = new int[regions.Length];
				void TestSymmetry(CPos[] sources, CPos destination)
				{
					var id = regionMask[destination];
					if (!playable[destination])
						return;
					if (sources.All(source => regionMask.TryGetValue(source, out var sourceId) && sourceId == id))
						symmetryScore[id]++;
				}

				Symmetry.RotateAndMirrorOverCPos(
					regionMask,
					Rotations,
					Mirror,
					TestSymmetry);

				for (var id = 0; id < symmetryScore.Length; id++)
					if (symmetryScore[id] < regions[id].Area / 2)
						disqualifications.Add(id);
			}

			Region largest = null;
			foreach (var region in regions)
			{
				if (disqualifications.Contains(region.Id))
					continue;
				if (largest == null || region.Area > largest.Area)
					largest = region;
			}

			if (largest == null)
				return null;

			return CellLayerUtils.Create(Map, (MPos mpos) => regionMask[mpos] == largest.Id);
		}

		/// <summary>
		/// Generate a CellLayer containing scores for the preferability of spawn locations, based
		/// on separation from symmetry projections and the map center. Higher scores are better.
		/// </summary>
		/// <param name="centralReservationFraction">
		/// Distance from the map center or symmetry lines inside of which spawns are biased away
		/// from. Measured as a fraction (out of 1024) of the map's smallest dimension.
		/// </param>
		public CellLayer<int> SpawnBias(int centralReservationFraction)
		{
			var minSpan = Math.Min(Map.MapSize.Width, Map.MapSize.Height);
			var projectionSpacing = lazyProjectionSpacing.Value;
			var spawnBias = new CellLayer<int>(Map);
			var spawnBiasRadius = Math.Max(1, minSpan * centralReservationFraction / FractionMax);
			spawnBias.Clear(spawnBiasRadius);
			CellLayerUtils.OverCircle(
				cellLayer: spawnBias,
				wCenter: CellLayerUtils.Center(Map),
				wRadius: new WDist(1024 * spawnBiasRadius),
				outside: false,
				action: (mpos, _, _, wrSq) => spawnBias[mpos] = (int)Exts.ISqrt(wrSq) / 1024);
			foreach (var mpos in Map.AllCells.MapCoords)
				spawnBias[mpos] = Math.Min(spawnBias[mpos], projectionSpacing[mpos]);
			return spawnBias;
		}

		/// <summary>
		/// Finds a random suitable mpspawn location, biased away from symmetries and the map
		/// center. Returns null if nowhere is suitable.
		/// </summary>
		/// <param name="random">Random source for spawn placement.</param>
		/// <param name="zoneable">Mask of valid space for spawn (and other object) placement.</param>
		/// <param name="centralReservationFraction">
		/// Distance from the map center or symmetry lines inside of which spawns are biased away
		/// from. Measured as a fraction (out of 1024) of the map's smallest dimension.
		/// </param>
		/// <param name="minimumRadius">Minimum space required for a spawn.</param>
		/// <param name="maximumRadius">Maximum space used by a spawn, beyond which larger spaces are equally preferable.</param>
		/// <param name="zoneRadius">
		/// Space that spawns are expected to reserve in zoneable. Note that this function does not
		/// modify zoneable, but this is needed in order to avoid placing symmetry-projected spawns
		/// with overlapping zone allocations.
		/// </param>
		public CPos? ChooseSpawnInZoneable(
			MersenneTwister random,
			CellLayer<bool> zoneable,
			int centralReservationFraction,
			int minimumRadius,
			int maximumRadius,
			int zoneRadius)
		{
			CheckHasMapShape(zoneable);
			var projectionSpacing = lazyProjectionSpacing.Value;
			var spawnBias = SpawnBias(centralReservationFraction);
			var spawnPreference = new CellLayer<int>(Map);
			CellLayerUtils.ChebyshevRoom(spawnPreference, zoneable, false);
			foreach (var mpos in Map.AllCells.MapCoords)
				if (spawnPreference[mpos] >= minimumRadius &&
					projectionSpacing[mpos] * 2 >= zoneRadius + minimumRadius)
				{
					spawnPreference[mpos] = spawnBias[mpos] * Math.Min(maximumRadius, spawnPreference[mpos]);
				}
				else
				{
					spawnPreference[mpos] = 0;
				}

			var (chosenMPos, chosenValue) = CellLayerUtils.FindRandomBest(
				spawnPreference,
				random,
				(a, b) => a.CompareTo(b));

			if (chosenValue < 1)
				return null;

			return chosenMPos.ToCPos(Map.Grid.Type);
		}

		/// <summary>
		/// Find a random cell in zoneable with the most free space. Spaces which are maximumSpace
		/// or more away from unzoned cells are treated equally.
		/// Returns the CPos and space (up to maximumSpace) of the chosen cell.
		/// The space value will be negative if there are no zoned cells.
		/// </summary>
		public (CPos CPos, int Space) ChooseInZoneable(
			MersenneTwister random,
			CellLayer<bool> zoneable,
			int maximumSpace)
		{
			CheckHasMapShape(zoneable);
			var projectionSpacing = lazyProjectionSpacing.Value;
			var roominess = new CellLayer<int>(Map);
			CellLayerUtils.ChebyshevRoom(roominess, zoneable, false);
			foreach (var mpos in Map.AllCells.MapCoords)
				roominess[mpos] = Math.Min(
					maximumSpace,
					Math.Min(roominess[mpos], projectionSpacing[mpos]));
			var (chosenMPos, chosenValue) = CellLayerUtils.FindRandomBest(
				roominess,
				random,
				(a, b) => a.CompareTo(b));
			return (chosenMPos.ToCPos(Map.Grid.Type), chosenValue);
		}

		/// <summary>
		/// Generate a CellLayer scoring cells on how close to a target walking distance through
		/// walkable cells they are from the closest seed point. Higher scores are better. The
		/// score considers the distance needed to walk around unwalkable cells. Unsuitable cells
		/// will have a score of -int.MaxValue.
		/// </summary>
		/// <param name="walkable">Walkable cells.</param>
		/// <param name="mask">Unmasked cells will have a score of -int.MaxValue. Can be null.</param>
		/// <param name="seeds">Points from which to measure walking distance.</param>
		/// <param name="targetRange">The highest scoring walking distance..</param>
		/// <param name="maximumRange">Distances greater than this are given a score of -int.MaxValue.</param>
		public CellLayer<int> TargetWalkingDistance(
			CellLayer<bool> walkable,
			CellLayer<bool> mask,
			IEnumerable<CPos> seeds,
			WDist targetRange,
			WDist maximumRange)
		{
			CheckHasMapShape(walkable);
			CheckHasMapShapeOrNull(mask);

			var walkingDistances = new CellLayer<WDist>(Map);
			CellLayerUtils.WalkingDistances(
				walkingDistances,
				walkable,
				seeds,
				maximumRange);
			var scores = new CellLayer<int>(Map);
			foreach (var mpos in Map.AllCells.MapCoords)
			{
				var v = (mask?[mpos] ?? true) ? walkingDistances[mpos].Length : int.MaxValue;
				if (v == int.MaxValue)
					scores[mpos] = -int.MaxValue;
				else if (v <= targetRange.Length)
					scores[mpos] = (v + 1023) / 1024;
				else
					scores[mpos] = (2 * targetRange.Length - v + 1023) / 1024;
			}

			return scores;
		}

		/// <summary>
		/// Add an actor and its symmetry projections to the map and subtract its footprint from
		/// zoneable. Optionally, a circle with a given dezone radius from the actor center can
		/// also be subtracted from zoneable.
		/// </summary>
		public void ProjectPlaceDezoneActor(
			ActorPlan actorPlan,
			CellLayer<bool> zoneable = null,
			WDist? dezoneRadius = null)
		{
			CheckHasMapShapeOrNull(zoneable);
			var projections = Symmetry.RotateAndMirrorActorPlan(
				actorPlan, Rotations, Mirror);
			ActorPlans.AddRange(projections);
			if (zoneable != null)
				foreach (var projection in projections)
					DezoneActor(projection, zoneable, dezoneRadius);
		}

		/// <summary>
		/// Chooses a location for an actor within zoneable, and then projects, places, and dezones
		/// for it. (The zoneable CellLayer is modified.)
		/// </summary>
		/// <returns>True if an actor was placed, false if there was insufficient space.</returns>
		public bool AddActor(
			MersenneTwister random,
			CellLayer<bool> zoneable,
			string actorType,
			WDist? actorDezoneRadius = null)
		{
			var actorPlan = new ActorPlan(Map, actorType);

			var requiredSpace = actorPlan.MaxSpan() * 1024 / 1448 + 2;
			var (chosenCPos, chosenValue) = ChooseInZoneable(
				random, zoneable, requiredSpace);
			if (chosenValue < requiredSpace)
				return false;

			actorPlan.WPosCenterLocation = CellLayerUtils.CPosToWPos(chosenCPos, Map.Grid.Type);

			ProjectPlaceDezoneActor(actorPlan, zoneable, actorDezoneRadius);

			return true;
		}

		/// <summary>
		/// Given a CellLayer of weights/priorities, chooses locations for actors within zoneable,
		/// and then projects, places, and dezones for them.
		/// </summary>
		/// <param name="random">Random source for locations and actor type selection.</param>
		/// <param name="zoneable">Available space for actors. Modified if actors placed.</param>
		/// <param name="distribution">Weights or priorities for placing an actor centered on cells.</param>
		/// <param name="weightedActorTypes">Actor types to choose from and their relative weights.</param>
		/// <param name="targetCount">Number of actors to attempt to place.</param>
		/// <param name="weighted">If true, choose actor locations using probabilistic weights instead of best candidate.</param>
		/// <param name="actorDezoneRadius">
		/// Dezone radius for placed actors (in addition to footprint).
		/// This does not affect spacing within the region.
		/// </param>
		/// <returns>Number of actors added. 0 indicates none could be added.</returns>
		public int AddDistributedActors(
			MersenneTwister random,
			CellLayer<bool> zoneable,
			CellLayer<int> distribution,
			IReadOnlyDictionary<string, int> weightedActorTypes,
			int targetCount,
			bool weighted,
			WDist? actorDezoneRadius = null)
		{
			CheckHasMapShape(zoneable);
			CheckHasMapShape(distribution);

			var (actorTypes, actorTypeWeights) = SplitDictionary(weightedActorTypes);
			var clusterZoneable = CellLayerUtils.Clone(zoneable);
			for (var count = 0; count < targetCount; count++)
			{
				var actorType = actorTypes[random.PickWeighted(actorTypeWeights)];
				var actorPlan = new ActorPlan(Map, actorType);
				var requiredSpace = actorPlan.MaxSpan() * 1024 / 1448 + 2;

				var roominess = new CellLayer<int>(Map);
				CellLayerUtils.ChebyshevRoom(roominess, clusterZoneable, false);
				var filteredDistribution = CellLayerUtils.Create(Map, (MPos mpos) =>
					roominess[mpos] >= requiredSpace ? distribution[mpos] : 0);

				MPos mpos;
				if (weighted)
					mpos = CellLayerUtils.PickWeighted(filteredDistribution, random);
				else
					(mpos, _) = CellLayerUtils.FindRandomBest(filteredDistribution, random, (a, b) => a.CompareTo(b));

				if (filteredDistribution[mpos] == 0)
					return count;

				actorPlan.Location = mpos.ToCPos(Map.Grid.Type);
				CellLayerUtils.OverCircle(
					cellLayer: distribution,
					wCenter: actorPlan.WPosLocation,
					wRadius: new WDist(actorPlan.MaxSpan() * 1024),
					outside: false,
					action: (mpos, _, _, _) => distribution[mpos] = 0);

				ProjectPlaceDezoneActor(actorPlan, zoneable, actorDezoneRadius);
				DezoneActor(actorPlan, clusterZoneable);
			}

			return targetCount;
		}

		/// <summary>
		/// Chooses a location for a cluster of actors within zoneable, and then projects, places,
		/// and dezones for them.
		/// </summary>
		/// <param name="random">Random source for locations and actor type selection.</param>
		/// <param name="zoneable">Available space for actors. Modified if actors placed.</param>
		/// <param name="weightedActorTypes">Actor types to choose from and their relative weights.</param>
		/// <param name="targetCount">Number of actors to attempt to place.</param>
		/// <param name="innerReservation">Avoid placing actors' centers within this radius unless it's a last resort.</param>
		/// <param name="minimumRadius">Minimum cluster radius for actor center placement.</param>
		/// <param name="maximumRadius">Maximum cluster radius for actor center placement.</param>
		/// <param name="outerBorder">Zoneable spacing required beyond radius (that actors' centers will not be placed in).</param>
		/// <param name="weighted">If true, choose actor locations using probabilistic weights instead of best candidate.</param>
		/// <param name="actorDezoneRadius">
		/// Dezone radius for placed actors (in addition to footprint).
		/// This does not affect spacing within the cluster.
		/// </param>
		/// <param name="distributor">
		/// Calculates location weights or candidate priorities based on distance from the cluster
		/// center. The input is the WDist.LengthSquared from the cluster center. Location choices
		/// are biased towards greater outputs. If null, defaults to a function where the weight is
		/// proportional to the squared distance, thus biasing actors towards the outside.
		/// </param>
		/// <returns>Number of actors added. 0 indicates none could be added.</returns>
		public int AddActorCluster(
			MersenneTwister random,
			CellLayer<bool> zoneable,
			IReadOnlyDictionary<string, int> weightedActorTypes,
			int targetCount,
			int innerReservation,
			int minimumRadius,
			int maximumRadius,
			int outerBorder,
			bool weighted,
			WDist? actorDezoneRadius = null,
			Func<long, int> distributor = null)
		{
			CheckHasMapShape(zoneable);

			var (chosenCPos, room) = ChooseInZoneable(
				random, zoneable, maximumRadius + outerBorder);
			var radius2 = room - outerBorder - 1;
			if (radius2 < minimumRadius)
				return 0;

			if (radius2 > maximumRadius)
				radius2 = maximumRadius;

			var radius1 = Math.Min(innerReservation, radius2);
			if (radius1 < 1)
				return 0;

			var distribution = new CellLayer<int>(Map);
			var wRadius1Sq = radius1 * radius1 * 1024L * 1024L;
			distributor ??= wrSq => (int)(wrSq / (1024 * 1024));
			CellLayerUtils.OverCircle(
				cellLayer: distribution,
				wCenter: CellLayerUtils.CPosToWPos(chosenCPos, Map.Grid.Type),
				wRadius: new WDist(radius2 * 1024),
				outside: false,
				action: (mpos, _, _, wrSq) =>
					distribution[mpos] = wrSq >= wRadius1Sq ? distributor(wrSq) : 0);

			return AddDistributedActors(
				random,
				zoneable,
				distribution,
				weightedActorTypes,
				targetCount,
				weighted,
				actorDezoneRadius);
		}

		/// <summary>
		/// For a 1x1 tile, return a TerrainTile with the given tile type, using a random index if
		/// it's a PickAny template.
		/// </summary>
		public TerrainTile PickTile(MersenneTwister random, ushort tileType)
		{
			if (templatedTerrainInfo.Templates.TryGetValue(tileType, out var template) && template.PickAny)
				return new TerrainTile(tileType, (byte)random.Next(0, template.TilesCount));
			else
				return new TerrainTile(tileType, 0);
		}

		/// <summary>Wrapper around MultiBrush.PaintArea.</summary>
		public void PaintArea(
			MersenneTwister random,
			CellLayer<MultiBrush.Replaceability> replace,
			IReadOnlyList<MultiBrush> brushes,
			bool alwaysPreferLargerBrushes = false)
		{
			CheckHasMapShape(replace);

			MultiBrush.PaintArea(
				Map,
				ActorPlans,
				replace,
				brushes,
				random,
				alwaysPreferLargerBrushes);
		}

		/// <summary>
		/// Wrapper around PaintArea that uses Replacibility.Actor for masked cells.
		/// </summary>
		public void PaintActors(
			MersenneTwister random,
			CellLayer<bool> mask,
			IReadOnlyList<MultiBrush> brushes,
			bool alwaysPreferLargerBrushes = false)
		{
			CheckHasMapShape(mask);

			var replace = new CellLayer<MultiBrush.Replaceability>(Map);
			foreach (var mpos in Map.AllCells.MapCoords)
				replace[mpos] = mask[mpos] ? MultiBrush.Replaceability.Actor : MultiBrush.Replaceability.None;

			PaintArea(
				random,
				replace,
				brushes,
				alwaysPreferLargerBrushes);
		}

		/// <summary>Wrapper around MultiBrush.Paint for path tiling results.</summary>
		public void PaintTiling(
			MersenneTwister random,
			MultiBrush brush)
		{
			brush.Paint(Map, ActorPlans, CPos.Zero, MultiBrush.Replaceability.Any, random);
		}

		/// <summary>
		/// Repaint the areas occupied by given tile types using MultiBrushes.
		/// </summary>
		public void RepaintTiles(
			MersenneTwister random,
			IReadOnlyDictionary<ushort, IReadOnlyList<MultiBrush>> rules)
		{
			foreach (var (tile, collection) in rules.OrderBy(kv => kv.Key))
			{
				var replace = new CellLayer<MultiBrush.Replaceability>(Map);
				foreach (var mpos in Map.AllCells.MapCoords)
					replace[mpos] =
						Map.Tiles[mpos].Type == tile
							? MultiBrush.Replaceability.Any
							: MultiBrush.Replaceability.None;

				MultiBrush.PaintArea(Map, ActorPlans, replace, collection, random);
			}
		}

		/// <summary>
		/// Creates a boolean fractal noise pattern obeying symmetry requirements.
		/// <param name="random">Random source</param>
		/// <param name="noiseFeatureSize">Largest interval for fractal noise.</param>
		/// <param name="fraction">Target fraction of true values (from 0 to FractionMax).</param>
		/// <param name="clumpiness">
		/// The number of times to square root the noise wavelength to arrive at the amplitude.
		/// In other words, amplitude = wavelength ** (1 / (2 ** clumpiness))
		/// Setting to 0 is equivalent to pink noise.
		/// </param>
		/// </summary>
		public CellLayer<bool> BooleanNoise(
			MersenneTwister random,
			int noiseFeatureSize,
			int fraction,
			int clumpiness = 0)
		{
			var noise = new CellLayer<int>(Map);
			NoiseUtils.SymmetricFractalNoiseIntoCellLayer(
				random,
				noise,
				Rotations,
				Mirror,
				noiseFeatureSize,
				wavelength => NoiseUtils.ClumpinessAmplitude(wavelength, clumpiness));

			return CellLayerUtils.CalibratedBooleanThreshold(
				noise, fraction, FractionMax);
		}

		/// <summary>
		/// Create a matrix containing a generated terrain elevation map.
		/// </summary>
		/// <param name="random">Random source for terrain noise.</param>
		/// <param name="noiseFeatureSize">Largest interval for fractal noise.</param>
		/// <param name="smoothing">Range in cells for smoothing.</param>
		public Matrix<int> ElevationNoiseMatrix(
			MersenneTwister random,
			int noiseFeatureSize,
			int smoothing)
		{
			var elevation = NoiseUtils.SymmetricFractalNoise(
				random,
				CellLayerUtils.CellBounds(Map).Size.ToInt2(),
				Rotations,
				Mirror,
				noiseFeatureSize,
				NoiseUtils.PinkAmplitude);
			MatrixUtils.NormalizeRangeInPlace(elevation, 1024);

			if (smoothing > 0)
				elevation = MatrixUtils.BinomialBlur(elevation, smoothing);

			return elevation;
		}

		/// <summary>
		/// <para>
		/// Produce an unbiased noise pattern for resource growth.
		/// </para><para>
		/// The output noise will have the range [uniformity, uniformity + 1024].
		/// </para>
		/// </summary>
		public CellLayer<int> ResourceNoise(
			MersenneTwister random,
			int noiseFeatureSize,
			int clumpiness,
			int uniformity)
		{
			var pattern = new CellLayer<int>(Map);
			NoiseUtils.SymmetricFractalNoiseIntoCellLayer(
				random,
				pattern,
				Rotations,
				Mirror,
				noiseFeatureSize,
				wavelength => NoiseUtils.ClumpinessAmplitude(wavelength, clumpiness));
			{
				CellLayerUtils.CalibrateQuantileInPlace(
					pattern,
					0,
					0, 1);
				var max = pattern.Max();
				foreach (var mpos in Map.AllCells.MapCoords)
					pattern[mpos] = uniformity + 1024 * pattern[mpos] / max;
			}

			return pattern;
		}

		/// <summary>
		/// Given elevation noise, partition it into a boolean Matrix where false represents low
		/// elevation and true represents high elevation.
		/// </summary>
		/// <param name="elevation">Terrain elevation noise.</param>
		/// <param name="mask">
		/// A mask (usually a previous slice) within which the new slice is constrained to and
		/// derived from. Can be null to imply all space is available.
		/// </param>
		/// <param name="fraction">Target fraction (out of FractionMax) of masked terrain to be carried over to the new slice.</param>
		/// <param name="minimumContourSpacing">Minimum distance between the contours of the mask and the new slice.</param>
		public Matrix<bool> SliceElevation(
			Matrix<int> elevation,
			Matrix<bool> mask,
			int fraction,
			int minimumContourSpacing = 0)
		{
			CheckHasMapShape(elevation);
			CheckHasMapShapeOrNull(mask);

			if (mask == null)
				return MatrixUtils.CalibratedBooleanThreshold(elevation, fraction, FractionMax);

			var filteredElevation = elevation.Clone();
			var roominess = MatrixUtils.ChebyshevRoom(mask, true);
			var available = 0;
			var total = filteredElevation.Data.Length;
			for (var n = 0; n < total; n++)
			{
				if (mask[n])
					available++;
				else
					filteredElevation.Data[n] = int.MinValue;
			}

			var slice = MatrixUtils.CalibratedBooleanThreshold(
				filteredElevation, available * fraction / FractionMax, total);

			// Calibration isn't perfect. Make sure constraints are still met.
			var minimumRoom = minimumContourSpacing + 1;
			for (var n = 0; n < total; n++)
				slice.Data[n] &= roominess.Data[n] >= minimumRoom;

			return slice;
		}

		/// <summary>
		/// If given a looped path, normalizes it such that symmetry projected paths should have
		/// symmetry projected start/end points. Note that this method isn't meaningful for loops
		/// which would overlap with their symmetry projections. For non-looped paths, returns the
		/// input unchanged.
		/// </summary>
		public int2[] NormalizeLoopStart(int2[] path)
		{
			if (path.Length < 2)
				throw new ArgumentException("path is too short");

			if (path[0] != path[^1])
				return path;

			var gridType = Map.Grid.Type;
			var center = CellLayerUtils.Center(Map);

			var cpath = CellLayerUtils.FromMatrixPoints([path[0..^1]], Map.Tiles)[0];
			var wpath = cpath
				.Select(cpos => CellLayerUtils.CornerToWPos(cpos, gridType))
				.ToList();

			// Choose the closest to the map center and makes it the start/end of the loop.
			// If there are ties, pick the first closest point that follows from the furthest
			// point(s), ensuring consistency for symmetries.
			var distances = wpath.ConvertAll(w => (w - center).LengthSquared);
			var closest = distances.Min();
			var furthest = distances.Max();
			var closestI = distances.IndexOf(furthest);
			while (distances[closestI] != closest)
				if (++closestI == distances.Count)
					closestI = 0;

			return path[closestI..^1].Concat(path[0..(closestI + 1)]).ToArray();
		}

		/// <summary>
		/// Given a matrix-style path, divide it into a chain of smaller paths and convert them to
		/// TilingPaths with segment types that best match a matrix of zones.
		/// </summary>
		/// <param name="path">The path to be divided.</param>
		/// <param name="allZones">The full set of zones, in order of preference for ties.</param>
		/// <param name="zoneMask">
		/// Matrix which assigns zones to matching points in the path.
		/// Null values can be used to describe locations with no zoning preference.
		/// </param>
		/// <param name="brushes">Segmented brushes for TilingPath creation.</param>
		/// <param name="minimumStraight">
		/// If greater than zero, sub-paths are only allowed to change over in straight sections
		/// and the starts/ends must be this number of points deep within a straight section.
		/// </param>
		public List<TilingPath> PartitionPath(
			int2[] path,
			IReadOnlyList<PathPartitionZone> allZones,
			Matrix<PathPartitionZone> zoneMask,
			IReadOnlyList<MultiBrush> brushes,
			int minimumStraight)
		{
			// Algorithmic Overview:
			//
			// First, find the straight-enough sections that can support changes between sub-paths.
			// We then find a best fit for subpaths that change over in these straights, according
			// to their minimum lengths and zone matching.
			//
			// A best fit is found using a Dijkstra's Algorithm-based best-first search. (Bottom-up
			// dynamic programming). The sub problems are just spans of the whole path, and are
			// built up towards the full path by adding on and scoring sub-paths.
			//
			// The minimum cost of a sub-path is the minimum possible number of mismatched zones if
			// an optimal zone is chosen.
			//
			// If there are multiple best solutions (with equal costs), there is a preference to
			// solutions with more sub-paths.
			if (allZones.Count == 0)
				throw new ArgumentException("no zones provided");

			if (allZones.Count(zone => zone.RequiredSomewhere) > 1)
				throw new ArgumentException("RequiredSomewhere only supported for at most one zone");

			if (path.Length < 2)
				throw new ArgumentException("path is too short");

			if (minimumStraight < 0)
				throw new ArgumentException("minimumStraight was not >= 0");

			var isLoop = path[0] == path[^1];

			if (isLoop)
				path = NormalizeLoopStart(path);

			var zones = new PathPartitionZone[isLoop ? path.Length - 1 : path.Length];
			for (var i = 0; i < zones.Length; i++)
			{
				if (zoneMask.ContainsXY(path[i]))
					zones[i] = zoneMask[path[i]];
			}

			// from must be >= 0.
			IEnumerable<int> Range(int from, int length)
			{
				for (var i = 0; i < length; i++)
					yield return (from + i) % zones.Length;
			}

			// from must be >= 0.
			IEnumerable<int> ReverseRange(int from, int length)
			{
				for (var i = length - 1; i >= 0; i--)
					yield return (from + i) % zones.Length;
			}

			// Can also be used to get lengths
			int Idx(int i) => (i + zones.Length) % zones.Length;

			var minimumZoneLength = allZones.Min(z => z.MinimumLength);

			// To optimize partition vote counting, we pre-sum all the matches for allZones[i]
			// within zone[0..j] into partitionAcc[i][j]. This means we can quickly count the
			// matches between a and b by subtracting partitionAcc[i][a] from partitionAcc[i][b].
			var partitionAcc = new int[allZones.Count][];
			for (var i = 0; i < allZones.Count; i++)
			{
				partitionAcc[i] = new int[zones.Length + 1];
				var sum = 0;
				for (var j = 0; j < zones.Length; j++)
				{
					if (zones[j] == allZones[i])
						sum++;
					partitionAcc[i][j + 1] = sum;
				}
			}

			// This is declared outside of Vote() to avoid unnecessary re-allocations.
			// The values are not reused across calls.
			var voteCounts = new int[allZones.Count];

			// Identifies valid zone choices in the given range and returns the cost (amount of
			// disagreement) for the best choice(s). Optionally provides the winner(s) via the
			// majorities argument.
			//
			// Note that the winner can sometimes be a zone not present within the range if
			// checkMinLength is enforcing candidates' MinimumLength requirement.
			int Vote(int from, int length, bool checkMinLength, List<PathPartitionZone> majorities = null)
			{
				const int Unsuitable = -1;

				if (checkMinLength && length < minimumZoneLength)
					return int.MaxValue;

				from = Idx(from);
				var to = from + length;
				if (to > zones.Length)
					to -= zones.Length;

				var nonWildcards = 0;
				var best = Unsuitable;

				for (var i = 0; i < allZones.Count; i++)
				{
					int count;
					if (to <= from)
					{
						count =
							partitionAcc[i][zones.Length] - partitionAcc[i][from] +
							partitionAcc[i][to] - partitionAcc[i][0];
					}
					else
					{
						count = partitionAcc[i][to] - partitionAcc[i][from];
					}

					nonWildcards += count;
					if (checkMinLength && length < allZones[i].MinimumLength)
					{
						voteCounts[i] = Unsuitable;
					}
					else
					{
						voteCounts[i] = count;
						if (count > best)
							best = count;
					}
				}

				if (best == Unsuitable)
					return int.MaxValue;

				if (majorities != null)
					for (var i = 0; i < allZones.Count; i++)
						if (voteCounts[i] == best)
							majorities.Add(allZones[i]);

				return nonWildcards - best;
			}

			PathPartitionZone fallbackPath;

			List<TilingPath> SinglePath(PathPartitionZone zone)
			{
				if (zone.ShouldTile)
					return [
						new TilingPath(
							Map,
							CellLayerUtils.FromMatrixPoints([path], Map.Tiles)[0],
							zone.MaximumDeviation,
							zone.SegmentType,
							zone.SegmentType,
							TilingPath.PermittedSegments.FromType(brushes, [zone.SegmentType]))];
				else
					return [];
			}

			if (allZones.Any(zone => zone.RequiredSomewhere))
			{
				fallbackPath = allZones.First(zone => zone.RequiredSomewhere);
			}
			else
			{
				var majorities = new List<PathPartitionZone>();
				if (Vote(0, zones.Length, false, majorities) == 0)
					return SinglePath(majorities[0]);

				fallbackPath = majorities[0];
			}

			var minLength = Math.Max(1, allZones.Min(r => r.MinimumLength));

			if (path.Length < minLength)
				return SinglePath(fallbackPath);

			var straight = new bool[zones.Length];
			for (var i = 0; i < zones.Length; i++)
			{
				var a = path[Idx(i - 1)];
				var b = path[Idx(i)];
				var c = path[Idx(i + 1)];
				straight[i] = DirectionExts.FromInt2(b - a) == DirectionExts.FromInt2(c - b);
			}

			if (!isLoop)
				straight[0] = straight[^1] = true;

			// Note that loops can't be all straight.
			var validTerminal = new bool[zones.Length];
			Array.Fill(validTerminal, true);
			{
				// Forward run
				var run = isLoop
					? ReverseRange(zones.Length - minimumStraight, minimumStraight).TakeWhile(i => straight[i]).Count()
					: 0;
				foreach (var i in Range(0, zones.Length))
				{
					run = straight[i] ? (run + 1) : 0;
					validTerminal[i] &= run >= minimumStraight;
				}

				// Backward run
				run = isLoop
					? Range(zones.Length, minimumStraight).TakeWhile(i => straight[i]).Count()
					: 0;
				foreach (var i in ReverseRange(0, zones.Length))
				{
					run = straight[i] ? (run + 1) : 0;
					validTerminal[i] &= run >= minimumStraight;
				}
			}

			if (!isLoop)
				validTerminal[0] = validTerminal[^1] = true;

			List<int> validStarts;
			if (isLoop)
				validStarts = validTerminal
					.Select((v, i) => (Valid: v, Index: i))
					.Where(t => t.Valid)
					.Select(t => t.Index)
					.ToList();
			else
				validStarts = [0];

			if (validStarts.Count == 0)
				return SinglePath(fallbackPath);

			var solutions = new List<(int Cost, List<int> Solution)>();

			// An optimization would be to include the start point in a combined search.
			// This is simpler though.
			foreach (var offset in validStarts)
			{
				var end = path.Length - 1;
				var costs = new int[end + 1];
				Array.Fill(costs, int.MaxValue);

				// Find costs
				{
					var costPriorities = new PriorityArray<int>(end + 1, int.MaxValue);
					costPriorities[0] = costs[0] = 0;
					while (true)
					{
						var from = costPriorities.GetMinIndex();
						var fromCost = costPriorities[from];
						if (fromCost == int.MaxValue || from == end)
							break;

						costPriorities[from] = int.MaxValue;
						var maxLength = end - from;
						for (var length = minLength; length <= maxLength; length++)
						{
							var to = from + length;
							if (!validTerminal[Idx(offset + to)])
								continue;

							var mismatch = Vote(offset + from, length, true);
							if (mismatch == int.MaxValue)
								continue;

							var toCost = fromCost + mismatch;
							if (toCost >= costs[to])
								continue;

							costPriorities[to] = costs[to] = toCost;
						}
					}
				}

				if (costs[end] == int.MaxValue)
					continue;

				// Work back from costs to solution
				{
					List<int> solution = [Idx(offset + end)];
					var to = end;
					while (to != 0)
					{
						var toCost = costs[to];
						var maxLength = to;
						for (var length = minLength; length <= maxLength; length++)
						{
							var from = to - length;
							if (!validTerminal[Idx(offset + from)])
								continue;

							var mismatch = Vote(offset + from, length, true);
							if (mismatch == int.MaxValue)
								continue;

							var fromCost = toCost - mismatch;

							// Use the first found solution.
							if (fromCost == costs[from])
							{
								solution.Add(Idx(offset + from));
								to = from;
								break;
							}
						}
					}

					solution.Reverse();
					solutions.Add((costs[end], solution));
				}
			}

			if (solutions.Count == 0)
				return SinglePath(fallbackPath);

			var bestCost = solutions.Min(t => t.Cost);
			var bestCostSolutions = solutions.Where(t => t.Cost == bestCost).ToList();
			var mostBoundaries = bestCostSolutions.Max(t => t.Solution.Count);
			var boundaries = bestCostSolutions.First(t => t.Solution.Count == mostBoundaries).Solution;
			var ranges = new List<(int Start, int Length, PathPartitionZone Zone)>();
			PathPartitionZone lastZone = null;
			for (var i = 0; i < boundaries.Count - 1; i++)
			{
				var from = boundaries[i];
				var to = boundaries[i + 1];
				if (from == to)
					return SinglePath(fallbackPath);

				var length = Idx(to - from);
				if (length + 1 == path.Length)
					return SinglePath(fallbackPath);

				var possibleZones = new List<PathPartitionZone>(allZones.Count);
				Vote(from, length, true, possibleZones);
				if (possibleZones[0] != lastZone)
					ranges.Add((from, length, possibleZones[0]));
				else
					ranges[^1] = (ranges[^1].Start, ranges[^1].Length + length, lastZone);

				lastZone = possibleZones[0];
			}

			if (isLoop && ranges.Count >= 2 && ranges[0].Zone == ranges[^1].Zone)
			{
				ranges[0] = (ranges[^1].Start, ranges[^1].Length + ranges[0].Length, ranges[^1].Zone);
				ranges.RemoveAt(ranges.Count - 1);
			}

			if (ranges.Count == 1)
				return SinglePath(fallbackPath);

			var partitions = new List<TilingPath>();
			var previousIncludedInterface = isLoop && ranges[^1].Zone.ShouldTile;
			for (var rangeI = 0; rangeI < ranges.Count; rangeI++)
			{
				var (start, length, zone) = ranges[rangeI];
				if (!zone.ShouldTile)
				{
					previousIncludedInterface = false;
					continue;
				}

				var innerType = zone.SegmentType;
				var startType = (!previousIncludedInterface && (isLoop || rangeI > 0))
					? ranges[(ranges.Count + rangeI - 1) % ranges.Count].Zone.SegmentType
					: innerType;
				var endType = (isLoop || rangeI < ranges.Count - 1)
					? ranges[(rangeI + 1) % ranges.Count].Zone.SegmentType
					: innerType;
				Direction? startDirection = (isLoop || rangeI > 0)
					? DirectionExts.FromInt2(
						path[(start + 1) % zones.Length]
							- path[start])
					: null;
				Direction? endDirection = (isLoop || rangeI < ranges.Count - 1)
					? DirectionExts.FromInt2(
						path[(length + start + 1) % zones.Length]
							- path[(length + start) % zones.Length])
					: null;

				var points = Range(start, length + 1)
					.Select(i => path[i])
					.ToList();

				var tilingPath = new TilingPath(
					Map,
					CellLayerUtils.FromMatrixPoints([points.ToArray()], Map.Tiles)[0],
					zone.MaximumDeviation,
					startType,
					endType,
					TilingPath.PermittedSegments.FromTypes(brushes, [startType], [innerType], [endType]));
				tilingPath.Start.Direction = startDirection;
				tilingPath.End.Direction = endDirection;

				partitions.Add(tilingPath);
				previousIncludedInterface = true;
			}

			return partitions;
		}

		/// <summary>Wrapper around PartitionPath to process multiple paths at once.</summary>
		public List<TilingPath> PartitionPaths(
			IEnumerable<int2[]> paths,
			IReadOnlyList<PathPartitionZone> zones,
			Matrix<PathPartitionZone> partitionMask,
			IReadOnlyList<MultiBrush> brushes,
			int minStraight)
		{
			return paths
				.SelectMany(path => PartitionPath(
					path, zones, partitionMask, brushes, minStraight))
				.ToList();
		}

		/// <summary>
		/// Wrapper around InsideOutside which performs both path tiling and side filling, painting
		/// the result to the map. If tiling fails, returns null without modifying the map.
		/// </summary>
		/// <param name="random">Random source used for tiling and filling.</param>
		/// <param name="tilingPaths">
		/// Paths to tile. Note that these are tiled exactly as specified, so if end deviation is
		/// enabled, this will allow tiling errors.
		/// </param>
		/// <param name="fallback">Side to assume if no paths are contained in the map.</param>
		/// <param name="outside">If non-null, these MultiBrushes are painted over outside regions.</param>
		/// <param name="inside">If non-null, these MultiBrushes are painted over inside regions.</param>
		/// <param name="replaceMask">Optional replaceability constraints for filling. Ignored for path tiling.</param>
		public CellLayer<Side> PaintLoopsAndFill(
			MersenneTwister random,
			IReadOnlyList<TilingPath> tilingPaths,
			Side fallback,
			IReadOnlyList<MultiBrush> outside,
			IReadOnlyList<MultiBrush> inside,
			CellLayer<MultiBrush.Replaceability> replaceMask = null)
		{
			CheckHasMapShapeOrNull(replaceMask);

			var tilings = new MultiBrush[tilingPaths.Count];
			for (var i = 0; i < tilingPaths.Count; i++)
			{
				var tiling = tilingPaths[i].Tile(random);
				if (tiling == null)
					return null;

				tilings[i] = tiling;
			}

			foreach (var tiling in tilings)
				tiling.Paint(Map, ActorPlans, CPos.Zero, MultiBrush.Replaceability.Any, random);

			if (inside == null && outside == null)
				return null;

			var sides = InsideOutside(tilings, fallback);

			foreach (var (brushes, side) in new[] { (inside, Side.In), (outside, Side.Out) })
			{
				if (brushes == null)
					continue;

				var replace = new CellLayer<MultiBrush.Replaceability>(Map);
				foreach (var mpos in Map.AllCells.MapCoords)
					replace[mpos] = (sides[mpos] == side)
						? (replaceMask?[mpos] ?? MultiBrush.Replaceability.Any)
						: MultiBrush.Replaceability.None;

				PaintArea(random, replace, brushes);
			}

			return sides;
		}

		/// <summary>
		/// Given a collection of path tiling results which form non-nested loops or extend beyond
		/// or out to the map edge, return a CellLayer identifying whether cells are inside or
		/// outside of the tiled loops, or Side.None if the cell is covered by a MultiBrush.
		/// If a loop wraps around a space clockwise, that space is considered inside.
		/// </summary>
		/// <param name="tilings">Path tiling results which partition the space.</param>
		/// <param name="fallback">Side to assume if no paths are contained in the map.</param>
		public CellLayer<Side> InsideOutside(
			IReadOnlyList<MultiBrush> tilings,
			Side fallback)
		{
			var sides = new CellLayer<Side>(Map);
			var tiledPoints = new CPos[tilings.Count][];
			var tiledArea = new CellLayer<bool>(Map);
			for (var i = 0; i < tilings.Count; i++)
			{
				tiledPoints[i] = tilings[i].Segment.Points
					.Select(vec => CPos.Zero + vec)
					.ToArray();
				foreach (var cvec in tilings[i].Shape)
					if (tiledArea.Contains(CPos.Zero + cvec))
						tiledArea[CPos.Zero + cvec] = true;
			}

			var chiralityMatrix = MatrixUtils.PointsChirality(
				CellLayerUtils.CellBounds(Map).Size.ToInt2(),
				CellLayerUtils.ToMatrixPoints(tiledPoints, Map.Tiles));
			if (chiralityMatrix == null)
			{
				sides.Clear(fallback);
				return sides;
			}

			var chirality = new CellLayer<int>(Map);
			CellLayerUtils.FromMatrix(chirality, chiralityMatrix);
			foreach (var mpos in Map.AllCells.MapCoords)
			{
				if (!tiledArea[mpos])
				{
					if (chirality[mpos] > 0)
						sides[mpos] = Side.In;
					else if (chirality[mpos] < 0)
						sides[mpos] = Side.Out;
				}
			}

			return sides;
		}

		/// <summary>
		/// Fill a CellLayer with a given value to identify or undo the effects of painting sided
		/// regions. For example, this can be used to un-paint an unplayable body of water along
		/// with its beaches.
		/// </summary>
		public void FillUnmaskedSideAndBorder(
			CellLayer<bool> mask,
			CellLayer<Side> sides,
			Side fillSide,
			Action<CPos> fillAction)
		{
			CheckHasMapShape(mask);
			CheckHasMapShape(sides);

			if (fillSide == Side.None)
				throw new ArgumentException("fillSide was not In or Out");

			var notFillSide = fillSide == Side.In ? Side.Out : Side.In;
			var fillSeeds = CellLayerUtils.Create(Map, (MPos mpos) =>
				sides[mpos] == fillSide &&
				!mask[mpos] &&
				Map.Contains(mpos));
			fillSeeds = ImproveSymmetry(fillSeeds, false, (a, b) => a || b);
			var fillable = CellLayerUtils.Map(sides, side => side != notFillSide);
			CellLayerUtils.SimpleFloodFill(
				fillable,
				fillSeeds,
				fillAction,
				DirectionExts.Spread4CVec);
		}

		/// <summary>
		/// Plan passageway cutouts that, when subtracted away from obstructions, preserve
		/// connectivity through a given space.
		/// </summary>
		/// <param name="random">Random source for carving addition passageways to comply with maximumCutoutSpacing.</param>
		/// <param name="space">Describes the space through which connectivity needs to be preserved.</param>
		/// <param name="cutoutRadius">Half-thickness of passageways.</param>
		/// <param name="maximumCutoutSpacing">
		/// If greater than zero, inserts additional passageways, ensuring that passageways are no
		/// greater than this distance apart (in Chebyshev distance).
		/// </param>
		public CellLayer<bool> PlanPassages(
			MersenneTwister random,
			CellLayer<bool> space,
			int cutoutRadius,
			int maximumCutoutSpacing = 0)
		{
			CheckHasMapShape(space);

			var passages = new CellLayer<bool>(Map);

			if (cutoutRadius <= 0)
				return passages;

			if (maximumCutoutSpacing > 0)
			{
				space = CellLayerUtils.Clone(space);
				var roominess = new CellLayer<int>(Map);
				CellLayerUtils.ChebyshevRoom(roominess, space, false);
				foreach (var mpos in Map.AllCells.MapCoords)
					roominess[mpos] = Math.Min(
						maximumCutoutSpacing,
						roominess[mpos]);

				while (true)
				{
					var (chosenMPos, room) = CellLayerUtils.FindRandomBest(
						roominess,
						random,
						(a, b) => a.CompareTo(b));
					if (room < maximumCutoutSpacing)
						break;

					var projections = Symmetry.RotateAndMirrorCPos(
						chosenMPos.ToCPos(Map),
						space,
						Rotations,
						Mirror);
					foreach (var projection in projections)
					{
						if (space.Contains(projection))
							space[projection] = false;
						var minX = projection.X - 2 * maximumCutoutSpacing + 1;
						var minY = projection.Y - 2 * maximumCutoutSpacing + 1;
						var maxX = projection.X + 2 * maximumCutoutSpacing - 1;
						var maxY = projection.Y + 2 * maximumCutoutSpacing - 1;
						for (var y = minY; y <= maxY; y++)
							for (var x = minX; x <= maxX; x++)
							{
								var mpos = new CPos(x, y).ToMPos(Map);
								if (roominess.Contains(mpos))
									roominess[mpos] = 0;
							}
					}
				}
			}

			var matrixSpace = CellLayerUtils.ToMatrix(space, false);

			// deflated is grid points, not squares. Has a size of `size + 1`.
			var deflated = MatrixUtils.DeflateSpace(matrixSpace, false);
			var kernel = new Matrix<bool>(2 * cutoutRadius, 2 * cutoutRadius).Fill(true);
			var inflated = MatrixUtils.KernelDilateOrErode(deflated.Map(v => v != 0), kernel, new int2(cutoutRadius - 1, cutoutRadius - 1), true);
			CellLayerUtils.FromMatrix(passages, inflated, true);

			return passages;
		}

		/// <summary>
		/// Plan paths for roads that travel through the middle of playable space.
		/// </summary>
		/// <param name="availableSpace">Space in which roads are permitted.</param>
		/// <param name="minimumSpacing">Minimum distance that roads must be from the edges of available space.</param>
		/// <param name="minimumLength">Roads shorter than this will be merged or pruned.</param>
		public CPos[][] PlanRoads(
			CellLayer<bool> availableSpace,
			int minimumSpacing,
			int minimumLength)
		{
			CheckHasMapShape(availableSpace);

			// For awkward symmetries, we try harder to make sure roads are fairer.
			// This can degrade the quantity of roads, though.
			var imperfectSymmetry =
				Mirror != Symmetry.Mirror.None ||
				Rotations == 3 ||
				Rotations >= 5;
			var gridType = Map.Grid.Type;

			// Enlargement must increase dimensions by multiple of 4 to maximize compatibility
			// with IsometricRectangular grids, where a non-multiple of 4 would change how the
			// center aligns with the grid.
			var enlargedSize = new Size(
				Map.MapSize.Width + (Map.MapSize.Width & ~3) + 4,
				Map.MapSize.Height + (Map.MapSize.Height & ~3) + 4);

			var space = new CellLayer<bool>(gridType, enlargedSize);
			space.Clear(true);

			var enlargedOffset =
				CellLayerUtils.WPosToCPos(CellLayerUtils.Center(space), gridType)
					- CellLayerUtils.WPosToCPos(CellLayerUtils.Center(Map.Tiles), gridType);

			foreach (var cpos in Map.AllCells)
				space[cpos + enlargedOffset] = availableSpace[cpos];

			space = ImproveSymmetry(space, true, (a, b) => a && b);

			var matrixSpace = CellLayerUtils.ToMatrix(space, true);
			var kernel = new Matrix<bool>(minimumSpacing * 2 + 1, minimumSpacing * 2 + 1);
			MatrixUtils.OverCircle(
				matrix: kernel,
				centerIn1024ths: kernel.Size * 512,
				radiusIn1024ths: minimumSpacing * 1024,
				outside: false,
				action: (xy, _) => kernel[xy] = true);
			var dilated = MatrixUtils.KernelDilateOrErode(
				matrixSpace,
				kernel,
				new int2(minimumSpacing, minimumSpacing),
				false);
			var deflated = MatrixUtils.DeflateSpace(dilated, true);

			if (imperfectSymmetry)
			{
				var changing = true;
				while (changing)
				{
					changing = false;

					// Delete short paths.
					{
						MatrixUtils.RemoveStubsFromDirectionMapInPlace(deflated);
						var paths = MatrixUtils.DirectionMapToPaths(deflated);
						if (paths.Length == 0)
							break;

						var minLength = paths.Min(p => p.Length);
						if (minLength < minimumLength)
						{
							changing = true;
							var shortPaths = paths
								.Where(path => path.Length == minLength);
							foreach (var path in shortPaths)
								foreach (var point in path)
									deflated[point] = 0;
							MatrixUtils.RemoveStubsFromDirectionMapInPlace(deflated);
						}
					}

					// Prune asymmetric paths.
					{
						const int Dilation = 3;
						var nearPath = MatrixUtils.KernelDilateOrErode(
							deflated.Map(v => v != 0),
							new Matrix<bool>(Dilation * 2 + 1, Dilation * 2 + 1).Fill(true),
							new int2(Dilation, Dilation),
							true);
						var matrixPaths = MatrixUtils.DirectionMapToPaths(deflated);
						foreach (var path in matrixPaths)
						{
							var cposPath = CellLayerUtils.FromMatrixPoints([path], space)[0];
							var projectedPoints = cposPath
								.SelectMany(p => Symmetry.RotateAndMirrorCPos(p, space, Rotations, Mirror))
								.ToArray();
							var matrixPoints = CellLayerUtils.ToMatrixPoints([projectedPoints], space)[0];
							if (!matrixPoints.All(p => !nearPath.ContainsXY(p) || nearPath[p]))
							{
								// The path doesn't exist across all symmetries (or isn't consistent enough).
								changing = true;
								foreach (var point in path)
									deflated[point] = 0;
							}
						}
					}
				}
			}

			var matrixPointArrays = MatrixUtils.DirectionMapToPathsWithPruning(
				input: deflated,
				minimumLength: minimumLength,
				minimumJunctionSeparation: 6,
				preserveEdgePaths: true);
			var pointArrays = CellLayerUtils.FromMatrixPoints(matrixPointArrays, space);
			pointArrays = TilingPath.RetainDisjointPaths(pointArrays);
			pointArrays = pointArrays
				.Select(a => a.Select(p => p - enlargedOffset).ToArray())
				.Select(a => TilingPath.ChirallyNormalizePathPoints(a, cvec => CellLayerUtils.CornerToWPos(cvec, gridType) - CellLayerUtils.Center(Map)))
				.ToArray();

			return pointArrays;
		}

		/// <summary>
		/// Given a resource noise pattern, rank cells for resource growth. (Higher is better.)
		/// Resources will be limited to masked cells. Resources will only be placed on compatible
		/// terrain tiles and will avoid actor footprints.
		/// Resources can be biased towards or away from specified actors. Biases are applied in
		/// the order they are supplied, but all reservations take precedence.
		/// Resource type will be determined by proximity to resource spawn actors, or a default
		/// resource.
		/// </summary>
		public (CellLayer<int> Plan, CellLayer<ResourceTypeInfo> TypePlan) PlanResources(
			CellLayer<int> pattern,
			CellLayer<bool> mask,
			ResourceTypeInfo defaultResource,
			IReadOnlyList<ResourceBias> resourceBiases)
		{
			CheckHasMapShape(pattern);
			CheckHasMapShape(mask);

			// IReadOnlyDictionary<string, ResourceTypeInfo> resourceSpawnSeeds = ...;
			var resourceTypes = Map.Rules.Actors[SystemActors.World]
				.TraitInfoOrDefault<ResourceLayerInfo>()
				.ResourceTypes
					.OrderBy(kv => kv.Key)
					.Select(kv => kv.Value)
					.ToImmutableArray();
			var allowedTerrainResourceCombos = resourceTypes
				.SelectMany(resourceTypeInfo => resourceTypeInfo.AllowedTerrainTypes
					.Select(terrainName => (resourceTypeInfo, terrainInfo.GetTerrainIndex(terrainName))))
				.ToImmutableHashSet();

			var strengths = new Dictionary<ResourceTypeInfo, CellLayer<int>>();
			foreach (var resourceType in resourceTypes)
			{
				var strength = new CellLayer<int>(Map);
				strength.Clear(1);
				strengths.Add(resourceType, strength);
			}

			foreach (var bias in resourceBiases)
			{
				if (bias.Bias == null || bias.BiasRadius == null)
					continue;

				IEnumerable<ResourceTypeInfo> types = bias.ResourceType != null
					? [bias.ResourceType]
					: resourceTypes;
				foreach (var resourceType in types)
				{
					var strength = strengths[resourceType];
					CellLayerUtils.OverCircle(
						cellLayer: strength,
						wCenter: bias.WPos,
						wRadius: bias.BiasRadius.Value,
						outside: false,
						action: (mpos, _, _, wrSq) =>
							strength[mpos] = bias.Bias(strength[mpos], wrSq));
				}
			}

			var maxStrength1024ths = new CellLayer<int>(Map);
			maxStrength1024ths.Clear(1);
			var bestResource = new CellLayer<ResourceTypeInfo>(Map);
			bestResource.Clear(defaultResource);
			foreach (var resourceStrength in strengths)
			{
				var resource = resourceStrength.Key;
				var strength1024ths = resourceStrength.Value;
				foreach (var mpos in Map.AllCells.MapCoords)
					if (strength1024ths[mpos] > maxStrength1024ths[mpos])
					{
						maxStrength1024ths[mpos] = strength1024ths[mpos];
						bestResource[mpos] = resource;
					}
			}

			// Closer to +inf means "more preferable" for plan.
			var plan = new CellLayer<int>(Map);
			foreach (var mpos in Map.AllCells.MapCoords)
			{
				plan[mpos] = pattern[mpos] >= 0
					? pattern[mpos] * maxStrength1024ths[mpos]
					: -int.MaxValue;
			}

			foreach (var mpos in Map.AllCells.MapCoords)
				if (!mask[mpos] || !allowedTerrainResourceCombos.Contains((bestResource[mpos], Map.GetTerrainIndex(mpos))))
					plan[mpos] = -int.MaxValue;

			foreach (var bias in resourceBiases)
			{
				if (bias.ExclusionRadius == null)
					continue;

				foreach (var resourceType in resourceTypes)
				{
					CellLayerUtils.OverCircle(
						cellLayer: plan,
						wCenter: bias.WPos,
						wRadius: bias.ExclusionRadius.Value,
						outside: false,
						action: (mpos, _, _, wrSq) =>
							plan[mpos] = -int.MaxValue);
				}
			}

			plan = ImproveSymmetry(plan, -int.MaxValue, int.Min);

			return (plan, bestResource);
		}

		/// <summary>
		/// Given a resource plan, place resources onto the map up to a target value.
		/// Resources are placed first on the pattern cells with the greatest value.
		/// No resources will be placed on pattern cells with a value less than 0.
		/// The plan should only contain values >= 0 where resource placement is legal.
		/// The type of resource placed is specified by typePlan.
		/// Any previously existing resources on the map will be cleared.
		/// </summary>
		public void GrowResources(
			CellLayer<int> plan,
			CellLayer<ResourceTypeInfo> typePlan,
			long targetValue)
		{
			CheckHasMapShape(plan);
			CheckHasMapShape(typePlan);

			var remaining = targetValue;

			var resourceTypes = Map.Rules.Actors[SystemActors.World].TraitInfoOrDefault<ResourceLayerInfo>().ResourceTypes;
			var playerResourcesInfo = Map.Rules.Actors[SystemActors.Player].TraitInfoOrDefault<PlayerResourcesInfo>();
			var resourceValues = playerResourcesInfo.ResourceValues
					.ToDictionary(kv => resourceTypes[kv.Key], kv => kv.Value);

			// Closer to -inf means "more preferable" for priorities.
			var priorities = new PriorityArray<int>(
				plan.Size.Width * plan.Size.Height,
				int.MaxValue);
			{
				var i = 0;
				foreach (var v in plan)
					priorities[i++] = -v;
			}

			int PriorityIndex(MPos mpos) => mpos.V * plan.Size.Width + mpos.U;
			MPos PriorityMPos(int index)
			{
				var v = Math.DivRem(index, plan.Size.Width, out var u);
				return new MPos(u, v);
			}

			Map.Resources.Clear();

			// Return resource value of a given square.
			// Matches the logic in ResourceLayer trait.
			int CheckValue(CPos cpos)
			{
				if (!Map.Resources.Contains(cpos))
					return 0;
				var resource = Map.Resources[cpos].Type;
				if (resource == 0)
					return 0;

				var resourceType = typePlan[cpos];

				var adjacent = 0;
				var directions = CVec.Directions;
				for (var i = 0; i < directions.Length; i++)
				{
					var c = cpos + directions[i];
					if (Map.Resources.Contains(c) && Map.Resources[c].Type == resource)
						++adjacent;
				}

				// We need to have at least one resource in the cell.
				// HACK: we should not be lerping to 9, as maximum adjacent resources is 8.
				// HACK: it's too disruptive to fix.
				var density = Math.Max(int2.Lerp(0, resourceType.MaxDensity, adjacent, 9), 1);

				return resourceValues[resourceType] * density;
			}

			int CheckValue3By3(CPos cpos)
			{
				var total = 0;
				for (var y = -1; y <= 1; y++)
					for (var x = -1; x <= 1; x++)
						total += CheckValue(cpos + new CVec(x, y));

				return total;
			}

			var gridType = Map.Grid.Type;

			// Set and return change in overall value.
			int AddResource(CPos cpos)
			{
				var mpos = cpos.ToMPos(gridType);
				priorities[PriorityIndex(mpos)] = int.MaxValue;

				// Generally shouldn't happen, but perhaps a rotation/mirror related inaccuracy.
				if (Map.Resources[mpos].Type != 0)
					return 0;

				var resourceType = typePlan[mpos];
				var oldValue = CheckValue3By3(cpos);
				Map.Resources[mpos] = new ResourceTile(
					resourceType.ResourceIndex,
					resourceType.MaxDensity);
				var newValue = CheckValue3By3(cpos);
				return newValue - oldValue;
			}

			while (remaining > 0)
			{
				var n = priorities.GetMinIndex();
				if (priorities[n] == int.MaxValue)
					break;

				var chosenMPos = PriorityMPos(n);
				var chosenCPos = chosenMPos.ToCPos(gridType);
				foreach (var cpos in Symmetry.RotateAndMirrorCPos(chosenCPos, plan, Rotations, Mirror))
					if (Map.Resources.Contains(cpos))
						remaining -= AddResource(cpos);
			}
		}

		/// <summary>
		/// Create a mask for placing decorations in out-of-the-way locations on a map.
		/// </summary>
		/// <param name="random">Random source for layout and tiling.</param>
		/// <param name="space">Space that decorations must not significantly choke.</param>
		/// <param name="zoneable">Cells where decoration is allowed.</param>
		/// <param name="coverage">Maximum fraction of map to cover in decorations.</param>
		/// <param name="featureSize">Noise feature size for layout.</param>
		/// <param name="density">Density of decoration layout.</param>
		/// <param name="minimumDensity">
		/// Enforces a minimum local density of decorations. This can, for example, be used to
		/// ensure that villages have a substantial size, preventing lonely buildings. Decoration
		/// cells are removed until the minimum density is satisfied for remaining cells.
		/// </param>
		/// <param name="minimumDensityRadius">Enforcement radius of minimum density.</param>
		public CellLayer<bool> DecorationPattern(
			MersenneTwister random,
			CellLayer<bool> space,
			CellLayer<bool> zoneable,
			int coverage,
			int featureSize,
			int density,
			int minimumDensity,
			int minimumDensityRadius)
		{
			CheckHasMapShape(space);
			CheckHasMapShape(zoneable);

			var matrixSpace = CellLayerUtils.ToMatrix(space, true);
			var deflated = MatrixUtils.DeflateSpace(matrixSpace, false);
			var kernel = new Matrix<bool>(2, 2).Fill(true);
			var reservedMatrix = MatrixUtils.KernelDilateOrErode(deflated.Map(v => v != 0), kernel, new int2(0, 0), true);
			var reserved = new CellLayer<bool>(Map);
			CellLayerUtils.FromMatrix(reserved, reservedMatrix, true);

			var decorationNoise = new CellLayer<int>(Map);
			NoiseUtils.SymmetricFractalNoiseIntoCellLayer(
				random,
				decorationNoise,
				Rotations,
				Mirror,
				featureSize,
				NoiseUtils.WhiteAmplitude);

			var densityNoise = new CellLayer<int>(Map);
			NoiseUtils.SymmetricFractalNoiseIntoCellLayer(
				random,
				densityNoise,
				Rotations,
				Mirror,
				1024,
				NoiseUtils.PinkAmplitude);
			var densityMask = CellLayerUtils.CalibratedBooleanThreshold(
				densityNoise, density, FractionMax);

			var decorable = new CellLayer<bool>(Map);
			var totalDecorable = 0;
			foreach (var mpos in Map.AllCells.MapCoords)
			{
				var isDecorable =
					zoneable[mpos] && space[mpos] && !reserved[mpos] && densityMask[mpos];
				decorable[mpos] = isDecorable;
				if (isDecorable)
					totalDecorable++;
				else
					decorationNoise[mpos] = -1024 * 1024;
			}

			var mapArea = Map.MapSize.Width * Map.MapSize.Height;
			var decorationMask = CellLayerUtils.CalibratedBooleanThreshold(
				decorationNoise, totalDecorable * coverage / FractionMax, mapArea);
			foreach (var mpos in Map.AllCells.MapCoords)
				decorable[mpos] &= decorationMask[mpos];

			for (var i = 0; i < 8; i++)
			{
				var (blurred, changes) = MatrixUtils.BooleanBlur(
					CellLayerUtils.ToMatrix(decorable, false),
					minimumDensityRadius,
					FractionMax - minimumDensity, FractionMax);
				if (changes == 0)
					break;

				var densityFilter = new CellLayer<bool>(Map);
				CellLayerUtils.FromMatrix(densityFilter, blurred);

				foreach (var mpos in Map.AllCells.MapCoords)
					decorable[mpos] &= densityFilter[mpos];
			}

			decorable = ImproveSymmetry(decorable, false, (a, b) => a && b);

			return decorable;
		}
	}
}
