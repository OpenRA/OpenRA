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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Support;
using OpenRA.Traits;
using static OpenRA.Mods.Common.Traits.ResourceLayerInfo;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	public sealed class ExperimentalMapGeneratorInfo : TraitInfo, IEditorMapGeneratorInfo
	{
		[FieldLoader.Require]
		public readonly string Type = null;

		[FieldLoader.Require]
		[FluentReference]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Tilesets that are compatible with this map generator.")]
		public readonly ImmutableArray<string> Tilesets = default;

		[FluentReference]
		[Desc("The title to use for generated maps.")]
		public readonly string MapTitle = "label-random-map";

		[Desc("The widget tree to open when the tool is selected.")]
		public readonly string PanelWidget = "MAP_GENERATOR_TOOL_PANEL";

		// This is purely of interest to the linter.
		[FieldLoader.LoadUsing(nameof(FluentReferencesLoader))]
		[FluentReference]
		public readonly ImmutableArray<string> FluentReferences = default;

		[FieldLoader.LoadUsing(nameof(SettingsLoader))]
		public readonly MiniYaml Settings;

		string IMapGeneratorInfo.Type => Type;
		string IMapGeneratorInfo.Name => Name;
		string IMapGeneratorInfo.MapTitle => MapTitle;
		ImmutableArray<string> IEditorMapGeneratorInfo.Tilesets => Tilesets;

		static MiniYaml SettingsLoader(MiniYaml my)
		{
			return my.NodeWithKey("Settings").Value;
		}

		static object FluentReferencesLoader(MiniYaml my)
		{
			return new MapGeneratorSettings(null, my.NodeWithKey("Settings").Value)
				.Options.SelectMany(o => o.GetFluentReferences()).ToImmutableArray();
		}

		const int FractionMax = Terraformer.FractionMax;
		const int EntityBonusMax = 1000000;

		sealed class Parameters
		{
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
			public readonly int MinimumCoastStraight = -1;
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
			public readonly int WaterRoughness = 0;
			[FieldLoader.Require]
			public readonly int MinimumTerrainContourSpacing = default;
			public readonly int MinimumBeachLength = 0;
			public readonly int MinimumWaterCliffLength = 0;
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
			public readonly IReadOnlyList<MultiBrush> SegmentedBrushes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> ForestObstacles;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> UnplayableObstacles;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> CivilianBuildingsObstacles;
			[FieldLoader.Ignore]
			public readonly IReadOnlyDictionary<ushort, IReadOnlyList<MultiBrush>> RepaintTiles;

			[FieldLoader.Ignore]
			public readonly ResourceTypeInfo DefaultResource;
			[FieldLoader.Ignore]
			public readonly IReadOnlyDictionary<string, ResourceTypeInfo> ResourceSpawnSeeds;
			[FieldLoader.LoadUsing(nameof(ResourceSpawnWeightsLoader))]
			public readonly IReadOnlyDictionary<string, int> ResourceSpawnWeights = default;

			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> ClearTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> PlayableTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> DominantTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> ZoneableTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> ClearSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> BeachSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> WaterCliffSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> CliffSegmentTypes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<string> RoadSegmentTypes;

			public Parameters(Map map, MiniYaml my)
			{
				FieldLoader.Load(this, my);

				var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
				SegmentedBrushes = MultiBrush.LoadCollection(map, "Segmented");
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

				var resourceTypes = map.Rules.Actors[SystemActors.World].TraitInfoOrDefault<ResourceLayerInfo>().ResourceTypes;
				if (!resourceTypes.TryGetValue(my.NodeWithKey("DefaultResource").Value.Value, out DefaultResource))
					throw new YamlException("DefaultResource is not valid");
				var playerResourcesInfo = map.Rules.Actors[SystemActors.Player].TraitInfoOrDefault<PlayerResourcesInfo>();
				try
				{
					ResourceSpawnSeeds = my.NodeWithKey("ResourceSpawnSeeds").Value
						.ToDictionary(subMy => subMy.Value)
						.ToDictionary(kv => kv.Key, kv => resourceTypes[kv.Value]);
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
						.Select(terrainInfo.GetTerrainIndex)
						.ToFrozenSet();
				}

				IReadOnlyList<string> ParseSegmentTypes(string key)
				{
					return my.NodeWithKey(key).Value.Value
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.ToImmutableArray();
				}

				ClearTerrain = ParseTerrainIndexes("ClearTerrain");
				PlayableTerrain = ParseTerrainIndexes("PlayableTerrain");
				DominantTerrain = ParseTerrainIndexes("DominantTerrain");
				ZoneableTerrain = ParseTerrainIndexes("ZoneableTerrain");

				ClearSegmentTypes = ParseSegmentTypes("ClearSegmentTypes");
				BeachSegmentTypes = ParseSegmentTypes("BeachSegmentTypes");
				if (WaterRoughness > 0)
					WaterCliffSegmentTypes = ParseSegmentTypes("WaterCliffSegmentTypes");

				CliffSegmentTypes = ParseSegmentTypes("CliffSegmentTypes");
				RoadSegmentTypes = ParseSegmentTypes("RoadSegmentTypes");

				Validate(terrainInfo);
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

			public void Validate(ITemplatedTerrainInfo terrainInfo)
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
				if (WaterRoughness > 0 && MinimumCoastStraight < 0)
					throw new MapGenerationException("MinimumCoastStraight must be >= 0");
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
				if (WaterRoughness < 0 || WaterRoughness > FractionMax)
					throw new MapGenerationException("WaterRoughness must be between 0 and {FractionMax}");
				if (RoughnessRadius < 1)
					throw new MapGenerationException("RoughnessRadius must be >= 1");
				if (MaximumAltitude < 0)
					throw new MapGenerationException("MaximumAltitude must be >= 0");
				if (MinimumTerrainContourSpacing < 0)
					throw new MapGenerationException("MinimumTerrainContourSpacing must be >= 0");
				if (WaterRoughness > 0 && MinimumBeachLength < 1)
					throw new MapGenerationException("MinimumBeachLength must be >= 1");
				if (WaterRoughness > 0 && MinimumCliffLength < 1)
					throw new MapGenerationException("MinimumWaterCliffLength must be >= 1");
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

				if (!(terrainInfo.Templates.TryGetValue(LandTile, out var landTemplate) && landTemplate.Contains(0)))
					throw new MapGenerationException("LandTile is not valid");
				if (!(terrainInfo.Templates.TryGetValue(LandTile, out var waterTemplate) && waterTemplate.Contains(0)))
					throw new MapGenerationException("WaterTile is not valid");

				if (Players > 32)
					throw new MapGenerationException("Total number of players must not exceed 32");

				var symmetryCount = Symmetry.RotateAndMirrorProjectionCount(Rotations, Mirror);
				if (Players % symmetryCount != 0)
					throw new MapGenerationException($"Total number of players must be a multiple of {symmetryCount}");
			}
		}

		public IMapGeneratorSettings GetSettings()
		{
			return new MapGeneratorSettings(this, Settings);
		}

		public Map Generate(ModData modData, MapGenerationArgs args)
		{
			var terrainInfo = modData.DefaultTerrainInfo[args.Tileset];
			var size = args.Size;

			var map = new Map(modData, terrainInfo, size);
			var actorPlans = new List<ActorPlan>();

			var param = new Parameters(map, args.Settings);

			var terraformer = new Terraformer(args, map, modData, actorPlans, param.Mirror, param.Rotations);

			var waterIsPlayable = param.PlayableTerrain.Contains(terrainInfo.GetTerrainIndex(new TerrainTile(param.WaterTile, 0)));

			var externalCircleRadius = CellLayerUtils.Radius(map) - new WDist((param.MinimumLandSeaThickness + param.MinimumMountainThickness) * 1024);
			if (param.ExternalCircularBias != 0 && externalCircleRadius.Length <= 0)
				throw new MapGenerationException("map is too small for circular shaping");

			CellLayer<MultiBrush.Replaceability> PlayableToReplaceable()
			{
				var playable = terraformer.CheckSpace(param.PlayableTerrain, true);
				var basicLand = terraformer.CheckSpace(param.LandTile);
				var replace = new CellLayer<MultiBrush.Replaceability>(map);
				foreach (var mpos in map.AllCells.MapCoords)
					if (playable[mpos])
					{
						if (basicLand[mpos])
							replace[mpos] = MultiBrush.Replaceability.Any;
						else
							replace[mpos] = MultiBrush.Replaceability.Actor;
					}
					else
					{
						replace[mpos] = MultiBrush.Replaceability.None;
					}

				return replace;
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

			var elevationRandom = new MersenneTwister(random.Next());
			var coastTilingRandom = new MersenneTwister(random.Next());
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
			var pickAnyRandom = new MersenneTwister(random.Next());

			terraformer.InitMap();

			foreach (var mpos in map.AllCells.MapCoords)
				map.Tiles[mpos] = terraformer.PickTile(pickAnyRandom, param.LandTile);

			var elevation = terraformer.ElevationNoiseMatrix(
				elevationRandom,
				param.TerrainFeatureSize,
				param.TerrainSmoothing);
			var roughnessMatrix = MatrixUtils.GridVariance(
				elevation,
				param.RoughnessRadius);

			Matrix<bool> mapShape;
			if (param.ExternalCircularBias == 0)
				mapShape = new Matrix<bool>(CellLayerUtils.CellBounds(map).Size.ToInt2()).Fill(true);
			else
				mapShape = CellLayerUtils.ToMatrix(terraformer.CenteredCircle(true, false, externalCircleRadius), false);

			var landPlan = terraformer.SliceElevation(elevation, mapShape, FractionMax - param.Water);

			if (param.ExternalCircularBias > 0)
			{
				for (var n = 0; n < landPlan.Data.Length; n++)
					landPlan[n] |= !mapShape[n];
				var ring = terraformer.CenteredCircle(false, true, externalCircleRadius + new WDist(param.MinimumMountainThickness * 1024));
				var path = TilingPath.QuickCreate(
					map,
					param.SegmentedBrushes,
					CellLayerUtils.BordersToPoints(ring)[0],
					(param.MinimumMountainThickness - 1) / 2,
					param.CliffSegmentTypes[0],
					param.CliffSegmentTypes[0]);
				var brush = path.Tile(cliffTilingRandom)
					?? throw new MapGenerationException("Could not fit tiles for exterior circle cliffs");
				terraformer.PaintTiling(pickAnyRandom, brush);
			}

			landPlan = MatrixUtils.BooleanBlotch(
				landPlan,
				param.TerrainSmoothing,
				param.SmoothingThreshold, /*smoothingThresholdOutOf=*/FractionMax,
				param.MinimumLandSeaThickness,
				/*bias=*/param.Water <= FractionMax / 2);

			var coast = MatrixUtils.BordersToPoints(landPlan);
			List<TilingPath> coastPaths;
			if (param.WaterRoughness > 0)
			{
				var beachZone = new Terraformer.PathPartitionZone()
				{
					RequiredSomewhere = true,
					SegmentType = param.BeachSegmentTypes[0],
					MinimumLength = param.MinimumBeachLength,
					MaximumDeviation = param.MinimumLandSeaThickness - 1,
				};
				var waterCliffZone = new Terraformer.PathPartitionZone()
				{
					SegmentType = param.WaterCliffSegmentTypes[0],
					MinimumLength = param.MinimumCliffLength,
					MaximumDeviation = param.MinimumLandSeaThickness - 1,
				};

				var waterCliffMask = MatrixUtils.CalibratedBooleanThreshold(
					roughnessMatrix,
					param.WaterRoughness, FractionMax);
				var partitionMask = waterCliffMask.Map(masked => masked ? waterCliffZone : beachZone);
				coastPaths = terraformer.PartitionPaths(
					coast,
					[beachZone, waterCliffZone],
					partitionMask,
					param.SegmentedBrushes,
					param.MinimumCoastStraight);

				foreach (var coastPath in coastPaths)
					coastPath
						.OptimizeLoop()
						.ExtendEdge(4);
			}
			else
			{
				coastPaths = CellLayerUtils.FromMatrixPoints(coast, map.Tiles)
					.Select(beach =>
						TilingPath.QuickCreate(
								map,
								param.SegmentedBrushes,
								beach,
								param.MinimumLandSeaThickness - 1,
								param.BeachSegmentTypes[0],
								param.BeachSegmentTypes[0])
									.ExtendEdge(4))
					.ToList();
			}

			var landCoastWater = terraformer.PaintLoopsAndFill(
				coastTilingRandom,
				coastPaths,
				landPlan[0] ? Terraformer.Side.In : Terraformer.Side.Out,
				[new MultiBrush().WithTemplate(map, param.WaterTile, CVec.Zero)],
				null)
					?? throw new MapGenerationException("Could not fit tiles for coast");

			if (param.Mountains > 0)
			{
				var cliffMask = MatrixUtils.CalibratedBooleanThreshold(
					roughnessMatrix,
					param.Roughness, FractionMax);
				var cliffPlan = Matrix<bool>.Zip(landPlan, mapShape, (a, b) => a && b);

				for (var altitude = 0; altitude < param.MaximumAltitude; altitude++)
				{
					cliffPlan = terraformer.SliceElevation(
						elevation,
						cliffPlan,
						param.Mountains,
						param.MinimumTerrainContourSpacing);
					cliffPlan = MatrixUtils.BooleanBlotch(
						cliffPlan,
						param.TerrainSmoothing,
						param.SmoothingThreshold, /*smoothingThresholdOutOf=*/FractionMax,
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
						var cliffPath = TilingPath.QuickCreate(
							map,
							param.SegmentedBrushes,
							cliff,
							(param.MinimumMountainThickness - 1) / 2,
							param.CliffSegmentTypes[0],
							param.ClearSegmentTypes[0])
								.ExtendEdge(4);
						var brush = cliffPath.Tile(cliffTilingRandom)
							?? throw new MapGenerationException("Could not fit tiles for cliffs");
						terraformer.PaintTiling(pickAnyRandom, brush);
					}
				}
			}

			if (param.Forests > 0)
			{
				var space = terraformer.CheckSpace(param.ClearTerrain);
				var passages = terraformer.PlanPassages(
					topologyRandom,
					terraformer.ImproveSymmetry(space, true, (a, b) => a && b),
					param.ForestCutout,
					param.MaximumCutoutSpacing);
				var forestNoise = terraformer.BooleanNoise(
					forestRandom,
					param.ForestFeatureSize,
					param.Forests,
					param.ForestClumpiness);
				var replace = PlayableToReplaceable();
				foreach (var mpos in map.AllCells.MapCoords)
					if (!forestNoise[mpos] || !space[mpos] || passages[mpos])
						replace[mpos] = MultiBrush.Replaceability.None;
				terraformer.PaintArea(forestTilingRandom, replace, param.ForestObstacles);
			}

			if (param.EnforceSymmetry != 0)
			{
				var asymmetries = terraformer.FindAsymmetries(param.DominantTerrain, true, param.EnforceSymmetry == 2);
				terraformer.PaintActors(symmetryTilingRandom, asymmetries, param.ForestObstacles);
			}

			CellLayer<bool> playable;
			{
				// For circle-in-mountains, the outside is unplayable and should never count as
				// the largest/preferred region.
				CellLayer<bool> poison = null;
				if (param.ExternalCircularBias > 0)
					poison = terraformer.CenteredCircle(
						false, true, CellLayerUtils.Radius(map.Tiles) - new WDist(1024));

				playable = terraformer.ChoosePlayableRegion(
					terraformer.CheckSpace(param.PlayableTerrain, true, false, true),
					poison)
						?? throw new MapGenerationException("could not find a playable region");

				var minimumPlayableSpace = (int)(param.Players * Math.PI * param.SpawnBuildSize * param.SpawnBuildSize);
				if (playable.Count(p => p) < minimumPlayableSpace)
					throw new MapGenerationException("playable space is too small");

				if (param.DenyWalledAreas)
				{
					// Coast tiles are particularly problematic. If they're for unplayable bodies
					// of water, they should be obliterated. If they're just surrounded by rocks,
					// trees, etc, they should be filled in with actors.
					if (waterIsPlayable)
					{
						var mask = CellLayerUtils.Clone(playable);
						terraformer.ZoneFromOutOfBounds(mask, true);
						terraformer.FillUnmaskedSideAndBorder(
							mask,
							landCoastWater,
							Terraformer.Side.Out,
							cpos => map.Tiles[cpos] = terraformer.PickTile(pickAnyRandom, param.LandTile));
					}

					var replace = PlayableToReplaceable();
					foreach (var mpos in map.AllCells.MapCoords)
						if (playable[mpos] || !map.Bounds.Contains(mpos.U, mpos.V))
							replace[mpos] = MultiBrush.Replaceability.None;

					terraformer.PaintArea(debrisTilingRandom, replace, param.UnplayableObstacles);
				}
			}

			if (param.Roads)
			{
				// TODO: Move or collapse into configuration
				const int RoadMinimumShrinkLength = 12;
				const int RoadStraightenShrink = 4;
				const int RoadStraightenGrow = 2;
				const int RoadInertialRange = 8;

				var roadPaths = terraformer.PlanRoads(
					terraformer.CheckSpace(param.ClearTerrain, true, false),
					param.RoadSpacing,
					RoadMinimumShrinkLength + 2 * (RoadStraightenShrink + param.RoadShrink));
				foreach (var roadPath in roadPaths)
				{
					var tilingPath = TilingPath.QuickCreate(
						map,
						param.SegmentedBrushes,
						roadPath,
						param.RoadSpacing - 1,
						param.RoadSegmentTypes[0],
						param.ClearSegmentTypes[0])
							.StraightenEnds(
								RoadStraightenShrink + param.RoadShrink,
								RoadStraightenGrow,
								RoadMinimumShrinkLength,
								RoadInertialRange)
							.RetainIfValid();
					if (tilingPath.Points == null)
						continue;

					var brush = tilingPath.Tile(roadTilingRandom)
						?? throw new MapGenerationException("Could not fit tiles for roads");
					terraformer.PaintTiling(pickAnyRandom, brush);
				}
			}

			if (param.CreateEntities)
			{
				var zoneable = terraformer.GetZoneable(param.ZoneableTerrain, playable);

				var zoneableArea = zoneable.Count(v => v);
				var symmetryCount = Symmetry.RotateAndMirrorProjectionCount(param.Rotations, param.Mirror);
				var entityMultiplier =
					(long)zoneableArea * param.AreaEntityBonus +
					(long)param.Players * param.PlayerCountEntityBonus;
				var perSymmetryEntityMultiplier = entityMultiplier / symmetryCount;

				// Spawn generation
				var symmetryPlayers = param.Players / symmetryCount;
				for (var iteration = 0; iteration < symmetryPlayers; iteration++)
				{
					var chosenCPos = terraformer.ChooseSpawnInZoneable(
						playerRandom,
						zoneable,
						param.CentralSpawnReservationFraction,
						param.MinimumSpawnRadius,
						param.SpawnRegionSize,
						param.SpawnReservation)
							?? throw new MapGenerationException("Not enough room for player spawns");

					var spawn = new ActorPlan(map, "mpspawn")
					{
						Location = chosenCPos,
					};

					var resourceSpawnPreferences = terraformer.TargetWalkingDistance(
						terraformer.CheckSpace(param.PlayableTerrain, true),
						terraformer.ErodeZones(zoneable, 1),
						[chosenCPos],
						new WDist((param.SpawnBuildSize + param.SpawnRegionSize * 2) * 512),
						new WDist(param.SpawnRegionSize * 1024));
					terraformer.AddDistributedActors(
						playerRandom,
						zoneable,
						resourceSpawnPreferences,
						param.ResourceSpawnWeights,
						param.SpawnResourceSpawns,
						false,
						new WDist(param.ResourceSpawnReservation * 1024));

					terraformer.ProjectPlaceDezoneActor(spawn, zoneable, new WDist(param.SpawnReservation * 1024));
				}

				// Expansions
				{
					var resourceSpawnsRemaining = (int)(param.MaximumExpansionResourceSpawns * perSymmetryEntityMultiplier / EntityBonusMax);
					while (resourceSpawnsRemaining > 0)
					{
						var added = terraformer.AddActorCluster(
							expansionRandom,
							zoneable,
							param.ResourceSpawnWeights,
							Math.Min(resourceSpawnsRemaining, expansionRandom.Next(param.MaximumResourceSpawnsPerExpansion) + 1),
							param.ExpansionInner,
							param.MinimumExpansionSize,
							param.MaximumExpansionSize,
							param.ExpansionBorder,
							true,
							new WDist(param.ResourceSpawnReservation * 1024));
						resourceSpawnsRemaining -= added;
						if (added == 0)
							break;
					}
				}

				// Neutral buildings
				{
					var (buildingTypes, buildingWeights) = Terraformer.SplitDictionary(param.BuildingWeights);
					var targetBuildingCount =
						(param.MaximumBuildings != 0)
							? buildingRandom.Next(
								(int)(param.MinimumBuildings * perSymmetryEntityMultiplier / EntityBonusMax),
								(int)(param.MaximumBuildings * perSymmetryEntityMultiplier / EntityBonusMax) + 1)
							: 0;
					for (var i = 0; i < targetBuildingCount; i++)
						terraformer.AddActor(
							buildingRandom,
							zoneable,
							buildingTypes[buildingRandom.PickWeighted(buildingWeights)]);
				}

				// Grow resources
				var targetResourceValue = param.ResourcesPerPlayer * entityMultiplier / EntityBonusMax;
				if (targetResourceValue > 0)
				{
					var resourcePattern = terraformer.ResourceNoise(
						resourceRandom,
						param.ResourceFeatureSize,
						param.OreClumpiness,
						param.OreUniformity * 1024 / FractionMax);

					var resourceBiases = new List<Terraformer.ResourceBias>();
					var wSpawnBuildSizeSq = (long)param.SpawnBuildSize * param.SpawnBuildSize * 1024 * 1024;

					// Bias towards resource spawns
					foreach (var (actorType, resourceType) in param.ResourceSpawnSeeds.OrderBy(kv => kv.Key))
					{
						resourceBiases.AddRange(
							terraformer.ActorsOfType(actorType)
								.Select(a => new Terraformer.ResourceBias(a)
								{
									BiasRadius = new WDist(16 * 1024),
									Bias = (value, rSq) => value + (int)(1024 * 1024 / (1024 + Exts.ISqrt(rSq))),
									ResourceType = resourceType,
								}));
					}

					// Bias towards player spawns, but also reserve an area for base building.
					resourceBiases.AddRange(
						terraformer.ActorsOfType("mpspawn")
							.Select(a => new Terraformer.ResourceBias(a)
							{
								ExclusionRadius = new WDist(param.SpawnBuildSize * 1024),
								BiasRadius = new WDist(param.SpawnRegionSize * 2 * 1024),
								Bias = (value, rSq) => value + (int)(value * param.SpawnResourceBias * wSpawnBuildSizeSq / Math.Max(rSq, 1024 * 1024) / FractionMax),
							}));

					var (plan, typePlan) = terraformer.PlanResources(
						resourcePattern,
						CellLayerUtils.Intersect([playable, terraformer.CheckSpace(null, true)]),
						param.DefaultResource,
						resourceBiases);
					terraformer.GrowResources(
						plan,
						typePlan,
						targetResourceValue);
					terraformer.ZoneFromResources(zoneable, false);
				}

				// CivilianBuildings
				if (param.CivilianBuildings > 0)
				{
					var decorationNoise = terraformer.DecorationPattern(
						decorationRandom,
						terraformer.CheckSpace(param.PlayableTerrain, true),
						CellLayerUtils.Intersect([zoneable, terraformer.CheckSpace(param.LandTile)]),
						param.CivilianBuildings,
						param.CivilianBuildingsFeatureSize,
						param.CivilianBuildingDensity,
						param.MinimumCivilianBuildingDensity,
						param.CivilianBuildingDensityRadius);
					terraformer.PaintActors(
						decorationTilingRandom,
						decorationNoise,
						param.CivilianBuildingsObstacles,
						alwaysPreferLargerBrushes: true);
				}
			}

			// Cosmetically repaint tiles
			terraformer.RepaintTiles(repaintRandom, param.RepaintTiles);

			terraformer.ReorderPlayerSpawns();
			terraformer.BakeMap();

			return map;
		}

		public override object Create(ActorInitializer init)
		{
			return new ExperimentalMapGenerator(init, this);
		}
	}

	public class ExperimentalMapGenerator : IEditorTool
	{
		public string Label { get; }
		public string PanelWidget { get; }
		public TraitInfo TraitInfo { get; }
		public bool IsEnabled { get; }

		public ExperimentalMapGenerator(ActorInitializer init, ExperimentalMapGeneratorInfo info)
		{
			Label = info.Name;
			PanelWidget = info.PanelWidget;
			TraitInfo = info;
			IsEnabled = info.Tilesets.Contains(init.Self.World.Map.Tileset);
		}
	}
}
