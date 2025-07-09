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
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;
using static OpenRA.Mods.Common.Traits.ResourceLayerInfo;

namespace OpenRA.Mods.D2k.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	public sealed class D2kMapGeneratorInfo : TraitInfo<D2kMapGenerator>, IEditorMapGeneratorInfo, IEditorToolInfo
	{
		[FieldLoader.Require]
		public readonly string Type = null;

		[FieldLoader.Require]
		[FluentReference]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Tilesets that are compatible with this map generator.")]
		public readonly string[] Tilesets = null;

		[FluentReference]
		[Desc("The title to use for generated maps.")]
		public readonly string MapTitle = "label-random-map";

		[Desc("The widget tree to open when the tool is selected.")]
		public readonly string PanelWidget = "MAP_GENERATOR_TOOL_PANEL";

		// This is purely of interest to the linter.
		[FieldLoader.LoadUsing(nameof(FluentReferencesLoader))]
		[FluentReference]
		public readonly List<string> FluentReferences = null;

		[FieldLoader.LoadUsing(nameof(SettingsLoader))]
		public readonly MiniYaml Settings;

		string IMapGeneratorInfo.Type => Type;
		string IMapGeneratorInfo.Name => Name;
		string IMapGeneratorInfo.MapTitle => MapTitle;
		string[] IEditorMapGeneratorInfo.Tilesets => Tilesets;

		static MiniYaml SettingsLoader(MiniYaml my)
		{
			return my.NodeWithKey("Settings").Value;
		}

		static List<string> FluentReferencesLoader(MiniYaml my)
		{
			return new MapGeneratorSettings(null, my.NodeWithKey("Settings").Value)
				.Options.SelectMany(o => o.GetFluentReferences()).ToList();
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
			public readonly int SandDetailFeatureSize = default;
			[FieldLoader.Require]
			public readonly int DuneFeatureSize = default;
			[FieldLoader.Require]
			public readonly int ResourceFeatureSize = default;
			[FieldLoader.Require]
			public readonly int TerrainSmoothing = default;
			[FieldLoader.Require]
			public readonly int DuneSmoothing = default;
			[FieldLoader.Require]
			public readonly int SmoothingThreshold = default;
			[FieldLoader.Require]
			public readonly int RockRoughness = default;
			[FieldLoader.Require]
			public readonly int SandRoughness = default;
			[FieldLoader.Require]
			public readonly int RoughnessRadius = default;
			[FieldLoader.Require]
			public readonly int Rock = default;
			[FieldLoader.Require]
			public readonly int SandCliffs = default;
			[FieldLoader.Require]
			public readonly int Dunes = default;
			[FieldLoader.Require]
			public readonly int MinimumRockStraight = default;
			[FieldLoader.Require]
			public readonly int MinimumSandCliffStraight = default;
			[FieldLoader.Require]
			public readonly int MinimumRockSandThickness = default;
			[FieldLoader.Require]
			public readonly int MinimumSandCliffThickness = default;
			[FieldLoader.Require]
			public readonly int MinimumDuneThickness = default;
			[FieldLoader.Require]
			public readonly int MinimumRockSmoothLength = default;
			[FieldLoader.Require]
			public readonly int MinimumSandRockCliffLength = default;
			[FieldLoader.Require]
			public readonly int MinimumSandSandCliffLength = default;
			[FieldLoader.Require]
			public readonly int MinimumSandLength = default;
			[FieldLoader.Require]
			public readonly int SandContourSpacing = default;
			[FieldLoader.Require]
			public readonly int DuneContourSpacing = default;
			[FieldLoader.Require]
			public readonly int SandDetail = default;
			[FieldLoader.Require]
			public readonly int SandDetailClumpiness = default;
			[FieldLoader.Require]
			public readonly int SandDetailCutout = default;
			[FieldLoader.Require]
			public readonly int MaximumSandDetailCutoutSpacing = default;

			[FieldLoader.Require]
			public readonly bool CreateEntities = default;
			[FieldLoader.Require]
			public readonly int AreaEntityBonus = default;
			[FieldLoader.Require]
			public readonly int PlayerCountEntityBonus = default;
			[FieldLoader.Require]
			public readonly int MinimumSpawnRockArea = default;
			[FieldLoader.Require]
			public readonly int CentralSpawnReservationFraction = default;
			[FieldLoader.Require]
			public readonly int SpawnRegionSize = default;
			[FieldLoader.Require]
			public readonly int MinimumSpawnRadius = default;
			[FieldLoader.Require]
			public readonly int SpawnReservation = default;
			[FieldLoader.Require]
			public readonly int BiasedResourceSpawns = default;
			[FieldLoader.Require]
			public readonly int ResourceSpawnSpacing = default;
			[FieldLoader.Require]
			public readonly int UnbiasedResourceSpawns = default;
			[FieldLoader.Require]
			public readonly int ResourceSpawnReservation = default;
			[FieldLoader.Require]
			public readonly int ResourcesPerPlayer = default;
			[FieldLoader.Require]
			public readonly int ResourceUniformity = default;
			[FieldLoader.Require]
			public readonly int ResourceClumpiness = default;
			[FieldLoader.Require]
			public readonly string ResourceSpawn = default;
			[FieldLoader.Ignore]
			public readonly ResourceTypeInfo Resource = default;
			[FieldLoader.Require]
			public readonly string WormSpawn = default;
			[FieldLoader.Require]
			public readonly int WormSpawns = default;
			[FieldLoader.Require]
			public readonly int WormSpawnReservation = default;

			[FieldLoader.Require]
			public readonly ushort SandTile = default;
			[FieldLoader.Require]
			public readonly ushort RockTile = default;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> PlayableTerrain;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> RockZoneableTerrain = default;
			[FieldLoader.Ignore]
			public readonly IReadOnlySet<byte> SandZoneableTerrain = default;
			[FieldLoader.Require]
			public readonly string RockSmoothSegmentType = default;
			[FieldLoader.Require]
			public readonly string SandRockCliffSegmentType = default;
			[FieldLoader.Require]
			public readonly string SandSandCliffSegmentType = default;
			[FieldLoader.Require]
			public readonly string SandSegmentType = default;
			[FieldLoader.Require]
			public readonly string DuneSegmentType = default;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> SegmentedBrushes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> SandDetailBrushes;
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<MultiBrush> DuneBrushes;

			public Parameters(Map map, MiniYaml my)
			{
				FieldLoader.Load(this, my);

				var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;

				IReadOnlySet<byte> ParseTerrainIndexes(string key)
				{
					return my.NodeWithKey(key).Value.Value
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.Select(terrainInfo.GetTerrainIndex)
						.ToImmutableHashSet();
				}

				var resourceTypes = map.Rules.Actors[SystemActors.World].TraitInfoOrDefault<ResourceLayerInfo>().ResourceTypes;
				if (!resourceTypes.TryGetValue(my.NodeWithKey("Resource").Value.Value, out Resource))
					throw new YamlException("Resource is not valid");

				PlayableTerrain = ParseTerrainIndexes("PlayableTerrain");
				RockZoneableTerrain = ParseTerrainIndexes("RockZoneableTerrain");
				SandZoneableTerrain = ParseTerrainIndexes("SandZoneableTerrain");
				SegmentedBrushes = MultiBrush.LoadCollection(map, "Segmented");
				SandDetailBrushes = MultiBrush.LoadCollection(map, my.NodeWithKey("SandDetailBrushes").Value.Value);
				DuneBrushes = MultiBrush.LoadCollection(map, my.NodeWithKey("DuneBrushes").Value.Value);
			}

			static object MirrorLoader(MiniYaml my)
			{
				if (Symmetry.TryParseMirror(my.NodeWithKey("Mirror").Value.Value, out var mirror))
					return mirror;
				else
					throw new YamlException($"Invalid Mirror value `{my.NodeWithKey("Mirror").Value.Value}`");
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

			var sandZone = new Terraformer.PathPartitionZone()
			{
				ShouldTile = false,
				SegmentType = param.SandSegmentType,
				MinimumLength = param.MinimumSandLength,
			};
			var rockSmoothZone = new Terraformer.PathPartitionZone()
			{
				SegmentType = param.RockSmoothSegmentType,
				MinimumLength = param.MinimumRockSmoothLength,
				MaximumDeviation = 10,
			};
			var sandRockCliffZone = new Terraformer.PathPartitionZone()
			{
				SegmentType = param.SandRockCliffSegmentType,
				MinimumLength = param.MinimumSandRockCliffLength,
				MaximumDeviation = 10,
			};
			var sandSandCliffZone = new Terraformer.PathPartitionZone()
			{
				SegmentType = param.SandSandCliffSegmentType,
				MinimumLength = param.MinimumSandSandCliffLength,
				MaximumDeviation = 10,
			};

			// Use `random` to derive separate independent random number generators.
			//
			// This prevents changes in one part of the algorithm from affecting randomness in
			// other parts.
			//
			// In order to maximize stability, additions should be appended only. Disused
			// derivatives may be deleted but should be replaced with their unused call to
			// random.Next(). All generators should be created unconditionally.
			var random = new MersenneTwister(param.Seed);
			var pickAnyRandom = new MersenneTwister(random.Next());
			var elevationRandom = new MersenneTwister(random.Next());
			var rockTilingRandom = new MersenneTwister(random.Next());
			var sandSandCliffTilingRandom = new MersenneTwister(random.Next());
			var playerRandom = new MersenneTwister(random.Next());
			var expansionRandom = new MersenneTwister(random.Next());
			var resourceRandom = new MersenneTwister(random.Next());
			var sandDetailRandom = new MersenneTwister(random.Next());
			var topologyRandom = new MersenneTwister(random.Next());
			var sandDetailTilingRandom = new MersenneTwister(random.Next());
			var duneRandom = new MersenneTwister(random.Next());
			var duneTilingRandom = new MersenneTwister(random.Next());

			terraformer.InitMap();

			// Clear map to random sand
			foreach (var mpos in map.AllCells.MapCoords)
				map.Tiles[mpos] = terraformer.PickTile(pickAnyRandom, param.SandTile);

			var elevation = terraformer.ElevationNoiseMatrix(
				elevationRandom,
				param.TerrainFeatureSize,
				param.TerrainSmoothing);
			var roughnessMatrix = MatrixUtils.GridVariance(
				elevation,
				param.RoughnessRadius);

			// Rock generation
			CellLayer<Terraformer.Side> rockSmoothSand;
			{
				var cliffMask = MatrixUtils.CalibratedBooleanThreshold(
					roughnessMatrix,
					param.RockRoughness, FractionMax);
				var plan = terraformer.SliceElevation(elevation, null, param.Rock);
				plan = MatrixUtils.BooleanBlotch(
					plan,
					param.TerrainSmoothing,
					param.SmoothingThreshold, /*smoothingThresholdOutOf=*/FractionMax,
					param.MinimumRockSandThickness,
					true);
				var contours = MatrixUtils.BordersToPoints(plan);
				var partitionMask = cliffMask.Map(masked => masked ? sandRockCliffZone : rockSmoothZone);
				var tilingPaths = terraformer.PartitionPaths(
					contours,
					[rockSmoothZone, sandRockCliffZone],
					partitionMask,
					param.SegmentedBrushes,
					param.MinimumRockStraight);
				foreach (var tilingPath in tilingPaths)
					tilingPath
						.OptimizeLoop()
						.ExtendEdge(4);

				rockSmoothSand = terraformer.PaintLoopsAndFill(
					rockTilingRandom,
					tilingPaths,
					plan[0] ? Terraformer.Side.In : Terraformer.Side.Out,
					null,
					[new MultiBrush().WithTemplate(map, param.RockTile, CVec.Zero)])
						?? throw new MapGenerationException("Could not fit tiles for rock platforms");
			}

			// Sand cliff generation
			if (param.SandCliffs > 0)
			{
				var inverseElevation = elevation.Map(v => -v);
				var cliffMask = MatrixUtils.CalibratedBooleanThreshold(
					roughnessMatrix,
					param.SandRoughness, FractionMax);
				var plan = terraformer.SliceElevation(
					inverseElevation,
					CellLayerUtils.ToMatrix(rockSmoothSand, Terraformer.Side.Out)
						.Map(s => s == Terraformer.Side.Out),
					param.SandCliffs,
					param.SandContourSpacing);
				plan = MatrixUtils.BooleanBlotch(
					plan,
					param.TerrainSmoothing,
					param.SmoothingThreshold, /*smoothingThresholdOutOf=*/FractionMax,
					param.MinimumSandCliffThickness,
					true);
				var contours = MatrixUtils.BordersToPoints(plan);
				var partitionMask = cliffMask.Map(masked => masked ? sandSandCliffZone : sandZone);
				var tilingPaths = terraformer.PartitionPaths(
					contours,
					[sandSandCliffZone, sandZone],
					partitionMask,
					param.SegmentedBrushes,
					param.MinimumSandCliffStraight);
				foreach (var tilingPath in tilingPaths)
				{
					var brush = tilingPath
						.OptimizeLoop()
						.ExtendEdge(4)
						.SetAutoEndDeviation()
						.Tile(sandSandCliffTilingRandom)
							?? throw new MapGenerationException("Could not fit tiles for sand-sand cliffs");
					terraformer.PaintTiling(pickAnyRandom, brush);
				}
			}

			// Sand Detail
			if (param.SandDetail > 0)
			{
				var space = terraformer.CheckSpace(param.PlayableTerrain);
				var passages = terraformer.PlanPassages(
					topologyRandom,
					terraformer.ImproveSymmetry(space, true, (a, b) => a && b),
					param.SandDetailCutout,
					param.MaximumSandDetailCutoutSpacing);
				var plan = terraformer.BooleanNoise(
					sandDetailRandom,
					param.SandDetailFeatureSize,
					param.SandDetail,
					param.SandDetailClumpiness);
				plan = CellLayerUtils.Subtract([
					CellLayerUtils.Intersect([
						plan,
						terraformer.CheckSpace(param.SandTile, true)]),
					passages]);
				terraformer.PaintArea(
					sandDetailTilingRandom,
					CellLayerUtils.Map(plan, p => p ? MultiBrush.Replaceability.Any : MultiBrush.Replaceability.None),
					param.SandDetailBrushes,
					true);
			}

			// Dunes
			if (param.Dunes > 0)
			{
				var duneNoise = terraformer.ElevationNoiseMatrix(
					duneRandom,
					param.DuneFeatureSize,
					param.DuneSmoothing);
				var duneable = terraformer.CheckSpace(param.SandTile, true);
				duneable = terraformer.ImproveSymmetry(duneable, true, (a, b) => a && b);
				var plan = terraformer.SliceElevation(
					duneNoise,
					CellLayerUtils.ToMatrix(duneable, true),
					param.Dunes,
					param.DuneContourSpacing);
				plan = MatrixUtils.BooleanBlotch(
					plan,
					param.DuneSmoothing,
					param.SmoothingThreshold, /*smoothingThresholdOutOf=*/FractionMax,
					param.MinimumDuneThickness,
					false);
				var contours = CellLayerUtils.FromMatrixPoints(
					MatrixUtils.BordersToPoints(plan),
					map.Tiles);
				var tilingPaths = contours
					.Select(contour =>
						TilingPath.QuickCreate(
								map,
								param.SegmentedBrushes,
								contour,
								(param.MinimumDuneThickness - 1) / 2,
								param.DuneSegmentType,
								param.DuneSegmentType)
									.ExtendEdge(4))
					.ToArray();
				_ = terraformer.PaintLoopsAndFill(
					duneTilingRandom,
					tilingPaths,
					plan[0] ? Terraformer.Side.In : Terraformer.Side.Out,
					null,
					param.DuneBrushes)
						?? throw new MapGenerationException("Could not fit tiles for rock platforms");
			}

			if (param.CreateEntities)
			{
				var playable = terraformer.ChoosePlayableRegion(
					terraformer.CheckSpace(param.PlayableTerrain, true, false, true),
					null)
						?? throw new MapGenerationException("could not find a playable region");

				var rockZoneable = terraformer.GetZoneable(param.RockZoneableTerrain, playable);
				var (regions, regionMask) = terraformer.FindRegions(rockZoneable, DirectionExts.Spread8CVec);
				var acceptableRegions = regions
					.Where(r => r.Area >= param.MinimumSpawnRockArea)
					.Select(r => r.Id)
					.ToHashSet();
				if (acceptableRegions.Count == 0)
					throw new MapGenerationException("rocks are not big enough for players");

				rockZoneable = CellLayerUtils.Intersect([
					rockZoneable,
					CellLayerUtils.Map(regionMask, acceptableRegions.Contains)]);

				var sandZoneable = terraformer.GetZoneable(param.SandZoneableTerrain, playable);
				var spiceZoneable = CellLayerUtils.Clone(sandZoneable);
				var sandZoneableArea = sandZoneable.Count(v => v);

				var symmetryCount = Symmetry.RotateAndMirrorProjectionCount(param.Rotations, param.Mirror);
				var entityMultiplier =
					(long)sandZoneableArea * param.AreaEntityBonus +
					(long)param.Players * param.PlayerCountEntityBonus;
				var perSymmetryEntityMultiplier = entityMultiplier / symmetryCount;

				// Spawn generation
				var symmetryPlayers = param.Players / symmetryCount;
				for (var iteration = 0; iteration < symmetryPlayers; iteration++)
				{
					var chosenCPos = terraformer.ChooseSpawnInZoneable(
						playerRandom,
						rockZoneable,
						param.CentralSpawnReservationFraction,
						param.MinimumSpawnRadius,
						param.SpawnRegionSize,
						param.SpawnReservation)
							?? throw new MapGenerationException("Not enough room for player spawns");

					var spawn = new ActorPlan(map, "mpspawn")
					{
						Location = chosenCPos,
					};

					terraformer.ProjectPlaceDezoneActor(spawn, rockZoneable, new WDist(param.SpawnReservation * 1024));
				}

				// Close-to-player spice bloom spawn generation
				if (param.BiasedResourceSpawns > 0)
				{
					// Biased blooms
					var walkingDistances = terraformer.TargetWalkingDistance(
						playable,
						terraformer.ErodeZones(sandZoneable, param.ResourceSpawnSpacing),
						terraformer.ActorsOfType("mpspawn").Select(a => a.Location),
						new WDist(0),
						new WDist(1024000));
					for (var i = 0; i < param.BiasedResourceSpawns; i++)
					{
						var (chosenMpos, score) = CellLayerUtils.FindRandomBest(
							walkingDistances,
							expansionRandom,
							(a, b) => a.CompareTo(b));
						if (score == -int.MaxValue)
							throw new MapGenerationException("failed to place spice blooms near players");

						terraformer.ProjectPlaceDezoneActor(
							new ActorPlan(map, param.ResourceSpawn)
							{
								Location = chosenMpos.ToCPos(map),
							},
							sandZoneable,
							new WDist(param.ResourceSpawnReservation * 1024));
						foreach (var mpos in map.AllCells.MapCoords)
							if (!sandZoneable[mpos])
								walkingDistances[mpos] = -int.MaxValue;
					}
				}

				// Unbiased spice bloom spawn generation
				{
					var targetResourceSpawnCount = (int)(param.UnbiasedResourceSpawns * perSymmetryEntityMultiplier / EntityBonusMax);
					for (var i = 0; i < targetResourceSpawnCount; i++)
					{
						var added = terraformer.AddActor(
							expansionRandom,
							sandZoneable,
							param.ResourceSpawn,
							new WDist(param.ResourceSpawnReservation * 1024));
						if (!added)
							break;
					}
				}

				// Worms
				{
					var targetWormSpawnCount = (int)(param.WormSpawns * perSymmetryEntityMultiplier / EntityBonusMax);
					for (var i = 0; i < targetWormSpawnCount; i++)
					{
						var added = terraformer.AddActor(
							expansionRandom,
							sandZoneable,
							param.WormSpawn,
							new WDist(param.WormSpawnReservation * 1024));
						if (!added)
							break;
					}
				}

				// Grow resources
				var targetResourceValue = param.ResourcesPerPlayer * entityMultiplier / EntityBonusMax;
				if (targetResourceValue > 0)
				{
					var resourcePattern = terraformer.ResourceNoise(
						resourceRandom,
						param.ResourceFeatureSize,
						param.ResourceClumpiness,
						param.ResourceUniformity * 1024 / FractionMax);

					var resourceBiases = new List<Terraformer.ResourceBias>();

					// Bias towards resource spawns
					resourceBiases.AddRange(
						terraformer.ActorsOfType(param.ResourceSpawn)
							.Select(a => new Terraformer.ResourceBias(a)
							{
								BiasRadius = new WDist(16 * 1024),
								Bias = (value, rSq) => value + (int)(1024 * 1024 / (1024 + Exts.ISqrt(rSq))),
							}));

					var (plan, typePlan) = terraformer.PlanResources(
						resourcePattern,
						spiceZoneable,
						param.Resource,
						resourceBiases);
					terraformer.GrowResources(
						plan,
						typePlan,
						targetResourceValue);
					terraformer.ZoneFromResources(sandZoneable, false);
				}
			}

			terraformer.BakeMap();

			return map;
		}

		string IEditorToolInfo.Label => Name;
		string IEditorToolInfo.PanelWidget => PanelWidget;
	}

	public class D2kMapGenerator { /* we're only interested in the Info */ }
}
