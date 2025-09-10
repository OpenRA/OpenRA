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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum BotMcvExpansionMode { CheckResourceCreator, CheckResource, CheckBase }

	[Desc("Manages AI MCVs and expansion.")]
	public class McvExpansionManagerBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that are considered MCVs (deploy into base builders).")]
		public readonly HashSet<string> McvTypes = [];

		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = [];

		[Desc("Actor types that are able to produce MCVs.")]
		public readonly HashSet<string> McvFactoryTypes = [];

		[Desc("Try to maintain at least this many ConstructionYardTypes, build an MCV if number is below this.")]
		public readonly int MinimumConstructionYardCount = 1;

		[Desc("Delay (in ticks) between looking for and giving out orders to new MCVs.")]
		public readonly int ScanForNewMcvInterval = 20;

		[Desc("Delay (in ticks) between check and build a MCV.")]
		public readonly int BuildMcvInterval = 101;

		[Desc("Move a conyard if have more than 1 conyard, for better expansion.")]
		public readonly int MoveConyardTick = 4500;

		[Desc("Tells the AI what building types are considered production.")]
		public readonly HashSet<string> ProductionTypes = [];

		[Desc("Tells the AI what building types are considered refineries.")]
		public readonly HashSet<string> RefineryTypes = [];

		[Desc("Move a conyard even when this is the only conyard, only works for once. Requires at least 1 refinery and 2 production buildings.")]
		public readonly int[] FirstMoveConyardTicks = [-1];

		[Desc("Initial expansion mode chosen by AI.")]
		public readonly BotMcvExpansionMode InitialExpansionMode = BotMcvExpansionMode.CheckResourceCreator;

		[Desc("Allow Bot switch expansion mode automatically on enough failure or successful attempts.")]
		public readonly bool ExpansionModeAutoSwitch = true;

		/* those are options shared by two or more modes */
		[Desc("Tick in update an indice of resource map when." + nameof(BotMcvExpansionMode.CheckResource) + "is inactive.")]
		public readonly int InactiveUpdateResourceMapInverval = 271;

		[Desc("Tick in update an indice of resource map." + nameof(BotMcvExpansionMode.CheckResource) + "is active.")]
		public readonly int ActiveUpdateResourceMapInverval = 103;

		[Desc("Distance in cells of half indice of the resource map.")]
		public readonly int ResourceMapStrideRadius = 12;

		/* those are CheckResourceCreator mode options */
		[Desc("Minimum distance in cells around the resource creator location when checking for MCV deployment location.")]
		public readonly int CRCmodeMinDeployRadius = 2;

		[Desc("Maximum distance in cells around the resource creator location when checking for MCV deployment location.")]
		public readonly int CRCmodeMaxDeployRadius = 12;

		[Desc("Tells the AI what types are considered resource creator.")]
		public readonly HashSet<string> ResourceCreatorTypes = [];

		[Desc("Distance in cells to a friendly conyard that AI dislike when choose a expanding location.")]
		public readonly int CRCmodeConyardUnfavorRange = 18;

		[Desc("Distance in cells to a friendly refinery that AI dislike when choose a expanding location.")]
		public readonly int CRCmodeRefineryUnfavorRange = 12;

		[Desc("Distance in cells that AI try to maintain to the expanding location in deployment.")]
		public readonly int CRCmodeTryMaintainRange = 10;

		[Desc("Distance in cells from center of the resource creator when checking nearby enemy base buildings for MCV expanding location.")]
		public readonly int CRCmodeEnemyBaseScanRadius = 16;

		/* those are CheckResource mode options */
		[Desc("Minimum distance in cells from the found resource creator location when checking for MCV deployment location.")]
		public readonly int CRmodeMinDeployRadius = 2;

		[Desc("Maximum distance in cells the found resource creator location when checking for MCV deployment location.")]
		public readonly int CRmodeMaxDeployRadius = 20;

		[Desc("Distance in cells that AI try to maintain to the expanding location in deployment.")]
		public readonly int CRmodeTryMaintainRange = 10;

		[Desc("Distance in cells from center of the resource indice when checking nearby enemy base buildings for MCV expanding location.",
			"Recommend to set it equal or bigger than " + nameof(ResourceMapStrideRadius) + "* 1.2.")]
		public readonly int CRmodeEnemyBaseScanRadius = 18;

		[Desc("Distance in cells to a friendly conyard that AI dislike when choose a expanding location.",
			"Recommend to set it equal or bigger than " + nameof(ResourceMapStrideRadius) + ".")]
		public readonly int CRmodeConyardUnfavorRange = 14;

		[Desc("Distance in cells to a friendly refinery that AI dislike when choose a expanding location.",
			"Recommend to set it equal or bigger than " + nameof(ResourceMapStrideRadius) + "*.")]
		public readonly int CRmodeRefineryUnfavorRange = 14;

		[Desc("Resource types that are considered can be harvested.")]
		public readonly HashSet<string> ValidResourceTypes = [];

		/* those are CheckBase mode options */
		[Desc("Minimum distance in cells from center of the base expansion when checking for MCV deployment location.")]
		public readonly int CBmodeMinDeployRadius = 2;

		[Desc("Maximum distance in cells from center of the base expansion when checking for MCV deployment location.")]
		public readonly int CBmodeMaxDeployRadius = 20;

		[Desc("Distance in cells from center of the indice when checking nearby enemy base buildings for MCV expanding location.")]
		public readonly int CBmodeEnemyBaseScanRadius = 27;

		public override object Create(ActorInitializer init) { return new McvExpansionManagerBotModule(init.Self, this); }
	}

	public class McvExpansionManagerBotModule : ConditionalTrait<McvExpansionManagerBotModuleInfo>, IBotTick, IBotRespondToAttack
	{
		// When ExpansionModeAutoSwitch is true, if the AI fails to find a deploy spot enough time even in CheckBase mode
		// NegativeMaxFailedAttempts is applied to make AI switch bettween modes more frequently until a successful attempt
		const int PositiveMaxFailedAttempts = 3;
		const int NegativeMaxFailedAttempts = 1;

		readonly World world;
		readonly Player player;
		readonly ActorIndex.OwnerAndNamesAndTrait<TransformsInfo> mcvs;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> constructionYards;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> mcvFactories;

		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotRequestUnitProduction[] requestUnitProduction;
		IResourceLayer resourceLayer;
		PathFinder pathfinder;

		int updateResourceMapDelay;
		int scanInterval;
		int buildMCVInterval;
		int moveConyardInterval;
		int updateResourceMapInterval;
		bool firstTick = true;
		bool firstUndeploy = true;
		bool allowfallback;

		BotMcvExpansionMode mcvExpansionMode;
		int mcvDeploymentMinDeployRadius;
		int mcvDeploymentMaxDeployRadius;
		int mcvDeploymentTryMaintainRange;

		int maxFailedAttempts = PositiveMaxFailedAttempts;
		int failedAttempts;
		CPos? lastFailedCheckSpot;

		// It is unnecessary to respond every tick, we only need to respond once in a while.
		int attackrespondcooldown = 20;

		(CPos IndiceCenter, int Value, CPos ResourceCenter)[] resourceMapIndices = null;
		readonly int indiceSideLength;
		readonly int indiceResourceScanRadius;
		int resourceMapIndicesColumnCount;
		int resourceMapIndicesRowCount;

		int pathDistanceSquareFactor;
		int updateResourceMapIndex = 0;

		public McvExpansionManagerBotModule(Actor self, McvExpansionManagerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			mcvs = new ActorIndex.OwnerAndNamesAndTrait<TransformsInfo>(world, info.McvTypes, player);
			constructionYards = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.ConstructionYardTypes, player);
			mcvFactories = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.McvFactoryTypes, player);
			indiceSideLength = info.ResourceMapStrideRadius << 1;

			// FindTilesInAnnulus returns cells in a rough circle shape, and resourceMapIndices are divided in square,
			// so we need a larger range to cover cells in the indice approximately, but avoid takes too much other indices' cells.
			indiceResourceScanRadius = info.ResourceMapStrideRadius * 12 / 10; // ≈ * (sqrt(2) + 1) / 2
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query player traits from self, which refers
			// for bot modules always to the Player actor.
			resourceLayer = self.World.WorldActor.TraitOrDefault<IResourceLayer>();
			notifyPositionsUpdated = self.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			requestUnitProduction = self.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
			pathfinder = world.WorldActor.Trait<PathFinder>();
			moveConyardInterval = Info.FirstMoveConyardTicks.RandomOrDefault(world.LocalRandom);
			if (moveConyardInterval < 0)
				firstUndeploy = false;
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			scanInterval = world.LocalRandom.Next(Info.ScanForNewMcvInterval, Info.ScanForNewMcvInterval << 1);
			buildMCVInterval = world.LocalRandom.Next(Info.BuildMcvInterval, Info.BuildMcvInterval << 1);
			updateResourceMapInterval = world.LocalRandom.Next(Info.ActiveUpdateResourceMapInverval, Info.ActiveUpdateResourceMapInverval << 1);
		}

		void SwitchExpansionMode(BotMcvExpansionMode nextMode)
		{
			mcvExpansionMode = nextMode;
			switch (nextMode)
			{
				case BotMcvExpansionMode.CheckResourceCreator:
					mcvDeploymentMinDeployRadius = Info.CRmodeMinDeployRadius;
					mcvDeploymentMaxDeployRadius = Info.CRmodeMaxDeployRadius;
					mcvDeploymentTryMaintainRange = Info.CRCmodeTryMaintainRange;
					updateResourceMapDelay = Info.InactiveUpdateResourceMapInverval;
					break;

				case BotMcvExpansionMode.CheckResource:
					mcvDeploymentMinDeployRadius = Info.CBmodeMinDeployRadius;
					mcvDeploymentMaxDeployRadius = Info.CBmodeMaxDeployRadius;
					mcvDeploymentTryMaintainRange = Info.CRmodeTryMaintainRange;
					updateResourceMapDelay = Info.ActiveUpdateResourceMapInverval;
					break;

				case BotMcvExpansionMode.CheckBase:
					mcvDeploymentMinDeployRadius = Info.CBmodeMinDeployRadius;
					mcvDeploymentMaxDeployRadius = Info.CBmodeMaxDeployRadius;
					mcvDeploymentTryMaintainRange = (Info.CBmodeMaxDeployRadius + Info.CBmodeMinDeployRadius) >> 1;
					updateResourceMapDelay = Info.InactiveUpdateResourceMapInverval;
					break;

				default:
					break;
			}
		}

		void FindBadDeploySpot(CPos? failedSpot)
		{
			lastFailedCheckSpot = failedSpot;

			if (!Info.ExpansionModeAutoSwitch)
			{
				if (++failedAttempts >= maxFailedAttempts)
					failedAttempts = maxFailedAttempts;
				return;
			}

			if (++failedAttempts >= maxFailedAttempts)
			{
				failedAttempts = 0;
				switch (mcvExpansionMode)
				{
					case BotMcvExpansionMode.CheckResourceCreator:
						SwitchExpansionMode(BotMcvExpansionMode.CheckResource);
						break;

					case BotMcvExpansionMode.CheckResource:
						SwitchExpansionMode(BotMcvExpansionMode.CheckBase);
						break;

					case BotMcvExpansionMode.CheckBase:
						SwitchExpansionMode(BotMcvExpansionMode.CheckResourceCreator);
						maxFailedAttempts = NegativeMaxFailedAttempts;
						break;
				}
			}
		}

		void FindGoodDeploySpot()
		{
			lastFailedCheckSpot = null;

			if (!Info.ExpansionModeAutoSwitch)
			{
				if (--failedAttempts <= -maxFailedAttempts)
					failedAttempts = -maxFailedAttempts;
				return;
			}

			if (--failedAttempts <= -maxFailedAttempts)
			{
				maxFailedAttempts = PositiveMaxFailedAttempts;
				switch (mcvExpansionMode)
				{
					case BotMcvExpansionMode.CheckResourceCreator:
						failedAttempts = -maxFailedAttempts;
						break;

					case BotMcvExpansionMode.CheckBase:
						failedAttempts = maxFailedAttempts - 1;
						SwitchExpansionMode(BotMcvExpansionMode.CheckResource);
						break;

					case BotMcvExpansionMode.CheckResource:
						failedAttempts = maxFailedAttempts - 1;
						SwitchExpansionMode(BotMcvExpansionMode.CheckResourceCreator);
						break;
				}
			}
		}

		public (CPos? ExpandLocation, int Attraction, CPos? CheckSpot, int PFCellsCount) GetExpansionCenter(Actor mcv, Mobile mobile, bool allowfallback)
		{
			/*
			 * indiceSideLengthSquare (which is equal to indiceSideLength * indiceSideLength) is used as the basic unit to calculate the attraction of a candidate,
			 * we  compare the attraction on the same scale on different factors, such as candidate's distance to current MCV and ally construction yard & refinery within range, etc:
			 *
			 * 1). the weight of candidate's distance-square to current MCV
			 *
			 *     a) if not Mobile: range from 0 to -indiceSideLengthSquare.
			 *
			 *     The reason why:
			 *
			 *     It is calculated as "(candidate - mcv.Location).LengthSquared / pathDistanceSquareFactor".
			 *     note that: pathDistanceSquareFactor = resourceMapIndicesColumnCount * resourceMapIndicesColumnCount + resourceMapIndicesRowCount * resourceMapIndicesRowCount,
			 *
			 *     Consider a map, we divide it at the length of indiceSideLength = r, and then its resourceMapIndicesColumnCount = a, resourceMapIndicesRowCount = b,
			 *     so the map.width ≈ a*r, map.height ≈ b*r,
			 *     the maximum euclid distance-square between two points on the map is (a*r)(a*r) + (b*r)(b*r),
			 *     so the maximum "weight of candidate's distance to current MCV" is from 0 to -((a*r)(a*r) + (b*r)(b*r)) / (a*a + b*b) = -r*r = -indiceSideLengthSquare.
			 *
			 *     b) if Mobile: range depends on pathfinding distance in cell.
			 *
			 *     It is calculated as "pathfindDistance * pathfindDistance / pathDistanceSquareFactor".
			 *
			 * 2). the weight of friendly construction yard within range: -indiceSideLengthSquare. If it belongs to an ally, -indiceSideLengthSquare/2.
			 *
			 * 3). the weight of enemy building within range: -indiceSideLengthSquare*4.
			 *
			 * 4). the weight of friendly refinery within range (not for CheckBase mode): -indiceSideLengthSquare. If it belongs to an ally, -indiceSideLengthSquare/2.
			 *
			 * 5). the weight of resource amount (only for CheckResource mode): from 0 to +indiceSideLengthSquare/8.
			 *
			 *     The reason why:
			 *
			 *     The maximum resource amount in a indice of resource map is approximately indiceSideLengthSquare (full of it), but a stride full of resources is less likely to
			 *     have room for buildings. So we prefer the indice have half of resource cells the most, which may give us enough room to place buildings.
			 *
			 *     so the weight can be: (indiceSideLengthSquare/2) - |(indiceResourceCellCount - (indiceSideLengthSquare/2))|, range from (0 to +indiceSideLengthSquare/2).
			 *
			 *     Note: In practive resource weight is not very important, we cannot let MCV go a long way just for a rich resource spot.
			 *     We have to take only 1/4 of it, wich is (0 to +indiceSideLengthSquare/8),
			 *     and apply some additional method to filter the indice for acceptable resource (not too low).
			 */
			var indiceSideLengthSquare = indiceSideLength * indiceSideLength;
			switch (mcvExpansionMode)
			{
				/*
				 * CheckBase mode only considers the distance to current MCV, ally construction yard within range and enemy buildings within range.
				 * Attaction has a base value of indiceSideLengthSquare >> 3 (1/8 of the maximum distance weight, 1/(2*sqrt(2))≈ 1/2.8 of the maximum distance in map)
				 */
				case BotMcvExpansionMode.CheckBase:
					var cb_conyardlocs = world.ActorsHavingTrait<Building>().Where(a => a.Owner.IsAlliedWith(player)
						&& Info.ConstructionYardTypes.Contains(a.Info.Name)).Select(a => (a.Location, a.Owner == player)).ToArray();

					CPos? cb_suitablespot = null;
					CPos? cb_checkspot = null;
					var cb_best = int.MinValue;
					var cb_pfcount = -1;

					foreach (var (indiceCenter, value, rescenter) in resourceMapIndices)
					{
						if (lastFailedCheckSpot == indiceCenter)
							continue;

						var attraction = 0;
						var pfcount = -1;
						if (mobile == null)
						{
							attraction = indiceSideLengthSquare >> 4;
							attraction -= (rescenter - mcv.Location).LengthSquared / pathDistanceSquareFactor;
							pfcount = -1;
						}
						else
						{
							attraction = indiceSideLengthSquare >> 3;

							var path = pathfinder.FindPathToTargetCells(mcv, mcv.Location, [rescenter], BlockedByActor.Immovable);

							if (path == PathFinder.NoPath)
								continue;

							pfcount = path.Count;
							attraction -= pfcount * pfcount / pathDistanceSquareFactor;
						}

						if (world.FindActorsInCircle(world.Map.CenterOfCell(indiceCenter), WDist.FromCells(Info.CBmodeEnemyBaseScanRadius)).Any(a => !a.Disposed
							&& (player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy)
							&& a.Info.HasTraitInfo<BuildingInfo>()
							&& a.Info.HasTraitInfo<SellableInfo>()))
							attraction -= indiceSideLengthSquare << 3;

						foreach (var (location, isAlly) in cb_conyardlocs)
						{
							var sdistance = (indiceCenter - location).LengthSquared;
							if (sdistance <= indiceSideLengthSquare)
							{
								if (isAlly)
									attraction -= indiceSideLengthSquare;
								else
									attraction -= indiceSideLengthSquare << 1;
							}
						}

						if (!allowfallback)
						{
							var sdistance = (indiceCenter - mcv.Location).LengthSquared;
							if (sdistance <= indiceSideLengthSquare)
								attraction -= indiceSideLengthSquare << 1;
						}

						if (attraction > cb_best)
						{
							cb_best = attraction;
							cb_checkspot = indiceCenter;
							cb_suitablespot = indiceCenter;
							cb_pfcount = pfcount;
						}
					}

					return (cb_suitablespot ?? mcv.Location, cb_best, cb_checkspot, cb_pfcount);

				/*
				 * CheckResource mode considers the distance to current MCV, ally construction yard & refinery within range,
				 * Attaction has a base value of:
				 * 1. if not Mobile: indiceSideLengthSquare >> 4 (1/16 of the maximum distance weight, = 0.25 of the maximum euclid distance in map)
				 * 2. if Mobile: indiceSideLengthSquare >> 3 (1/8 of the maximum distance weight, ≈ 0.35 of the maximum euclid distance in map)
				 */
				case BotMcvExpansionMode.CheckResource:

					var cr_refinarylocs = world.ActorsHavingTrait<Refinery>().Where(a => a.Owner == player && Info.RefineryTypes.Contains(a.Info.Name))
						.Select(a => (a.Location, a.Owner != player))
						.ToArray();

					var cr_conyardlocs = world.ActorsHavingTrait<Building>().Where(a => a.Owner.IsAlliedWith(player)
						&& Info.ConstructionYardTypes.Contains(a.Info.Name)).Select(a => (a.Location, a.Owner != player)).ToArray();

					// We only take indice has more than half of average indice value (in weight calculation), to skip the indice with very poor resource
					// when failedAttempts is acceptable.
					var thresholdRes = (resourceMapIndices.Sum(i => (indiceSideLengthSquare >> 1) - Math.Abs(i.Value - (indiceSideLengthSquare >> 1)))
						/ resourceMapIndices.Length) >> 1;

					CPos? cr_suitablespot = null;
					CPos? cr_checkspot = null;
					var cr_best = int.MinValue;
					var cr_pfcount = -1;

					foreach (var (indiceCenter, value, rescenter) in resourceMapIndices)
					{
						if ((failedAttempts > maxFailedAttempts >> 1 && value <= thresholdRes) || lastFailedCheckSpot == indiceCenter)
							continue;

						var attraction = 0;
						var pfcount = 0;
						if (mobile == null)
						{
							attraction = indiceSideLengthSquare >> 4;
							attraction -= (rescenter - mcv.Location).LengthSquared / pathDistanceSquareFactor;
							pfcount = -1;
						}
						else
						{
							attraction = indiceSideLengthSquare >> 3;

							var path = pathfinder.FindPathToTargetCells(mcv, mcv.Location, [rescenter], BlockedByActor.Immovable);

							if (path == PathFinder.NoPath)
								continue;

							pfcount = path.Count;
							attraction -= pfcount * pfcount / pathDistanceSquareFactor;
						}

						// it is better that resource cells takes only half of the indice cells, which give us the place to place building.
						attraction += ((indiceSideLengthSquare >> 1) - Math.Abs(value - (indiceSideLengthSquare >> 1))) >> 2;

						if (world.FindActorsInCircle(world.Map.CenterOfCell(rescenter), WDist.FromCells(Info.CRmodeEnemyBaseScanRadius)).Any(a => !a.Disposed
							&& (player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy)
							&& a.Info.HasTraitInfo<BuildingInfo>()
							&& a.Info.HasTraitInfo<SellableInfo>()))
							attraction -= indiceSideLengthSquare << 3;

						foreach (var (location, isAlly) in cr_refinarylocs)
						{
							var sdistance = (rescenter - location).LengthSquared;
							if (sdistance <= Info.CRmodeRefineryUnfavorRange * Info.CRmodeRefineryUnfavorRange)
							{
								if (isAlly)
									attraction -= indiceSideLengthSquare;
								else
									attraction -= indiceSideLengthSquare << 1;
							}
						}

						foreach (var (location, isAlly) in cr_conyardlocs)
						{
							var sdistance = (rescenter - location).LengthSquared;
							if (sdistance <= Info.CRmodeConyardUnfavorRange * Info.CRmodeConyardUnfavorRange)
							{
								if (isAlly)
									attraction -= indiceSideLengthSquare;
								else
									attraction -= indiceSideLengthSquare << 1;
							}
						}

						if (!allowfallback)
						{
							var sdistance = (rescenter - mcv.Location).LengthSquared;
							if (sdistance <= Info.CRmodeConyardUnfavorRange * Info.CRmodeConyardUnfavorRange)
								attraction -= indiceSideLengthSquare << 1;
						}

						if (attraction > cr_best)
						{
							cr_best = attraction;
							cr_checkspot = indiceCenter;
							cr_suitablespot = rescenter;
							cr_pfcount = pfcount;
						}
					}

					if (cr_suitablespot == null)
						return (null, int.MinValue, null, -1);

					if (failedAttempts < maxFailedAttempts >> 1)
						return (cr_suitablespot, cr_best, cr_checkspot, cr_pfcount);
					else
						return (world.Map.FindTilesInAnnulus(cr_suitablespot.Value, 0, indiceResourceScanRadius)
							.Where(c => Info.ValidResourceTypes.Contains(resourceLayer.GetResource(c).Type))
							.Random(world.LocalRandom), cr_best, cr_checkspot, cr_pfcount);
				/*
				 * CheckResourceCreator mode considers the distance to current MCV, ally construction yard & refinery within range,
				 * Attaction has a base value of:
				 * 1. if not Mobile: (indiceSideLengthSquare >> 3) - (indiceSideLengthSquare >> 5) (3/32 of the maximum distance weight, ≈ 0.31 of the maximum euclid distance in map))
				 * 2. if Mobile: (indiceSideLengthSquare >> 2)  - (indiceSideLengthSquare >> 4) (3/16 of the maximum distance weight, ≈ 0.43 of the maximum euclid distance in map)
				 */
				case BotMcvExpansionMode.CheckResourceCreator:

					var crc_conyardlocs = world.ActorsHavingTrait<Building>().Where(a => a.Owner.IsAlliedWith(player)
						&& Info.ConstructionYardTypes.Contains(a.Info.Name)).Select(a => (a.Location, a.Owner != player)).ToArray();

					var crc_refinarylocs = world.ActorsHavingTrait<Refinery>().Where(a => a.Owner.IsAlliedWith(player) && Info.RefineryTypes.Contains(a.Info.Name))
						.Select(a => (a.Location, a.Owner != player))
						.ToArray();

					var crc_rescreators = world.ActorsHavingTrait<SeedsResource>().Where(a => Info.ResourceCreatorTypes.Contains(a.Info.Name));

					CPos? crc_suitablelocation = null;
					CPos? crc_checkspot = null;
					var crc_best = int.MinValue;
					var crc_pfcount = -1;

					foreach (var rescreator in crc_rescreators)
					{
						if (lastFailedCheckSpot == rescreator.Location)
							continue;

						var attraction = 0;
						var pfcount = 0;
						if (mobile == null)
						{
							attraction = (indiceSideLengthSquare >> 3) - (indiceSideLengthSquare >> 5);
							attraction -= (rescreator.Location - mcv.Location).LengthSquared / pathDistanceSquareFactor;
							pfcount = -1;
						}
						else
						{
							attraction = (indiceSideLengthSquare >> 2) - (indiceSideLengthSquare >> 4);

							var path = pathfinder.FindPathToTargetCells(mcv, mcv.Location, [rescreator.Location], BlockedByActor.Immovable, ignoreActor: rescreator);

							if (path == PathFinder.NoPath)
								continue;

							pfcount = path.Count;
							attraction -= pfcount * pfcount / pathDistanceSquareFactor;
						}

						if (world.FindActorsInCircle(rescreator.CenterPosition, WDist.FromCells(Info.CRCmodeEnemyBaseScanRadius)).Any(a => !a.Disposed
							&& (player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy)
							&& a.Info.HasTraitInfo<BuildingInfo>()
							&& a.Info.HasTraitInfo<SellableInfo>()))
							attraction -= indiceSideLengthSquare << 3;

						foreach (var (location, isAlly) in crc_refinarylocs)
						{
							var sdistance = (rescreator.Location - location).LengthSquared;
							if (sdistance <= Info.CRCmodeRefineryUnfavorRange * Info.CRCmodeRefineryUnfavorRange)
							{
								if (isAlly)
									attraction -= indiceSideLengthSquare;
								else
									attraction -= indiceSideLengthSquare << 1;
							}
						}

						foreach (var (location, isAlly) in crc_conyardlocs)
						{
							var sdistance = (rescreator.Location - location).LengthSquared;
							if (sdistance <= Info.CRCmodeConyardUnfavorRange * Info.CRCmodeConyardUnfavorRange)
							{
								if (isAlly)
									attraction -= indiceSideLengthSquare;
								else
									attraction -= indiceSideLengthSquare << 1;
							}
						}

						if (!allowfallback)
						{
							var sdistance = (rescreator.Location - mcv.Location).LengthSquared;
							if (sdistance <= indiceSideLengthSquare)
								attraction -= indiceSideLengthSquare << 1;
						}

						if (attraction > crc_best)
						{
							crc_best = attraction;
							crc_checkspot = rescreator.Location;
							crc_suitablelocation = rescreator.Location;
							crc_pfcount = pfcount;
						}
					}

					return (crc_suitablelocation, crc_best, crc_checkspot, crc_pfcount);

				default:
					return (null, int.MinValue, null, -1);
			}
		}

		void IBotTick.BotTick(IBot bot)
		{
			attackrespondcooldown--;

			if (firstTick)
			{
				var resourceSum = 0;

				if (resourceMapIndices == null)
				{
					var map = world.Map;
					var actualMapWidth = map.Bounds.Width;
					var actualMapHeight = map.Bounds.Height;
					var xoffset = map.Bounds.X;
					var yoffset = map.Bounds.Y;

					resourceMapIndicesColumnCount = (actualMapWidth + indiceSideLength - 1) / indiceSideLength;
					resourceMapIndicesRowCount = (actualMapHeight + indiceSideLength - 1) / indiceSideLength;
					resourceMapIndices = Exts.MakeArray(resourceMapIndicesColumnCount * resourceMapIndicesRowCount, i => (new MPos(
						xoffset + i % resourceMapIndicesColumnCount * indiceSideLength + (indiceSideLength >> 1),
						yoffset + i / resourceMapIndicesColumnCount * indiceSideLength + (indiceSideLength >> 1)).ToCPos(map), 0, CPos.Zero))
						.Shuffle(world.LocalRandom).ToArray();

					// Note: we can only get map resource data in IBotTick.BotTick, instead of TraitEnabled or Created.
					for (var i = 0; i < resourceMapIndices.Length; i++)
					{
						UpdateResourceMap(i);
						resourceSum += resourceMapIndices[i].Value;
					}

					pathDistanceSquareFactor = resourceMapIndicesColumnCount * resourceMapIndicesColumnCount + resourceMapIndicesRowCount * resourceMapIndicesRowCount;
				}

				// check which mode we should use in map
				if (Info.InitialExpansionMode == BotMcvExpansionMode.CheckResourceCreator && world.ActorsHavingTrait<SeedsResource>().Any())
					SwitchExpansionMode(BotMcvExpansionMode.CheckResourceCreator);
				else if (Info.InitialExpansionMode != BotMcvExpansionMode.CheckBase && resourceSum > 0)
					SwitchExpansionMode(BotMcvExpansionMode.CheckResource);
				else
					SwitchExpansionMode(BotMcvExpansionMode.CheckBase);

				DeployMcvs(bot, false);
				firstTick = false;
			}

			if (--scanInterval <= 0)
			{
				scanInterval = Info.ScanForNewMcvInterval;
				DeployMcvs(bot, true);
			}

			if (--buildMCVInterval <= 0)
			{
				buildMCVInterval = Info.BuildMcvInterval;
				BuildMCV(bot);
			}

			if (--updateResourceMapInterval <= 0)
			{
				updateResourceMapInterval = updateResourceMapDelay;
				UpdateResourceMap(updateResourceMapIndex);
				updateResourceMapIndex = (updateResourceMapIndex + 1) % resourceMapIndices.Length;
			}

			if (--moveConyardInterval <= 0)
			{
				moveConyardInterval = Info.MoveConyardTick;
				UnDeployConyard(bot);
			}
		}

		void UpdateResourceMap(int index)
		{
			if (resourceLayer == null || resourceMapIndices == null || resourceMapIndices.Length == 0)
				return;

			var sumCellsX = 0;
			var sumCellsY = 0;
			var resTiles = world.Map.FindTilesInAnnulus(resourceMapIndices[index].IndiceCenter, 0, indiceResourceScanRadius).Where(c =>
			{
				if (!Info.ValidResourceTypes.Contains(resourceLayer.GetResource(c).Type))
					return false;

				sumCellsX += c.X;
				sumCellsY += c.Y;
				return true;
			}).ToList();

			if (resTiles.Count != 0)
			{
				var resAvgCell = new CPos(sumCellsX / resTiles.Count, sumCellsY / resTiles.Count);
				var bestCell = resTiles[0];
				var bestDist = (bestCell - resAvgCell).LengthSquared;
				foreach (var c in resTiles)
				{
					var dist = (c - resAvgCell).LengthSquared;
					if (dist < bestDist)
					{
						bestDist = dist;
						bestCell = c;
					}
				}

				resourceMapIndices[index] = (resourceMapIndices[index].IndiceCenter, resTiles.Count, bestCell);
			}
			else
				resourceMapIndices[index] = (resourceMapIndices[index].IndiceCenter, 0, CPos.Zero);
		}

		void BuildMCV(IBot bot)
		{
			var conyardNum = AIUtils.CountActorByCommonName(constructionYards);
			var mcvNum = AIUtils.CountActorByCommonName(mcvs);

			// Only build MCV if we have no mcv in the field (make it an exception if have no conyard),
			// don't have one in production and don't have the desired number of construction yards
			if ((conyardNum <= 0 && mcvNum > 1) || (conyardNum > 0 && mcvNum > 0)
				|| conyardNum + mcvNum >= Info.MinimumConstructionYardCount || AIUtils.CountActorByCommonName(mcvFactories) <= 0
				|| mcvFactories.Actors.Any(a => !a.IsDead && a.TraitsImplementing<ProductionQueue>().Any(t => t.Enabled && t.AllQueued()
				.Any(q => Info.McvTypes.Contains(q.Item)))) || player.PlayerActor.TraitsImplementing<ProductionQueue>().Any(t => t.Enabled && t.AllQueued()
				.Any(q => Info.McvTypes.Contains(q.Item))) || Info.McvTypes.Count <= 0)
				return;

			var unitBuilder = requestUnitProduction.FirstEnabledTraitOrDefault();
			if (unitBuilder == null)
				return;
			var mcvType = Info.McvTypes.Random(world.LocalRandom);
			if (unitBuilder.RequestedProductionCount(bot, mcvType) <= 0)
				unitBuilder.RequestUnitProduction(bot, mcvType);
		}

		void DeployMcvs(IBot bot, bool chooseLocation)
		{
			var newMCVs = world.ActorsHavingTrait<Transforms>()
				.Where(a => a.Owner == player && a.IsIdle && Info.McvTypes.Contains(a.Info.Name));

			foreach (var mcv in newMCVs)
				DeployMcv(bot, mcv, chooseLocation);
		}

		void UnDeployConyard(IBot bot)
		{
			if (firstUndeploy)
			{
				if (world.ActorsHavingTrait<Building>().Count(a => a.Owner == player && Info.ProductionTypes.Contains(a.Info.Name)) >= 2
					&& world.ActorsHavingTrait<Refinery>().Any(a => a.Owner == player && Info.RefineryTypes.Contains(a.Info.Name)))
				{
					var idleconyards = constructionYards.Actors.Where(a => !a.IsDead).ToList();

					if (idleconyards.Count > 0)
					{
						bot.QueueOrder(new Order("DeployTransform", idleconyards[0], true));
						allowfallback = false;
					}
				}

				firstUndeploy = false;
				moveConyardInterval = world.LocalRandom.Next(Info.MoveConyardTick, Info.MoveConyardTick * 2);
			}
			else
			{
				var conyards = constructionYards.Actors
					.Where(a => !a.IsDead).OrderBy(a => a.ActorID)
					.ToList();

				if (conyards.Count > 1)
				{
					var movableMCV = conyards.FirstOrDefault(a => !a.TraitsImplementing<ProductionQueue>()
					.Any(t => t.Enabled && t.AllQueued().Any(q => Info.RefineryTypes.Contains(q.Item))));

					if (movableMCV != null)
						bot.QueueOrder(new Order("DeployTransform", movableMCV, true));
				}
			}
		}

		// Find any MCV and deploy them at a sensible location.
		void DeployMcv(IBot bot, Actor mcv, bool move)
		{
			if (move)
			{
				var transformsInfo = mcv.Info.TraitInfo<TransformsInfo>();
				var desiredLocation = ChooseMcvDeployLocation(mcv, transformsInfo.IntoActor, transformsInfo.Offset, allowfallback);
				if (desiredLocation == null)
					return;

				bot.QueueOrder(new Order("Move", mcv, Target.FromCell(world, desiredLocation.Value), true));

				allowfallback = true;

				if (constructionYards.Actors.Any(a => !a.IsDead))
				{
					foreach (var n in notifyPositionsUpdated)
					{
						n.UpdatedBaseCenter(desiredLocation.Value);
						n.UpdatedDefenseCenter(desiredLocation.Value);
					}
				}
			}

			bot.QueueOrder(new Order("DeployTransform", mcv, true));
		}

		// First, find a suitable expansion location according to current mode,
		// Then, find a deployable cell around it.
		CPos? ChooseMcvDeployLocation(Actor mcv, string transformIntoType, CVec offset, bool allowfallback)
		{
			var actorInfo = world.Map.Rules.Actors[transformIntoType];
			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (bi == null)
				return null;

			Mobile mobile = null;
			if (mcv.TraitOrDefault<IMove>() == null)
				return null;
			else
				mobile = mcv.TraitOrDefault<Mobile>();

			var (expandCenter, attraction, checkspot, pfcount) = GetExpansionCenter(mcv, mobile, allowfallback);

			// Find the deployable cell
			CPos? FindDeployCell(CPos? sourceCell, CPos? targetCell, int minRange, int maxRange, int tryMaintainRange, int pfcount)
			{
				if (!sourceCell.HasValue || !targetCell.HasValue)
					return null;

				var target = targetCell.Value;
				var source = sourceCell.Value;

				var cells = world.Map.FindTilesInAnnulus(target, minRange, maxRange).Where(c => world.CanPlaceBuilding(c + offset, actorInfo, bi, null));

				/* First, sort the cells that keep tryMaintainRange to target (meanwhile direction is from center to target) the first to be considered
				 * by using following code. The idea is to use a linear combination of two distances-square for sorting weight.
				 *
				 * See comments in https://github.com/OpenRA/OpenRA/pull/22028#issuecomment-3242518793 for explaination.
				 */
				if (source != target)
				{
					var theta = tryMaintainRange;
					var deta = (pfcount < 0 ? (source - target).Length : pfcount) - tryMaintainRange;

					return cells.OrderBy(c =>
					{
						var c2target = pathfinder.FindPathToTargetCells(mcv, c, [target], BlockedByActor.Immovable);
						if (c2target == PathFinder.NoPath)
							return int.MaxValue;
						var c2source = (c - source).LengthSquared;
						return deta * c2target.Count * c2target.Count + theta * c2source;
					}).FirstOrDefault();
				}
				else
					return cells.Shuffle(world.LocalRandom).FirstOrDefault();
			}

			var bc = FindDeployCell(mcv.Location, expandCenter, mcvDeploymentMinDeployRadius, mcvDeploymentMaxDeployRadius, mcvDeploymentTryMaintainRange, pfcount);

			// At last, if the attraction of the found expansion location is good enough (>0) and deploy cell found,
			// we consider it as a good expansion, otherwise, we consider it as a bad expansion.
			if (bc.HasValue && attraction > 0)
				FindGoodDeploySpot();
			else
				FindBadDeploySpot(bc.HasValue ? null : checkspot);

			return bc;
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (attackrespondcooldown <= 0 && Info.McvTypes.Contains(self.Info.Name))
			{
				attackrespondcooldown = 20;

				var transformsInfo = self.Info.TraitInfo<TransformsInfo>();
				var actorInfo = world.Map.Rules.Actors[transformsInfo.IntoActor];
				var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
				if (bi == null)
					return;

				if (world.CanPlaceBuilding(self.Location + transformsInfo.Offset, actorInfo, bi, null))
					bot.QueueOrder(new Order("DeployTransform", self, false));

				if (AIUtils.CountActorByCommonName(constructionYards) == 0)
				{
					foreach (var n in notifyPositionsUpdated)
						n.UpdatedBaseCenter(self.Location);
				}
			}
		}
	}
}
