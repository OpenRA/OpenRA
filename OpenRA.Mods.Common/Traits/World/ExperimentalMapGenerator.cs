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
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;
using static OpenRA.Mods.Common.Traits.ResourceLayerInfo;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	public sealed class ExperimentalMapGeneratorInfo : TraitInfo<ExperimentalMapGenerator>, IMapGeneratorInfo
	{
		[FieldLoader.Require]
		public readonly string Type = null;

		[FieldLoader.Require]
		[FluentReference]
		public readonly string Name = null;

		// This is purely of interest to the linter.
		[FieldLoader.LoadUsing(nameof(FluentReferencesLoader))]
		[FluentReference]
		public readonly List<string> FluentReferences = null;

		[FieldLoader.LoadUsing(nameof(SettingsLoader))]
		public readonly MiniYaml Settings;

		string IMapGeneratorInfo.Type => Type;
		string IMapGeneratorInfo.Name => Name;

		static MiniYaml SettingsLoader(MiniYaml my)
		{
			return my.NodeWithKey("Settings").Value;
		}

		static List<string> FluentReferencesLoader(MiniYaml my)
		{
			return MapGeneratorSettings.DumpFluent(my.NodeWithKey("Settings").Value);
		}

		const int FractionMax = 1000;
		const int EntityBonusMax = 1000000;

		sealed class Parameters
		{
			[FieldLoader.Ignore]
			public readonly Map Map;
			[FieldLoader.Ignore]
			public readonly ITemplatedTerrainInfo TemplatedTerrainInfo;

			[FieldLoader.Require]
			public readonly int Seed = default;
			[FieldLoader.Require]
			public readonly int Rotations = default;
			[FieldLoader.LoadUsing(nameof(MirrorLoader))]
			public readonly Symmetry.Mirror Mirror = default;
			[FieldLoader.Require]
			public readonly int Players = default;
			[FieldLoader.Require]
			public readonly int TerrainFeatureSize = default;
			[FieldLoader.Require]
			public readonly int ForestFeatureSize = default;
			[FieldLoader.Require]
			public readonly int ResourceFeatureSize = default;
			[FieldLoader.Require]
			public readonly int CivilianBuildingsFeatureSize = default;
			[FieldLoader.Require]
			public readonly int Water = default;
			[FieldLoader.Require]
			public readonly int Mountains = default;
			[FieldLoader.Require]
			public readonly int Forests = default;
			[FieldLoader.Require]
			public readonly int ForestCutout = default;
			[FieldLoader.Require]
			public readonly int MaximumCutoutSpacing = default;
			[FieldLoader.Require]
			public readonly int ExternalCircularBias = default;
			[FieldLoader.Require]
			public readonly int TerrainSmoothing = default;
			[FieldLoader.Require]
			public readonly int SmoothingThreshold = default;
			[FieldLoader.Require]
			public readonly int MinimumLandSeaThickness = default;
			[FieldLoader.Require]
			public readonly int MinimumMountainThickness = default;
			[FieldLoader.Require]
			public readonly int MaximumAltitude = default;
			[FieldLoader.Require]
			public readonly int RoughnessRadius = default;
			[FieldLoader.Require]
			public readonly int Roughness = default;
			[FieldLoader.Require]
			public readonly int MinimumTerrainContourSpacing = default;
			[FieldLoader.Require]
			public readonly int MinimumCliffLength = default;
			[FieldLoader.Require]
			public readonly int ForestClumpiness = default;
			[FieldLoader.Require]
			public readonly bool DenyWalledAreas = default;
			[FieldLoader.Require]
			public readonly int EnforceSymmetry = default;
			[FieldLoader.Require]
			public readonly bool Roads = default;
			[FieldLoader.Require]
			public readonly int RoadSpacing = default;
			[FieldLoader.Require]
			public readonly int RoadShrink = default;
			[FieldLoader.Require]
			public readonly bool CreateEntities = default;
			[FieldLoader.Require]
			public readonly int AreaEntityBonus = default;
			[FieldLoader.Require]
			public readonly int PlayerCountEntityBonus = default;
			[FieldLoader.Require]
			public readonly int CentralSpawnReservationFraction = default;
			[FieldLoader.Require]
			public readonly int ResourceSpawnReservation = default;
			[FieldLoader.Require]
			public readonly int SpawnRegionSize = default;
			[FieldLoader.Require]
			public readonly int SpawnBuildSize = default;
			[FieldLoader.Require]
			public readonly int MinimumSpawnRadius = default;
			[FieldLoader.Require]
			public readonly int SpawnResourceSpawns = default;
			[FieldLoader.Require]
			public readonly int SpawnReservation = default;
			[FieldLoader.Require]
			public readonly int SpawnResourceBias = default;
			[FieldLoader.Require]
			public readonly int ResourcesPerPlayer = default;
			[FieldLoader.Require]
			public readonly int OreUniformity = default;
			[FieldLoader.Require]
			public readonly int OreClumpiness = default;
			[FieldLoader.Require]
			public readonly int MaximumExpansionResourceSpawns = default;
			[FieldLoader.Require]
			public readonly int MaximumResourceSpawnsPerExpansion = default;
			[FieldLoader.Require]
			public readonly int MinimumExpansionSize = default;
			[FieldLoader.Require]
			public readonly int MaximumExpansionSize = default;
			[FieldLoader.Require]
			public readonly int ExpansionInner = default;
			[FieldLoader.Require]
			public readonly int ExpansionBorder = default;
			[FieldLoader.Require]
			public readonly int MinimumBuildings = default;
			[FieldLoader.Require]
			public readonly int MaximumBuildings = default;
			[FieldLoader.LoadUsing(nameof(BuildingWeightsLoader))]
			public readonly IReadOnlyDictionary<string, int> BuildingWeights = default;
			[FieldLoader.Require]
			public readonly int CivilianBuildings = default;
			[FieldLoader.Require]
			public readonly int CivilianBuildingDensity = default;
			[FieldLoader.Require]
			public readonly int MinimumCivilianBuildingDensity = default;
			[FieldLoader.Require]
			public readonly int CivilianBuildingDensityRadius = default;

			[FieldLoader.Require]
			public readonly ushort LandTile = default;
			[FieldLoader.Require]
			public readonly ushort WaterTile = default;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> ForestObstacles;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> UnplayableObstacles;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> CivilianBuildingsObstacles;
			[FieldLoader.Ignore]
			public readonly IReadOnlyDictionary<ushort, IReadOnlyList<MultiBrush>> RepaintTiles;

			[FieldLoader.Ignore]
			public readonly IReadOnlyDictionary<string, ResourceTypeInfo> ResourceTypes;
			[FieldLoader.Ignore]
			public readonly ResourceTypeInfo DefaultResource;
			[FieldLoader.Ignore]
			public readonly IReadOnlyDictionary<ResourceTypeInfo, int> ResourceValues;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<(ResourceTypeInfo, byte)> AllowedTerrainResourceCombos;
			[FieldLoader.Ignore]
			public readonly IReadOnlyDictionary<string, ResourceTypeInfo> ResourceSpawnSeeds;
			[FieldLoader.LoadUsing(nameof(ResourceSpawnWeightsLoader))]
			public readonly IReadOnlyDictionary<string, int> ResourceSpawnWeights = default;

			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> ClearTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> PlayableTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> PartiallyPlayableTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> UnplayableTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> DominantTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> ZoneableTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<string> PartiallyPlayableCategories;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> ClearSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> BeachSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> CliffSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> RoadSegmentTypes;

			[FieldLoader.Ignore]
			public readonly int SymmetryCount;
			[FieldLoader.Ignore]
			public readonly int TotalPlayers;

			public Parameters(Map map, MiniYaml my)
			{
				FieldLoader.Load(this, my);
				Map = map;
				TemplatedTerrainInfo = Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
				ForestObstacles = MultiBrush.LoadCollection(map, my.NodeWithKey("ForestObstacles").Value.Value);
				UnplayableObstacles = MultiBrush.LoadCollection(map, my.NodeWithKey("UnplayableObstacles").Value.Value);
				CivilianBuildingsObstacles = MultiBrush.LoadCollection(map, my.NodeWithKey("CivilianBuildingsObstacles").Value.Value);
				RepaintTiles = my.NodeWithKeyOrDefault("RepaintTiles")?.Value.ToDictionary(
					k =>
					{
						if (Exts.TryParseUshortInvariant(k, out var tile))
							return tile;
						else
							throw new YamlException($"RepaintTile {k} is not a ushort");
					},
					v => MultiBrush.LoadCollection(map, v.Value) as IReadOnlyList<MultiBrush>);
				RepaintTiles ??= ImmutableDictionary<ushort, IReadOnlyList<MultiBrush>>.Empty;

				ResourceTypes = map.Rules.Actors[SystemActors.World].TraitInfoOrDefault<ResourceLayerInfo>().ResourceTypes;
				if (!ResourceTypes.TryGetValue(my.NodeWithKey("DefaultResource").Value.Value, out DefaultResource))
					throw new YamlException("DefaultResource is not valid");
				var playerResourcesInfo = map.Rules.Actors[SystemActors.Player].TraitInfoOrDefault<PlayerResourcesInfo>();
				ResourceValues = playerResourcesInfo.ResourceValues
					.ToDictionary(kv => ResourceTypes[kv.Key], kv => kv.Value);
				AllowedTerrainResourceCombos = ResourceTypes
					.Values
					.SelectMany(resourceTypeInfo => resourceTypeInfo.AllowedTerrainTypes
						.Select(terrainName => (resourceTypeInfo, TemplatedTerrainInfo.GetTerrainIndex(terrainName))))
					.ToImmutableHashSet();
				try
				{
					ResourceSpawnSeeds = my.NodeWithKey("ResourceSpawnSeeds").Value
						.ToDictionary(subMy => subMy.Value)
						.ToDictionary(kv => kv.Key, kv => ResourceTypes[kv.Value]);
				}
				catch (KeyNotFoundException e)
				{
					throw new YamlException("Bad ResourceSpawnSeeds resource: " + e);
				}

				switch (Rotations)
				{
					case 1:
					case 2:
					case 4:
						break;
					default:
						EnforceSymmetry = 0;
						break;
				}

				IReadOnlySet<byte> ParseTerrainIndexes(string key)
				{
					return my.NodeWithKey(key).Value.Value
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.Select(TemplatedTerrainInfo.GetTerrainIndex)
						.ToImmutableHashSet();
				}

				IReadOnlyList<string> ParseSegmentTypes(string key)
				{
					return my.NodeWithKey(key).Value.Value
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.ToImmutableArray();
				}

				ClearTerrain = ParseTerrainIndexes("ClearTerrain");
				PlayableTerrain = ParseTerrainIndexes("PlayableTerrain");
				PartiallyPlayableTerrain = ParseTerrainIndexes("PartiallyPlayableTerrain");
				UnplayableTerrain = ParseTerrainIndexes("UnplayableTerrain");
				DominantTerrain = ParseTerrainIndexes("DominantTerrain");
				ZoneableTerrain = ParseTerrainIndexes("ZoneableTerrain");

				PartiallyPlayableCategories = my.NodeWithKey("PartiallyPlayableCategories").Value.Value
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.ToImmutableHashSet();

				ClearSegmentTypes = ParseSegmentTypes("ClearSegmentTypes");
				BeachSegmentTypes = ParseSegmentTypes("BeachSegmentTypes");
				CliffSegmentTypes = ParseSegmentTypes("CliffSegmentTypes");
				RoadSegmentTypes = ParseSegmentTypes("RoadSegmentTypes");

				SymmetryCount = Symmetry.RotateAndMirrorProjectionCount(Rotations, Mirror);
				TotalPlayers = Players * SymmetryCount;

				Validate();
			}

			static object MirrorLoader(MiniYaml my)
			{
				if (Symmetry.TryParseMirror(my.NodeWithKey("Mirror").Value.Value, out var mirror))
					return mirror;
				else
					throw new YamlException($"Invalid Mirror value `{my.NodeWithKey("Mirror").Value.Value}`");
			}

			static IReadOnlyDictionary<string, int> BuildingWeightsLoader(MiniYaml my)
			{
				return my.NodeWithKey("BuildingWeights").Value.ToDictionary(subMy =>
					{
						if (Exts.TryParseInt32Invariant(subMy.Value, out var f))
							return f;
						else
							throw new YamlException($"Invalid building weight `{subMy.Value}`");
					});
			}

			static IReadOnlyDictionary<string, int> ResourceSpawnWeightsLoader(MiniYaml my)
			{
				return my.NodeWithKey("ResourceSpawnWeights").Value.ToDictionary(subMy =>
					{
						if (Exts.TryParseInt32Invariant(subMy.Value, out var f))
							return f;
						else
							throw new YamlException($"Invalid resource spawn weight `{subMy.Value}`");
					});
			}

			public void Validate()
			{
				if (Rotations < 1)
					throw new MapGenerationException("Rotations must be >= 1");
				if (TerrainFeatureSize < 1)
					throw new MapGenerationException("TerrainFeatureSize must be >= 1");
				if (ForestFeatureSize < 1)
					throw new MapGenerationException("ForestFeatureSize must be >= 1");
				if (ResourceFeatureSize < 1)
					throw new MapGenerationException("ResourceFeatureSize must be >= 1");
				if (CivilianBuildingsFeatureSize < 1)
					throw new MapGenerationException("CivilianBuildingsFeatureSize must be >= 1");
				if (TerrainSmoothing < 0 || TerrainSmoothing > MatrixUtils.MaxBinomialKernelRadius)
					throw new MapGenerationException($"TerrainSmoothing must be between 0 and {MatrixUtils.MaxBinomialKernelRadius} inclusive");
				if (SmoothingThreshold < (FractionMax + 1) / 2 || SmoothingThreshold > FractionMax)
					throw new MapGenerationException($"SmoothingThreshold must be between {(FractionMax + 1) / 2} and {FractionMax} inclusive");
				if (MinimumLandSeaThickness < 1)
					throw new MapGenerationException("MinimumLandSeaThickness must be >= 1");
				if (MinimumMountainThickness < 1)
					throw new MapGenerationException("MinimumMountainThickness must be >= 1");
				if (Water < 0 || Water > FractionMax)
					throw new MapGenerationException($"Water must be between 0 and {FractionMax} inclusive");
				if (Forests < 0 || Forests > FractionMax)
					throw new MapGenerationException($"Forest must be between 0 and {FractionMax} inclusive");
				if (ForestCutout < 0)
					throw new MapGenerationException("ForestCutout must be >= 0");
				if (MaximumCutoutSpacing < 0)
					throw new MapGenerationException("TopologyAugmentationThreshold must be >= 0");
				if (ForestClumpiness < 0)
					throw new MapGenerationException("ForestClumpiness must be >= 0");
				if (Mountains < 0 || Mountains > FractionMax)
					throw new MapGenerationException($"Mountains must be between 0 and {FractionMax} inclusive");
				if (Roughness < 0 || Roughness > FractionMax)
					throw new MapGenerationException("Roughness must be between 0 and {FractionMax}");
				if (RoughnessRadius < 1)
					throw new MapGenerationException("RoughnessRadius must be >= 1");
				if (MaximumAltitude < 0)
					throw new MapGenerationException("MaximumAltitude must be >= 0");
				if (MinimumTerrainContourSpacing < 0)
					throw new MapGenerationException("MinimumTerrainContourSpacing must be >= 0");
				if (MinimumCliffLength < 1)
					throw new MapGenerationException("MinimumCliffLength must be >= 1");
				if (RoadSpacing < 0)
					throw new MapGenerationException("RoadSpacing must be >= 0");
				if (RoadShrink < 0)
					throw new MapGenerationException("RoadShrink must be >= 0");
				if (Players < 0)
					throw new MapGenerationException("Players must be >= 0");
				if (CentralSpawnReservationFraction < 0)
					throw new MapGenerationException("CentralSpawnReservationFraction must be >= 0");
				if (AreaEntityBonus < 0)
					throw new MapGenerationException("PlayableAreaDensityBonus must be >= 0");
				if (PlayerCountEntityBonus < 0)
					throw new MapGenerationException("PlayerCountDensityBonus must be >= 0");
				if (SpawnRegionSize < 1)
					throw new MapGenerationException("SpawnRegionSize must be >= 1");
				if (SpawnReservation < 1)
					throw new MapGenerationException("SpawnReservation must be >= 1");
				if (SpawnBuildSize < 1)
					throw new MapGenerationException("SpawnBuildSize must be >= 1");
				if (MinimumSpawnRadius < 1)
					throw new MapGenerationException("MinimumSpawnRadius must be >= 1");
				if (SpawnResourceSpawns < 0)
					throw new MapGenerationException("SpawnResourceSpawns must be >= 0");
				if (ResourceSpawnReservation < 1)
					throw new MapGenerationException("ResourceSpawnReservation must be >= 1");
				if (MaximumExpansionResourceSpawns < 0)
					throw new MapGenerationException("MaximumExpansionResourceSpawns must be >= 0");
				if (MinimumExpansionSize < 1)
					throw new MapGenerationException("MinimumExpansionSize must be >= 1");
				if (MaximumExpansionSize < 1)
					throw new MapGenerationException("MaximumExpansionSize must be >= 1");
				if (MinimumExpansionSize > MaximumExpansionSize)
					throw new MapGenerationException("MinimumExpansionSize must be <= maximumExpansionSize");
				if (ExpansionBorder < 1)
					throw new MapGenerationException("ExpansionBorder must be >= 1");
				if (ExpansionInner < 1)
					throw new MapGenerationException("ExpansionInner must be >= 1");
				if (MaximumResourceSpawnsPerExpansion < 1)
					throw new MapGenerationException("MaximumResourceSpawnsPerExpansion must be >= 1");
				if (MinimumBuildings < 0)
					throw new MapGenerationException("MinimumBuildings must be >= 0");
				if (MaximumBuildings < 0)
					throw new MapGenerationException("MaximumBuildings must be >= 0");
				if (MinimumBuildings > MaximumBuildings)
					throw new MapGenerationException("MinimumBuildings must be <= maximumBuildings");
				if (CivilianBuildings < 0 || CivilianBuildings > FractionMax)
					throw new MapGenerationException($"CivilianBuildings must be between 0 and {FractionMax} inclusive");
				if (CivilianBuildingDensity < 0 || CivilianBuildingDensity > FractionMax)
					throw new MapGenerationException($"CivilianBuildingDensity must be between 0 and {FractionMax} inclusive");
				if (MinimumCivilianBuildingDensity < 0 || MinimumCivilianBuildingDensity > FractionMax)
					throw new MapGenerationException($"MinimumCivilianBuildingDensity must be between 0 and {FractionMax} inclusive");
				if (CivilianBuildingDensityRadius < 0)
					throw new MapGenerationException("CivilianBuildingDensityRadius must be >= 0");
				if (ResourcesPerPlayer < 0)
					throw new MapGenerationException("ResourcesPerPlayer must be >= 0");
				if (OreUniformity < 0)
					throw new MapGenerationException("OreUniformity must be >= 0");
				if (OreClumpiness < 0)
					throw new MapGenerationException("OreClumpiness must be >= 0");
				foreach (var kv in BuildingWeights)
					if (kv.Value < 0)
						throw new MapGenerationException("BuildingWeights.* must be >= 0");
				foreach (var kv in ResourceSpawnWeights)
					if (kv.Value < 0)
						throw new MapGenerationException("ResourceSpawnWeights.* must be >= 0");
				foreach (var kv in ResourceSpawnWeights)
					if (!ResourceSpawnSeeds.ContainsKey(kv.Key))
						throw new MapGenerationException($"ResourceSpawnSeeds does not contain possible resource spawn `{kv.Key}`");

				if (!(TemplatedTerrainInfo.Templates.TryGetValue(LandTile, out var landTemplate) && landTemplate.Contains(0)))
					throw new MapGenerationException("LandTile is not valid");
				if (!(TemplatedTerrainInfo.Templates.TryGetValue(LandTile, out var waterTemplate) && waterTemplate.Contains(0)))
					throw new MapGenerationException("WaterTile is not valid");

				if (TotalPlayers > 32)
					throw new MapGenerationException("Total number of players must not exceed 32");
			}

			public static (T[] Types, int[] Weights) SplitWeights<T>(IReadOnlyDictionary<T, int> typeWeights)
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
		}

		public MapGeneratorSettings GetSettings(ITerrainInfo terrainInfo)
		{
			return MapGeneratorSettings.LoadSettings(Settings, terrainInfo);
		}

		public void Generate(Map map, MiniYaml settings)
		{
			const int ExternalBias = 4096;

			var size = map.MapSize;
			var minSpan = Math.Min(size.X, size.Y);
			var maxSpan = Math.Max(size.X, size.Y);
			var mapCenter1024ths = size * 512;
			var wMapCenter = CellLayerUtils.Center(map.Tiles);
			var matrixMapCenter1024ths = CellLayerUtils.CellBounds(map).Size.ToInt2() * 512;
			var cellBounds = CellLayerUtils.CellBounds(map);
			var minCSpan = Math.Min(cellBounds.Size.Width, cellBounds.Size.Height);
			var gridType = map.Grid.Type;

			var actorPlans = new List<ActorPlan>();

			var param = new Parameters(map, settings);

			var externalCircleRadius = minCSpan / 2 - (param.MinimumLandSeaThickness + param.MinimumMountainThickness);
			if (externalCircleRadius <= 0)
				throw new MapGenerationException("map is too small for circular shaping");

			var trivialMirror =
				size.X == size.Y ||
				param.Mirror == Symmetry.Mirror.None ||
				param.Mirror == Symmetry.Mirror.LeftMatchesRight ||
				param.Mirror == Symmetry.Mirror.TopMatchesBottom;

			var tileset = param.TemplatedTerrainInfo;

			var beachPermittedTemplates =
				TilingPath.PermittedSegments.FromType(tileset, param.BeachSegmentTypes);
			var beachTiles = beachPermittedTemplates.PossibleTiles().ToImmutableHashSet();

			var replaceabilityMap = new Dictionary<TerrainTile, MultiBrush.Replaceability>();
			var playabilityMap = new Dictionary<TerrainTile, PlayableSpace.Playability>();
			foreach (var kv in tileset.Templates)
			{
				var id = kv.Key;
				var template = kv.Value;
				for (var ti = 0; ti < template.TilesCount; ti++)
				{
					if (template[ti] == null)
						continue;
					var tile = new TerrainTile(id, (byte)ti);
					var type = tileset.GetTerrainIndex(tile);

					if (param.PlayableTerrain.Contains(type))
						playabilityMap[tile] = PlayableSpace.Playability.Playable;
					else if (param.PartiallyPlayableTerrain.Contains(type))
						playabilityMap[tile] = PlayableSpace.Playability.Partial;
					else if (param.UnplayableTerrain.Contains(type))
						playabilityMap[tile] = PlayableSpace.Playability.Unplayable;
					else
						throw new MapGenerationException($"Terrain index {type} has unknown playability.");

					if (id == param.LandTile)
					{
						replaceabilityMap[tile] = MultiBrush.Replaceability.Any;
					}
					else if (id == param.WaterTile)
					{
						replaceabilityMap[tile] = MultiBrush.Replaceability.Tile;
					}
					else
					{
						if (playabilityMap[tile] == PlayableSpace.Playability.Unplayable)
							replaceabilityMap[tile] = MultiBrush.Replaceability.None;
						else
							replaceabilityMap[tile] = MultiBrush.Replaceability.Actor;

						if (param.PartiallyPlayableCategories.Overlaps(template.Categories)
							&& playabilityMap[tile] == PlayableSpace.Playability.Unplayable)
						{
							playabilityMap[tile] = PlayableSpace.Playability.Partial;
						}
					}
				}
			}

			// Use `random` to derive separate independent random number generators.
			//
			// This prevents changes in one part of the algorithm from affecting randomness in
			// other parts and provides flexibility for future parallel processing.
			//
			// In order to maximize stability, additions should be appended only. Disused
			// derivatives may be deleted but should be replaced with their unused call to
			// random.Next(). All generators should be created unconditionally.
			var random = new MersenneTwister(param.Seed);

			var pickAnyRandom = new MersenneTwister(random.Next());
			var waterRandom = new MersenneTwister(random.Next());
			var beachTilingRandom = new MersenneTwister(random.Next());
			var cliffTilingRandom = new MersenneTwister(random.Next());
			var forestRandom = new MersenneTwister(random.Next());
			var forestTilingRandom = new MersenneTwister(random.Next());
			var symmetryTilingRandom = new MersenneTwister(random.Next());
			var debrisTilingRandom = new MersenneTwister(random.Next());
			var resourceRandom = new MersenneTwister(random.Next());
			var roadTilingRandom = new MersenneTwister(random.Next());
			var playerRandom = new MersenneTwister(random.Next());
			var expansionRandom = new MersenneTwister(random.Next());
			var buildingRandom = new MersenneTwister(random.Next());
			var topologyRandom = new MersenneTwister(random.Next());
			var repaintRandom = new MersenneTwister(random.Next());
			var decorationRandom = new MersenneTwister(random.Next());
			var decorationTilingRandom = new MersenneTwister(random.Next());

			TerrainTile PickTile(ushort tileType)
			{
				if (tileset.Templates.TryGetValue(tileType, out var template) && template.PickAny)
					return new TerrainTile(tileType, (byte)random.Next(0, template.TilesCount));
				else
					return new TerrainTile(tileType, 0);
			}

			foreach (var cell in map.AllCells)
			{
				var mpos = cell.ToMPos(gridType);
				map.Tiles[mpos] = PickTile(param.LandTile);
				map.Resources[mpos] = new ResourceTile(0, 0);
				map.Height[mpos] = 0;
			}

			var elevation = NoiseUtils.SymmetricFractalNoise(
				waterRandom,
				cellBounds.Size.ToInt2(),
				param.Rotations,
				param.Mirror,
				param.TerrainFeatureSize,
				NoiseUtils.PinkAmplitude);
			MatrixUtils.NormalizeRangeInPlace(elevation, 1024);

			if (param.TerrainSmoothing > 0)
			{
				var radius = param.TerrainSmoothing;
				elevation = MatrixUtils.BinomialBlur(elevation, radius);
			}

			MatrixUtils.CalibrateQuantileInPlace(
				elevation,
				0,
				param.Water, FractionMax);

			if (param.ExternalCircularBias != 0)
				MatrixUtils.OverCircle(
					matrix: elevation,
					centerIn1024ths: mapCenter1024ths,
					radiusIn1024ths: externalCircleRadius * 1024,
					outside: true,
					action: (xy, _) => elevation[xy] = param.ExternalCircularBias * ExternalBias);

			var landPlan = MatrixUtils.BooleanBlotch(
				elevation.Map(v => v >= 0),
				param.TerrainSmoothing,
				param.SmoothingThreshold, FractionMax,
				param.MinimumLandSeaThickness,
				/*bias=*/param.Water < FractionMax / 2);

			var beaches = CellLayerUtils.FromMatrixPoints(
				MatrixUtils.BordersToPoints(landPlan),
				map.Tiles);
			if (beaches.Length > 0)
			{
				var tiledBeaches = new CPos[beaches.Length][];
				for (var i = 0; i < beaches.Length; i++)
				{
					var beachPath = new TilingPath(
						map,
						beaches[i],
						(param.MinimumLandSeaThickness - 1) / 2,
						param.BeachSegmentTypes[0],
						param.BeachSegmentTypes[0],
						beachPermittedTemplates);
					beachPath
						.ExtendEdge(4)
						.OptimizeLoop();
					tiledBeaches[i] =
						beachPath.Tile(beachTilingRandom)
							?? throw new MapGenerationException("Could not fit tiles for beach");
				}

				var beachChiralityMatrix = MatrixUtils.PointsChirality(
					landPlan.Size,
					CellLayerUtils.ToMatrixPoints(tiledBeaches, map.Tiles));
				var beachChirality = new CellLayer<int>(map);
				CellLayerUtils.FromMatrix(beachChirality, beachChiralityMatrix);
				foreach (var mpos in map.AllCells.MapCoords)
				{
					// `map.Tiles[mpos].Type == param.LandTile` avoids overwriting beach tiles.
					if (beachChirality[mpos] < 0 && map.Tiles[mpos].Type == param.LandTile)
						map.Tiles[mpos] = PickTile(param.WaterTile);
				}
			}
			else
			{
				// There weren't any coastlines
				var tileType = landPlan[0] ? param.LandTile : param.WaterTile;
				foreach (var cell in map.AllCells)
				{
					var mpos = cell.ToMPos(gridType);
					map.Tiles[mpos] = PickTile(tileType);
				}
			}

			var nonLoopedCliffPermittedTemplates =
				TilingPath.PermittedSegments.FromInnerAndTerminalTypes(
					tileset, param.CliffSegmentTypes, param.ClearSegmentTypes);
			var loopedCliffPermittedTemplates =
				TilingPath.PermittedSegments.FromType(
					tileset, param.CliffSegmentTypes);
			if (param.ExternalCircularBias > 0)
			{
				var cliffRing = new CellLayer<bool>(map);
				CellLayerUtils.OverCircle(
					cellLayer: cliffRing,
					wCenter: wMapCenter,
					wRadius: (externalCircleRadius + param.MinimumMountainThickness) * 1024,
					outside: true,
					action: (mpos, _, _, _) => cliffRing[mpos] = true);
				foreach (var cliff in CellLayerUtils.BordersToPoints(cliffRing))
				{
					var isLoop = cliff[0] == cliff[^1];
					TilingPath cliffPath;
					if (isLoop)
						cliffPath = new TilingPath(
							map,
							cliff,
							(param.MinimumMountainThickness - 1) / 2,
							param.CliffSegmentTypes[0],
							param.CliffSegmentTypes[0],
							loopedCliffPermittedTemplates);
					else
						cliffPath = new TilingPath(
							map,
							cliff,
							(param.MinimumMountainThickness - 1) / 2,
							param.ClearSegmentTypes[0],
							param.ClearSegmentTypes[0],
							nonLoopedCliffPermittedTemplates);
					cliffPath
						.ExtendEdge(4)
						.OptimizeLoop();
					if (cliffPath.Tile(cliffTilingRandom) == null)
						throw new MapGenerationException("Could not fit tiles for exterior circle cliffs");
				}
			}

			if (param.Mountains > 0 || param.ExternalCircularBias == 1)
			{
				var roughnessMatrix = MatrixUtils.GridVariance(
					elevation,
					param.RoughnessRadius);
				MatrixUtils.CalibrateQuantileInPlace(
					roughnessMatrix,
					0,
					FractionMax - param.Roughness, FractionMax);
				var cliffMask = roughnessMatrix.Map(v => v >= 0);
				var mountainElevation = elevation.Clone();
				var cliffPlan = landPlan;
				if (param.ExternalCircularBias > 0)
					MatrixUtils.OverCircle(
						matrix: cliffPlan,
						centerIn1024ths: matrixMapCenter1024ths,
						radiusIn1024ths: externalCircleRadius * 1024,
						outside: true,
						action: (xy, _) => cliffPlan[xy] = false);

				for (var altitude = 1; altitude <= param.MaximumAltitude; altitude++)
				{
					// Limit mountain area to the existing mountain space (starting with all available land)
					var roominess = MatrixUtils.ChebyshevRoom(cliffPlan, true);
					var available = 0;
					var total = size.X * size.Y;
					for (var n = 0; n < mountainElevation.Data.Length; n++)
					{
						if (roominess.Data[n] < param.MinimumTerrainContourSpacing)
							mountainElevation.Data[n] = -1;
						else
							available++;

						total++;
					}

					MatrixUtils.CalibrateQuantileInPlace(
						mountainElevation,
						0,
						total - available * param.Mountains / FractionMax, total);
					cliffPlan = MatrixUtils.BooleanBlotch(
						mountainElevation.Map(v => v >= 0),
						param.TerrainSmoothing,
						param.SmoothingThreshold, FractionMax,
						param.MinimumMountainThickness,
						/*bias=*/false);
					var unmaskedCliffs = MatrixUtils.BordersToPoints(cliffPlan);
					var maskedCliffs = MatrixUtils.MaskPathPoints(unmaskedCliffs, cliffMask);
					var cliffs = CellLayerUtils.FromMatrixPoints(maskedCliffs, map.Tiles)
						.Where(cliff => cliff.Length >= param.MinimumCliffLength).ToArray();
					if (cliffs.Length == 0)
						break;
					foreach (var cliff in cliffs)
					{
						var isLoop = cliff[0] == cliff[^1];
						TilingPath cliffPath;
						if (isLoop)
							cliffPath = new TilingPath(
								map,
								cliff,
								(param.MinimumMountainThickness - 1) / 2,
								param.CliffSegmentTypes[0],
								param.CliffSegmentTypes[0],
								loopedCliffPermittedTemplates);
						else
							cliffPath = new TilingPath(
								map,
								cliff,
								(param.MinimumMountainThickness - 1) / 2,
								param.ClearSegmentTypes[0],
								param.ClearSegmentTypes[0],
								nonLoopedCliffPermittedTemplates);
						cliffPath
							.ExtendEdge(4)
							.OptimizeLoop();
						if (cliffPath.Tile(cliffTilingRandom) == null)
							throw new MapGenerationException("Could not fit tiles for cliffs");
					}
				}
			}

			if (param.Forests > 0)
			{
				var forestNoise = new CellLayer<int>(map);
				NoiseUtils.SymmetricFractalNoiseIntoCellLayer(
					forestRandom,
					forestNoise,
					param.Rotations,
					param.Mirror,
					param.ForestFeatureSize,
					wavelength => ClumpinessAmplitude(wavelength, param.ForestClumpiness));
				CellLayerUtils.CalibrateQuantileInPlace(
					forestNoise,
					0,
					FractionMax - param.Forests, FractionMax);

				var forestPlan = new CellLayer<bool>(map);
				foreach (var mpos in map.AllCells.MapCoords)
					if (param.ClearTerrain.Contains(map.GetTerrainIndex(mpos)) && forestNoise[mpos] >= 0)
						forestPlan[mpos] = true;

				if (param.ForestCutout > 0)
				{
					var space = new CellLayer<bool>(map);
					foreach (var mpos in map.AllCells.MapCoords)
						space[mpos] = param.ClearTerrain.Contains(map.GetTerrainIndex(mpos));

					// Improve symmetry.
					{
						var newSpace = new CellLayer<bool>(map);
						Symmetry.RotateAndMirrorOverCPos(
							space,
							param.Rotations,
							param.Mirror,
							(sources, destination)
								=> newSpace[destination] =
									sources.All(source => !space.TryGetValue(source, out var value) || value));
						space = newSpace;
					}

					if (param.MaximumCutoutSpacing > 0)
					{
						var roominess = new CellLayer<int>(map);
						CellLayerUtils.ChebyshevRoom(roominess, space, false);
						foreach (var mpos in map.AllCells.MapCoords)
							roominess[mpos] = Math.Min(
								param.MaximumCutoutSpacing,
								roominess[mpos]);

						while (true)
						{
							var (chosenMPos, room) = CellLayerUtils.FindRandomBest(
								roominess,
								topologyRandom,
								(a, b) => a.CompareTo(b));
							if (room < param.MaximumCutoutSpacing)
								break;

							var projections = Symmetry.RotateAndMirrorCPos(
								chosenMPos.ToCPos(map),
								space,
								param.Rotations,
								param.Mirror);
							foreach (var projection in projections)
							{
								if (space.Contains(projection))
									space[projection] = false;
								var minX = projection.X - 2 * param.MaximumCutoutSpacing + 1;
								var minY = projection.Y - 2 * param.MaximumCutoutSpacing + 1;
								var maxX = projection.X + 2 * param.MaximumCutoutSpacing - 1;
								var maxY = projection.Y + 2 * param.MaximumCutoutSpacing - 1;
								for (var y = minY; y <= maxY; y++)
									for (var x = minX; x <= maxX; x++)
									{
										var mpos = new CPos(x, y).ToMPos(map);
										if (roominess.Contains(mpos))
											roominess[mpos] = 0;
									}
							}
						}
					}

					var matrixSpace = CellLayerUtils.ToMatrix(space, false);

					// deflated is grid points, not squares. Has a size of `size + 1`.
					var deflated = MatrixUtils.DeflateSpace(matrixSpace, false);
					var kernel = new Matrix<bool>(2 * param.ForestCutout, 2 * param.ForestCutout).Fill(true);
					var inflated = MatrixUtils.KernelDilateOrErode(deflated.Map(v => v != 0), kernel, new int2(param.ForestCutout - 1, param.ForestCutout - 1), true);
					var cutout = new CellLayer<bool>(map);
					CellLayerUtils.FromMatrix(cutout, inflated, true);
					foreach (var mpos in map.AllCells.MapCoords)
						if (cutout[mpos])
							forestPlan[mpos] = false;
				}

				var replaceable = IdentifyReplaceableTiles(map, replaceabilityMap, null);
				var forestReplace = new CellLayer<MultiBrush.Replaceability>(map);
				foreach (var mpos in map.AllCells.MapCoords)
					if (forestPlan[mpos])
						forestReplace[mpos] = replaceable[mpos];
					else
						forestReplace[mpos] = MultiBrush.Replaceability.None;
				MultiBrush.PaintArea(map, actorPlans, forestReplace, param.ForestObstacles, forestTilingRandom);
			}

			if (param.EnforceSymmetry != 0)
			{
				// This is not commutative.
				bool CheckCompatibility(byte main, byte other)
				{
					if (main == other)
						return true;
					else if (param.DominantTerrain.Contains(main))
						return true;
					else if (param.DominantTerrain.Contains(other))
						return false;
					else
						return param.EnforceSymmetry < 2;
				}

				var replace = new CellLayer<MultiBrush.Replaceability>(map);
				Symmetry.RotateAndMirrorOverCPos(
					replace,
					param.Rotations,
					param.Mirror,
					(CPos[] sources, CPos destination) =>
					{
						var main = tileset.GetTerrainIndex(map.Tiles[destination]);
						var compatible = sources
							.Where(replace.Contains)
							.Select(source => tileset.GetTerrainIndex(map.Tiles[source]))
							.All(source => CheckCompatibility(main, source));
						replace[destination] = compatible ? MultiBrush.Replaceability.None : MultiBrush.Replaceability.Actor;
					});
				MultiBrush.PaintArea(map, actorPlans, replace, param.ForestObstacles, symmetryTilingRandom);
			}

			var playableArea = new CellLayer<bool>(map);
			{
				var (regions, regionMask, playability) = PlayableSpace.FindPlayableRegions(map, actorPlans, playabilityMap);
				PlayableSpace.Region largest = null;
				var disqualifications = new HashSet<int>();

				// For circle-in-mountains, the outside is unplayable and should never count as
				// the largest/preferred region.
				if (param.ExternalCircularBias > 0)
				{
					if (map.Grid.Type != MapGridType.Rectangular)
						throw new NotImplementedException();
					CellLayerUtils.OverCircle(
						cellLayer: regionMask,
						wCenter: wMapCenter,
						wRadius: (minSpan - 2) * 512,
						outside: true,
						action: (mpos, _, _, _) =>
							{
								if (regionMask[mpos] != PlayableSpace.NullRegion)
									disqualifications.Add(regionMask[mpos]);
							});
				}

				// Disqualify regions that violate any symmetry requirements.
				{
					var symmetryScore = new int[regions.Length];
					void TestSymmetry(CPos[] sources, CPos destination)
					{
						var id = regionMask[destination];
						if (playability[destination] != PlayableSpace.Playability.Playable)
							return;
						if (sources.All(source => regionMask.TryGetValue(source, out var sourceId) && sourceId == id))
							symmetryScore[id]++;
					}

					Symmetry.RotateAndMirrorOverCPos(
						regionMask,
						param.Rotations,
						param.Mirror,
						TestSymmetry);

					for (var id = 0; id < symmetryScore.Length; id++)
						if (symmetryScore[id] < regions[id].PlayableArea / 2)
							disqualifications.Add(id);
				}

				foreach (var region in regions)
				{
					if (disqualifications.Contains(region.Id))
						continue;
					if (largest == null || region.PlayableArea > largest.PlayableArea)
						largest = region;
				}

				if (largest == null)
					throw new MapGenerationException("could not find a playable region");

				var minimumPlayableSpace = (int)(param.TotalPlayers * Math.PI * param.SpawnBuildSize * param.SpawnBuildSize);
				if (largest.PlayableArea < minimumPlayableSpace)
					throw new MapGenerationException("playable space is too small");

				bool? AdoptPartiallyPlayableIntoLargest(CPos cpos, bool first)
				{
					if (first)
						return false;
					else if (regionMask[cpos] == largest.Id || playability[cpos] != PlayableSpace.Playability.Partial)
						return null;

					regionMask[cpos] = largest.Id;
					largest.Area++;
					return false;
				}

				// Adopt any partially playable tiles connected to the largest region into the largest region.
				// This avoids potentially debris-filling connected oceans for mods where water is unplayable.
				CellLayerUtils.FloodFill(
					regionMask,
					map.AllCells.Where(cpos => regionMask[cpos] == largest.Id).Select(cpos => (cpos, true)),
					AdoptPartiallyPlayableIntoLargest,
					Direction.Spread4CVec);

				if (param.DenyWalledAreas)
				{
					// Beach tiles are particularly problematic. If they're for unplayable bodies
					// of water, they should be obliterated. If they're just surrounded by rocks,
					// trees, etc, they should be filled in with actors.
					{
						var unplayableWater = new HashSet<CPos>();
						foreach (var mpos in map.AllCells.MapCoords)
							if (map.Contains(mpos) &&
								map.Tiles[mpos].Type == param.WaterTile &&
								regionMask[mpos] != largest.Id)
							{
								var cpos = mpos.ToCPos(gridType);
								var projections = Symmetry.RotateAndMirrorCPos(
									cpos, map.Tiles, param.Rotations, param.Mirror);
								foreach (var projection in projections)
									if (map.Tiles.Contains(projection) && map.Tiles[projection].Type == param.WaterTile)
										unplayableWater.Add(projection);
							}

						bool? ClearWaterBody(CPos cpos, bool _)
						{
							var mpos = cpos.ToMPos(gridType);
							var propagate =
								map.Tiles[mpos].Type == param.WaterTile ||
								beachTiles.Contains(map.Tiles[mpos]);
							map.Tiles[mpos] = PickTile(param.LandTile);
							regionMask[mpos] = PlayableSpace.NullRegion;
							return propagate ? false : null;
						}

						CellLayerUtils.FloodFill(
							map.Tiles,
							unplayableWater.Select(cpos => (cpos, false)),
							ClearWaterBody,
							Direction.Spread4CVec);
					}

					var replaceable = IdentifyReplaceableTiles(map, replaceabilityMap, actorPlans);
					var replace = new CellLayer<MultiBrush.Replaceability>(map);
					foreach (var mpos in map.AllCells.MapCoords)
						if (regionMask[mpos] == largest.Id || !map.Contains(mpos))
							replace[mpos] = MultiBrush.Replaceability.None;
						else
							replace[mpos] = replaceable[mpos];

					MultiBrush.PaintArea(map, actorPlans, replace, param.UnplayableObstacles, debrisTilingRandom);
				}

				foreach (var mpos in map.AllCells.MapCoords)
					playableArea[mpos] = playability[mpos] == PlayableSpace.Playability.Playable && regionMask[mpos] == largest.Id;
			}

			if (param.Roads)
			{
				var space = new CellLayer<bool>(map);
				foreach (var mpos in map.AllCells.MapCoords)
					space[mpos] = param.ClearTerrain.Contains(tileset.GetTerrainIndex(map.Tiles[mpos]));

				foreach (var actorPlan in actorPlans)
					foreach (var (cpos, _) in actorPlan.Footprint())
						if (space.Contains(cpos))
							space[cpos] = false;

				// Improve symmetry.
				{
					var newSpace = new CellLayer<bool>(map);
					Symmetry.RotateAndMirrorOverCPos(
						space,
						param.Rotations,
						param.Mirror,
						(sources, destination)
							=> newSpace[destination] =
								sources.All(source => !space.TryGetValue(source, out var value) || value));
					space = newSpace;
				}

				// TODO: Move to configuration
				const int RoadStraightenShrink = 4;
				const int RoadStraightenGrow = 2;
				const int RoadMinimumShrinkLength = 12;
				const int RoadInertialRange = 8;
				var roadTotalShrink = RoadStraightenShrink + param.RoadShrink;

				var matrixSpace = CellLayerUtils.ToMatrix(space, true);
				var kernel = new Matrix<bool>(param.RoadSpacing * 2 + 1, param.RoadSpacing * 2 + 1);
				MatrixUtils.OverCircle(
					matrix: kernel,
					centerIn1024ths: kernel.Size * 512,
					radiusIn1024ths: param.RoadSpacing * 1024,
					outside: false,
					action: (xy, _) => kernel[xy] = true);
				var dilated = MatrixUtils.KernelDilateOrErode(
					matrixSpace,
					kernel,
					new int2(param.RoadSpacing, param.RoadSpacing),
					false);
				var deflated = MatrixUtils.DeflateSpace(dilated, true);
				var matrixPointArrays = MatrixUtils.DirectionMapToPathsWithPruning(
					input: deflated,
					minimumLength: 20 + 2 * param.RoadShrink,
					minimumJunctionSeparation: 6,
					preserveEdgePaths: true);
				var pointArrays = CellLayerUtils.FromMatrixPoints(matrixPointArrays, space);
				pointArrays = TilingPath.RetainDisjointPaths(pointArrays);

				var nonLoopedRoadPermittedTemplates =
					TilingPath.PermittedSegments.FromInnerAndTerminalTypes(
						tileset, param.RoadSegmentTypes, param.ClearSegmentTypes);
				var loopedRoadPermittedTemplates =
					TilingPath.PermittedSegments.FromType(
						tileset, param.RoadSegmentTypes);

				foreach (var pointArray in pointArrays)
				{
					var isLoop = pointArray[0] == pointArray[^1];
					TilingPath path;
					if (isLoop)
						path = new TilingPath(
							map,
							pointArray,
							param.RoadSpacing - 1,
							param.RoadSegmentTypes[0],
							param.RoadSegmentTypes[0],
							loopedRoadPermittedTemplates);
					else
						path = new TilingPath(
							map,
							pointArray,
							param.RoadSpacing - 1,
							param.ClearSegmentTypes[0],
							param.ClearSegmentTypes[0],
							nonLoopedRoadPermittedTemplates);

					path
						.ChirallyNormalize(cvec => CellLayerUtils.CornerToWPos(cvec, gridType) - wMapCenter)
						.ExtendEdge(2 * roadTotalShrink + RoadMinimumShrinkLength)
						.Shrink(roadTotalShrink, RoadMinimumShrinkLength)
						.InertiallyExtend(RoadStraightenGrow, RoadInertialRange)
						.OptimizeLoop()
						.RetainIfValid();

					// Shrinking may have deleted the path.
					if (path.Points == null)
						continue;

					// Roads that are _almost_ vertical or horizontal tile badly. Filter them out.
					var minX = path.Points.Min((p) => p.X);
					var minY = path.Points.Min((p) => p.Y);
					var maxX = path.Points.Max((p) => p.X);
					var maxY = path.Points.Max((p) => p.Y);
					if (maxX - minX < 6 || maxY - minY < 6)
						continue;

					if (path.Tile(roadTilingRandom) == null)
						throw new MapGenerationException("Could not fit tiles for roads");
				}
			}

			if (param.CreateEntities)
			{
				var (buildingTypes, buildingWeights) = Parameters.SplitWeights(param.BuildingWeights);
				var (resourceSpawnTypes, resourceSpawnWeights) = Parameters.SplitWeights(param.ResourceSpawnWeights);

				var projectionSpacing = new CellLayer<int>(map);
				Symmetry.RotateAndMirrorOverCPos(
					projectionSpacing,
					param.Rotations,
					param.Mirror,
					(projections, cpos) =>
						projectionSpacing[cpos] = Symmetry.ProjectionProximity(projections) / 2);

				// Spawn bias tries to move spawns away from the map center and their symmetry
				// projections.
				var spawnBiasRadius = Math.Max(1, minSpan * param.CentralSpawnReservationFraction / FractionMax);
				var spawnBias = new CellLayer<int>(map);
				spawnBias.Clear(spawnBiasRadius);
				CellLayerUtils.OverCircle(
					cellLayer: spawnBias,
					wCenter: wMapCenter,
					wRadius: 1024 * spawnBiasRadius,
					outside: false,
					action: (mpos, _, _, wrSq) => spawnBias[mpos] = (int)Exts.ISqrt(wrSq) / 1024);
				foreach (var mpos in map.AllCells.MapCoords)
					spawnBias[mpos] = Math.Min(spawnBias[mpos], projectionSpacing[mpos]);

				var zoneable = new CellLayer<bool>(map);
				foreach (var mpos in map.AllCells.MapCoords)
					zoneable[mpos] = playableArea[mpos] && param.ZoneableTerrain.Contains(tileset.GetTerrainIndex(map.Tiles[mpos]));

				foreach (var actorPlan in actorPlans)
					foreach (var cpos in actorPlan.Footprint().Keys)
						if (map.AllCells.Contains(cpos))
							zoneable[cpos] = false;

				// Improve symmetry.
				{
					var newZoneable = new CellLayer<bool>(map);
					Symmetry.RotateAndMirrorOverCPos(
						zoneable,
						param.Rotations,
						param.Mirror,
						(sources, destination)
							=> newZoneable[destination] =
								sources.All(source => zoneable.TryGetValue(source, out var value) && value));
					zoneable = newZoneable;
				}

				if (param.Rotations > 1 || param.Mirror != Symmetry.Mirror.None)
				{
					// Reserve the center of the map - otherwise it will mess with rotations
					CellLayerUtils.OverCircle(
						cellLayer: zoneable,
						wCenter: wMapCenter,
						wRadius: 1024,
						outside: false,
						action: (mpos, _, _, _) => zoneable[mpos] = false);
				}

				var zoneableArea = zoneable.Count(v => v);
				var entityMultiplier =
					(long)zoneableArea * param.AreaEntityBonus +
					(long)param.TotalPlayers * param.PlayerCountEntityBonus;
				var perSymmetryEntityMultiplier = entityMultiplier / param.SymmetryCount;

				// Spawn generation
				for (var iteration = 0; iteration < param.Players; iteration++)
				{
					var spawnPreference = new CellLayer<int>(map);
					CellLayerUtils.ChebyshevRoom(spawnPreference, zoneable, false);
					foreach (var mpos in map.AllCells.MapCoords)
						if (spawnPreference[mpos] >= param.MinimumSpawnRadius &&
							projectionSpacing[mpos] * 2 >= param.SpawnReservation + param.MinimumSpawnRadius)
						{
							spawnPreference[mpos] = spawnBias[mpos] * Math.Min(param.SpawnRegionSize, spawnPreference[mpos]);
						}
						else
						{
							spawnPreference[mpos] = 0;
						}

					var (chosenMPos, chosenValue) = CellLayerUtils.FindRandomBest(
						spawnPreference,
						playerRandom,
						(a, b) => a.CompareTo(b));

					if (chosenValue < 1)
						throw new MapGenerationException("Not enough room for player spawns");

					var spawn = new ActorPlan(map, "mpspawn")
					{
						Location = chosenMPos.ToCPos(gridType),
					};

					var preferedRange1024ths = (param.SpawnBuildSize + param.SpawnRegionSize * 2) * 512;
					var resourceSpawnPreferences = new CellLayer<int>(map);
					CellLayerUtils.WalkingDistances(
						resourceSpawnPreferences,
						zoneable,
						new[] { chosenMPos.ToCPos(gridType) },
						param.SpawnRegionSize * 1024);
					foreach (var mpos in map.AllCells.MapCoords)
					{
						var v = resourceSpawnPreferences[mpos];
						resourceSpawnPreferences[mpos] =
							((v > preferedRange1024ths ? 2 * preferedRange1024ths - v : v) + 1023) / 1024;
					}

					var resourceSpawns = new List<ActorPlan>();
					for (var resourceSpawn = 0; resourceSpawn < param.SpawnResourceSpawns; resourceSpawn++)
					{
						var (mpos, value) = CellLayerUtils.FindRandomBest(
							resourceSpawnPreferences,
							playerRandom,
							(a, b) => a.CompareTo(b));
						if (value <= 1)
							break;

						var resourceSpawnType = resourceSpawnTypes[playerRandom.PickWeighted(resourceSpawnWeights)];
						var resourceSpawnPlan =
							new ActorPlan(map, resourceSpawnType)
							{
								Location = mpos.ToCPos(gridType)
							};
						resourceSpawns.Add(resourceSpawnPlan);
						CellLayerUtils.OverCircle(
							cellLayer: resourceSpawnPreferences,
							wCenter: resourceSpawnPlan.WPosLocation,
							wRadius: 1024,
							outside: false,
							action: (mpos, _, _, _) => resourceSpawnPreferences[mpos] = 0);
					}

					var projectedSpawns = Symmetry.RotateAndMirrorActorPlan(spawn, param.Rotations, param.Mirror);
					actorPlans.AddRange(projectedSpawns);
					foreach (var projectedSpawn in projectedSpawns)
						CellLayerUtils.OverCircle(
							cellLayer: zoneable,
							wCenter: projectedSpawn.WPosLocation,
							wRadius: param.SpawnReservation * 1024,
							outside: false,
							action: (mpos, _, _, _) => zoneable[mpos] = false);

					var projectedResourceSpawns = Symmetry.RotateAndMirrorActorPlans(resourceSpawns, param.Rotations, param.Mirror);
					actorPlans.AddRange(projectedResourceSpawns);
					foreach (var projectedResourceSpawn in projectedResourceSpawns)
						CellLayerUtils.OverCircle(
							cellLayer: zoneable,
							wCenter: projectedResourceSpawn.WPosLocation,
							wRadius: param.ResourceSpawnReservation * 1024,
							outside: false,
							action: (mpos, _, _, _) => zoneable[mpos] = false);
				}

				// Expansions
				{
					var resourceSpawnsRemaining = (int)(param.MaximumExpansionResourceSpawns * perSymmetryEntityMultiplier / EntityBonusMax);
					while (resourceSpawnsRemaining > 0)
					{
						var roominess = new CellLayer<int>(map);
						CellLayerUtils.ChebyshevRoom(roominess, zoneable, false);
						foreach (var mpos in map.AllCells.MapCoords)
							roominess[mpos] = Math.Min(
								param.MaximumExpansionSize + param.ExpansionBorder,
								Math.Min(roominess[mpos], projectionSpacing[mpos]));
						var (chosenMPos, chosenValue) = CellLayerUtils.FindRandomBest(
							roominess,
							expansionRandom,
							(a, b) => a.CompareTo(b));
						var room = chosenValue - 1;
						var radius2 = room - param.ExpansionBorder;
						if (radius2 < param.MinimumExpansionSize)
							break;
						if (radius2 > param.MaximumExpansionSize)
							radius2 = param.MaximumExpansionSize;
						var radius1 = Math.Min(Math.Min(param.ExpansionInner, room), radius2);
						var resourceSpawnCount = Math.Min(resourceSpawnsRemaining, expansionRandom.Next(param.MaximumResourceSpawnsPerExpansion) + 1);
						resourceSpawnsRemaining -= resourceSpawnCount;

						if (radius1 < 1)
							break;

						var resourceSpawns = new List<ActorPlan>();
						var resourceSpawnPreferences = new CellLayer<int>(map);
						var radius1Sq = radius1 * radius1;
						CellLayerUtils.OverCircle(
							cellLayer: resourceSpawnPreferences,
							wCenter: CellLayerUtils.MPosToWPos(chosenMPos, gridType),
							wRadius: radius2 * 1024,
							outside: false,
							action: (mpos, _, _, wrSq) =>
							{
								var rSq = (int)(wrSq / (1024 * 1024));
								resourceSpawnPreferences[mpos] =
									rSq >= radius1Sq ? rSq : 0;
							});
						for (var resourceSpawn = 0; resourceSpawn < resourceSpawnCount; resourceSpawn++)
						{
							var mpos = CellLayerUtils.PickWeighted(resourceSpawnPreferences, expansionRandom);
							var resourceSpawnType = resourceSpawnTypes[playerRandom.PickWeighted(resourceSpawnWeights)];
							var resourceSpawnPlan =
								new ActorPlan(map, resourceSpawnType)
								{
									Location = mpos.ToCPos(gridType)
								};
							resourceSpawns.Add(resourceSpawnPlan);
							CellLayerUtils.OverCircle(
								cellLayer: resourceSpawnPreferences,
								wCenter: resourceSpawnPlan.WPosLocation,
								wRadius: 1024,
								outside: false,
								action: (mpos, _, _, _) => resourceSpawnPreferences[mpos] = 0);
						}

						var projectedResourceSpawns = Symmetry.RotateAndMirrorActorPlans(resourceSpawns, param.Rotations, param.Mirror);
						actorPlans.AddRange(projectedResourceSpawns);
						foreach (var projectedResourceSpawn in projectedResourceSpawns)
							CellLayerUtils.OverCircle(
								cellLayer: zoneable,
								wCenter: projectedResourceSpawn.WPosLocation,
								wRadius: param.ResourceSpawnReservation * 1024,
								outside: false,
								action: (mpos, _, _, _) => zoneable[mpos] = false);
					}
				}

				// Neutral buildings
				{
					var targetBuildingCount =
						(param.MaximumBuildings != 0)
							? expansionRandom.Next(
								(int)(param.MinimumBuildings * perSymmetryEntityMultiplier / EntityBonusMax),
								(int)(param.MaximumBuildings * perSymmetryEntityMultiplier / EntityBonusMax) + 1)
							: 0;
					for (var i = 0; i < targetBuildingCount; i++)
					{
						var roominess = new CellLayer<int>(map);
						CellLayerUtils.ChebyshevRoom(roominess, zoneable, false);
						foreach (var mpos in map.AllCells.MapCoords)
							roominess[mpos] = Math.Min(
								3,
								Math.Min(roominess[mpos], projectionSpacing[mpos]));
						var (chosenMPos, chosenValue) = CellLayerUtils.FindRandomBest(
							roominess,
							buildingRandom,
							(a, b) => a.CompareTo(b));
						if (chosenValue < 3)
							break;
						var chosenCPos = chosenMPos.ToCPos(gridType);
						var typeChoice = buildingRandom.PickWeighted(buildingWeights);
						var type = buildingTypes[typeChoice];
						var actorPlan = new ActorPlan(map, type)
						{
							WPosCenterLocation = CellLayerUtils.CPosToWPos(chosenCPos, gridType),
						};

						var projectedBuildings = Symmetry.RotateAndMirrorActorPlan(actorPlan, param.Rotations, param.Mirror);
						actorPlans.AddRange(projectedBuildings);
						foreach (var projectedBuilding in projectedBuildings)
							CellLayerUtils.OverCircle(
								cellLayer: zoneable,
								wCenter: projectedBuilding.WPosLocation,
								wRadius: 2048,
								outside: false,
								action: (mpos, _, _, _) => zoneable[mpos] = false);
					}
				}

				// Grow resources
				{
					var pattern1024ths = new CellLayer<int>(map);
					NoiseUtils.SymmetricFractalNoiseIntoCellLayer(
						resourceRandom,
						pattern1024ths,
						param.Rotations,
						param.Mirror,
						param.ResourceFeatureSize,
						wavelength => ClumpinessAmplitude(wavelength, param.OreClumpiness));
					{
						CellLayerUtils.CalibrateQuantileInPlace(
							pattern1024ths,
							0,
							0, 1);
						var max1024ths = pattern1024ths.Max();
						var uniformity1024ths = param.OreUniformity * 1024 / FractionMax;
						foreach (var mpos in map.AllCells.MapCoords)
							pattern1024ths[mpos] = uniformity1024ths + 1024 * pattern1024ths[mpos] / max1024ths;
					}

					var strengths1024ths = new Dictionary<ResourceTypeInfo, CellLayer<int>>();
					foreach (var actorPlan in actorPlans)
					{
						var type = actorPlan.Reference.Type;
						if (param.ResourceSpawnWeights.ContainsKey(type))
						{
							var resource = param.ResourceSpawnSeeds[type];
							if (!strengths1024ths.TryGetValue(resource, out var strength1024ths))
							{
								strength1024ths = new CellLayer<int>(map);
								strengths1024ths.Add(resource, strength1024ths);
							}

							CellLayerUtils.OverCircle(
								cellLayer: strength1024ths,
								wCenter: actorPlan.WPosLocation,
								wRadius: 16 * 1024,
								outside: false,
								action: (mpos, _, _, wrSq) =>
									strength1024ths[mpos] +=
										(int)(1024 * 1024 / (1024 + Exts.ISqrt(wrSq))));
						}
					}

					var maxStrength1024ths = new CellLayer<int>(map);
					var bestResource = new CellLayer<ResourceTypeInfo>(map);
					bestResource.Clear(param.DefaultResource);
					foreach (var resourceStrength in strengths1024ths)
					{
						var resource = resourceStrength.Key;
						var strength1024ths = resourceStrength.Value;
						foreach (var mpos in map.AllCells.MapCoords)
							if (strength1024ths[mpos] > maxStrength1024ths[mpos])
							{
								maxStrength1024ths[mpos] = strength1024ths[mpos];
								bestResource[mpos] = resource;
							}
					}

					// Closer to +inf means "more preferable" for plan.
					var plan1024ths = new CellLayer<int>(map);
					foreach (var mpos in map.AllCells.MapCoords)
						plan1024ths[mpos] = pattern1024ths[mpos] * maxStrength1024ths[mpos] / 1024;

					var wSpawnBuildSizeSq = (long)param.SpawnBuildSize * param.SpawnBuildSize * 1024 * 1024;
					foreach (var actorPlan in actorPlans)
						if (actorPlan.Reference.Type == "mpspawn")
							CellLayerUtils.OverCircle(
								cellLayer: plan1024ths,
								wCenter: actorPlan.WPosLocation,
								wRadius: param.SpawnRegionSize * 2 * 1024,
								outside: false,
								action: (mpos, _, _, rSq) =>
									plan1024ths[mpos] += (int)(plan1024ths[mpos] * param.SpawnResourceBias * wSpawnBuildSizeSq / Math.Max(rSq, 1024 * 1024) / FractionMax));

					foreach (var mpos in map.AllCells.MapCoords)
						if (!playableArea[mpos] || !param.AllowedTerrainResourceCombos.Contains((bestResource[mpos], map.GetTerrainIndex(mpos))))
							plan1024ths[mpos] = -int.MaxValue;

					foreach (var actorPlan in actorPlans)
						if (actorPlan.Reference.Type == "mpspawn")
							CellLayerUtils.OverCircle(
								cellLayer: plan1024ths,
								wCenter: actorPlan.WPosLocation,
								wRadius: param.SpawnBuildSize * 1024,
								outside: false,
								action: (mpos, _, _, _) => plan1024ths[mpos] = -int.MaxValue);

					foreach (var actorPlan in actorPlans)
						foreach (var (cpos, _) in actorPlan.Footprint())
							if (plan1024ths.Contains(cpos))
								plan1024ths[cpos] = -int.MaxValue;

					// Improve symmetry.
					{
						var newPlan = new CellLayer<int>(map);
						Symmetry.RotateAndMirrorOverCPos(
							plan1024ths,
							param.Rotations,
							param.Mirror,
							(sources, destination)
								=> newPlan[destination] =
									sources.Min(source => plan1024ths.TryGetValue(source, out var value) ? value : -int.MaxValue));
						plan1024ths = newPlan;
					}

					var remaining = param.ResourcesPerPlayer * entityMultiplier / EntityBonusMax;

					// Closer to -inf means "more preferable" for priorities.
					var priorities = new PriorityArray<int>(
						plan1024ths.Size.Width * plan1024ths.Size.Height,
						int.MaxValue);
					{
						var i = 0;
						foreach (var v in plan1024ths)
							priorities[i++] = -v;
					}

					int PriorityIndex(MPos mpos) => mpos.V * plan1024ths.Size.Width + mpos.U;
					MPos PriorityMPos(int index)
					{
						var v = Math.DivRem(index, plan1024ths.Size.Width, out var u);
						return new MPos(u, v);
					}

					map.Resources.Clear();

					// Return resource value of a given square.
					// See https://github.com/OpenRA/OpenRA/blob/9302bac6199fbc925a85fd7a08fc2ba4b9317d16/OpenRA.Mods.Common/Traits/World/ResourceLayer.cs#L144-L166
					// https://github.com/OpenRA/OpenRA/blob/9302bac6199fbc925a85fd7a08fc2ba4b9317d16/OpenRA.Mods.Common/Traits/World/EditorResourceLayer.cs#L175-L183
					int CheckValue(CPos cpos)
					{
						if (!map.Resources.Contains(cpos))
							return 0;
						var resource = map.Resources[cpos].Type;
						if (resource == 0)
							return 0;
						var adjacent = 0;
						for (var y = -1; y <= 1; y++)
							for (var x = -1; x <= 1; x++)
							{
								var offsetCpos = cpos + new CVec(x, y);
								if (!map.Resources.Contains(offsetCpos))
									continue;
								if (map.Resources[offsetCpos].Type == resource)
									adjacent++;
							}

						var resourceType = bestResource[cpos];
						var density = Math.Max(resourceType.MaxDensity * adjacent / /*maxAdjacent=*/9, 1);

						// density + 1 to mirror a bug that got ossified due to balancing.
						return param.ResourceValues[resourceType] * (density + 1);
					}

					int CheckValue3By3(CPos cpos)
					{
						var total = 0;
						for (var y = -1; y <= 1; y++)
							for (var x = -1; x <= 1; x++)
								total += CheckValue(cpos + new CVec(x, y));

						return total;
					}

					// Set and return change in overall value.
					int AddResource(CPos cpos)
					{
						var mpos = cpos.ToMPos(gridType);
						priorities[PriorityIndex(mpos)] = int.MaxValue;
						zoneable[mpos] = false;

						// Generally shouldn't happen, but perhaps a rotation/mirror related inaccuracy.
						if (map.Resources[mpos].Type != 0)
							return 0;

						var resourceType = bestResource[mpos];
						var oldValue = CheckValue3By3(cpos);
						map.Resources[mpos] = new ResourceTile(
							resourceType.ResourceIndex,
							(byte)resourceType.MaxDensity);
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
						foreach (var cpos in Symmetry.RotateAndMirrorCPos(chosenCPos, plan1024ths, param.Rotations, param.Mirror))
							if (map.Resources.Contains(cpos))
								remaining -= AddResource(cpos);
					}
				}

				// CivilianBuildings
				if (param.CivilianBuildings > 0)
				{
					var space = new CellLayer<bool>(map);
					foreach (var mpos in map.AllCells.MapCoords)
						space[mpos] = param.PlayableTerrain.Contains(tileset.GetTerrainIndex(map.Tiles[mpos]));

					foreach (var actorPlan in actorPlans)
						foreach (var (cpos, _) in actorPlan.Footprint())
							if (space.Contains(cpos))
								space[cpos] = false;

					var matrixSpace = CellLayerUtils.ToMatrix(space, true);
					var deflated = MatrixUtils.DeflateSpace(matrixSpace, false);
					var kernel = new Matrix<bool>(2, 2).Fill(true);
					var reservedMatrix = MatrixUtils.KernelDilateOrErode(deflated.Map(v => v != 0), kernel, new int2(0, 0), true);
					var reserved = new CellLayer<bool>(map);
					CellLayerUtils.FromMatrix(reserved, reservedMatrix, true);

					var decorationNoise = new CellLayer<int>(map);
					NoiseUtils.SymmetricFractalNoiseIntoCellLayer(
						decorationRandom,
						decorationNoise,
						param.Rotations,
						param.Mirror,
						param.CivilianBuildingsFeatureSize,
						wavelength => 1);

					var densityNoise = new CellLayer<int>(map);
					NoiseUtils.SymmetricFractalNoiseIntoCellLayer(
						decorationRandom,
						densityNoise,
						param.Rotations,
						param.Mirror,
						1024,
						NoiseUtils.PinkAmplitude);
					CellLayerUtils.CalibrateQuantileInPlace(
						densityNoise,
						0,
						FractionMax - param.CivilianBuildingDensity, FractionMax);

					var decorable = new CellLayer<bool>(map);
					var totalDecorable = 0;
					foreach (var mpos in map.AllCells.MapCoords)
					{
						var isDecorable =
							map.Tiles[mpos].Type == param.LandTile
								&& zoneable[mpos] && space[mpos] && !reserved[mpos] && densityNoise[mpos] >= 0;
						decorable[mpos] = isDecorable;
						if (isDecorable)
							totalDecorable++;
						else
							decorationNoise[mpos] = -1024 * 1024;
					}

					var mapArea = map.MapSize.X * map.MapSize.Y;
					CellLayerUtils.CalibrateQuantileInPlace(
						decorationNoise,
						0,
						mapArea - totalDecorable * param.CivilianBuildings / FractionMax, mapArea);
					foreach (var mpos in map.AllCells.MapCoords)
						if (decorationNoise[mpos] < 0)
							decorable[mpos] = false;

					for (var i = 0; i < 8; i++)
					{
						var (blurred, changes) = MatrixUtils.BooleanBlur(
							CellLayerUtils.ToMatrix(decorable, false),
							param.CivilianBuildingDensityRadius,
							FractionMax - param.MinimumCivilianBuildingDensity, FractionMax);
						if (changes == 0)
							break;

						var densityFilter = new CellLayer<bool>(map);
						CellLayerUtils.FromMatrix(densityFilter, blurred);

						foreach (var mpos in map.AllCells.MapCoords)
							if (!densityFilter[mpos])
								decorable[mpos] = false;
					}

					// Improve symmetry.
					{
						var newDecorable = new CellLayer<bool>(map);
						Symmetry.RotateAndMirrorOverCPos(
							decorable,
							param.Rotations,
							param.Mirror,
							(sources, destination)
								=> newDecorable[destination] =
									sources.All(source => decorable.TryGetValue(source, out var value) && value));
						decorable = newDecorable;
					}

					var replace = new CellLayer<MultiBrush.Replaceability>(map);
					foreach (var mpos in map.AllCells.MapCoords)
						replace[mpos] = decorable[mpos] ? MultiBrush.Replaceability.Actor : MultiBrush.Replaceability.None;

					MultiBrush.PaintArea(
						map,
						actorPlans,
						replace,
						param.CivilianBuildingsObstacles,
						decorationTilingRandom,
						alwaysPreferLargerBrushes: true);
				}
			}

			// Cosmetically repaint tiles
			foreach (var (tile, collection) in param.RepaintTiles.OrderBy(kv => kv.Key))
			{
				var replace = new CellLayer<MultiBrush.Replaceability>(map);
				foreach (var mpos in replace.CellRegion.MapCoords)
					replace[mpos] =
						map.Tiles[mpos].Type == tile
							? MultiBrush.Replaceability.Any
							: MultiBrush.Replaceability.None;

				MultiBrush.PaintArea(map, actorPlans, replace, collection, repaintRandom);
			}

			map.PlayerDefinitions = new MapPlayers(map.Rules, 0).ToMiniYaml();
			map.ActorDefinitions = actorPlans
				.Select((plan, i) => new MiniYamlNode($"Actor{i}", plan.Reference.Save()))
				.ToImmutableArray();
		}

		static CellLayer<MultiBrush.Replaceability> IdentifyReplaceableTiles(
			Map map,
			Dictionary<TerrainTile, MultiBrush.Replaceability> replaceabilityMap,
			IEnumerable<ActorPlan> actorPlans)
		{
			var output = new CellLayer<MultiBrush.Replaceability>(map);

			foreach (var mpos in map.AllCells.MapCoords)
			{
				var tile = map.Tiles[mpos];
				var replaceability = MultiBrush.Replaceability.Any;
				if (replaceabilityMap.TryGetValue(tile, out var value))
					replaceability = value;
				output[mpos] = replaceability;
			}

			if (actorPlans != null)
				foreach (var actorPlan in actorPlans)
					foreach (var cpos in actorPlan.Footprint().Keys)
						if (map.AllCells.Contains(cpos))
							output[cpos] = MultiBrush.Replaceability.None;

			return output;
		}

		static int ClumpinessAmplitude(int wavelength, int clumpiness)
		{
			var amplitude = wavelength;
			for (var i = 0; i < clumpiness; i++)
				amplitude = Exts.ISqrt(amplitude);
			return amplitude;
		}
	}

	public class ExperimentalMapGenerator { /* we're only interested in the Info */ }
}
