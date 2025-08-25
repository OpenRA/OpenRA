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
	public enum BotMcvExpansionMode { CheckResource, CheckBase, CheckCurrentLocation }

	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI MCVs and expansion.")]
	public class McvExpansionManagerBotModuleInfo : ConditionalTraitInfo, Requires<ResourceMapBotModuleInfo>, NotBefore<ResourceMapBotModuleInfo>
	{
		[Desc("Actor types that are considered MCVs (deploy into base builders).")]
		public readonly HashSet<string> McvTypes = [];

		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = [];

		[Desc("Actor types that are able to produce MCVs.")]
		public readonly HashSet<string> McvFactoryTypes = [];

		[Desc("Try to maintain at least this many ConstructionYardTypes, build an MCV if number is below this.")]
		public readonly int MinimumConstructionYardCount = 1;

		[Desc("Try to maintain at additional this many ConstructionYardTypes.")]
		public readonly int AdditionalConstructionYardCount = 0;

		[Desc("Build additional MCV if cash is above this.")]
		public readonly int BuildAdditionalMCVCashAmount = 5000;

		[Desc("Delay (in ticks) for giving orders to idle MCVs.")]
		public readonly int ScanForNewMcvInterval = 20;

		[Desc("Delay (in ticks) for checking and building a MCV.")]
		public readonly int BuildMcvInterval = 101;

		[Desc("Delay (in ticks) for moving a conyard to better expansion. Only work with more than 1 conyard.")]
		public readonly int MoveConyardTick = 4000;

		[Desc("Should moving the oldest or newest conyard be preferred? Random ordering if unset.")]
		public readonly bool? MoveOldConyardFirst = null;

		[Desc("Initial expansion mode chosen by AI.")]
		public readonly BotMcvExpansionMode InitialExpansionMode = BotMcvExpansionMode.CheckResource;

		[Desc("Allow the bot to switch expansion mode automatically on enough failure or successful attempts.")]
		public readonly bool ExpansionModeAutoSwitch = true;

		/* those are CheckResource mode options */
		[Desc("Minimum distance (in cells) from the found resource creator location when checking for MCV deployment location.")]
		public readonly int CRmodeMinDeployRadius = 2;

		[Desc("Maximum distance (in cells) the found resource creator location when checking for MCV deployment location.")]
		public readonly int CRmodeMaxDeployRadius = 20;

		[Desc("When moving to a resource, what distance (in cells) to resource should we attempt to maintain?")]
		public readonly int CRmodeTryMaintainRange = 8;

		[Desc("Distance (in cells) to avoid a friendly conyard when choosing an expansion location.",
					"Recommended to set it equal or larger than ResourceMapStrideRadius.")]
		public readonly int CRmodeFriendlyConyardDislikeRange = 14;

		[Desc("Distance (in cells) to avoid a friendly refinery when choosing an expansion location.",
					"Recommended to set it equal or larger than ResourceMapStrideRadius.")]
		public readonly int CRmodeFriendlyRefineryDislikeRange = 14;

		/* those are CheckBase mode options */
		[Desc("Minimum distance (in cells) from center of the base expansion when checking for MCV deployment location.")]
		public readonly int CBmodeMinDeployRadius = 2;

		[Desc("Maximum distance (in cells) from center of the base expansion when checking for MCV deployment location.")]
		public readonly int CBmodeMaxDeployRadius = 20;

		public override object Create(ActorInitializer init) { return new McvExpansionManagerBotModule(init.Self, this); }
	}

	public class McvExpansionManagerBotModule :
		ConditionalTrait<McvExpansionManagerBotModuleInfo>,
		IBotTick,
		IBotRespondToAttack,
		IBotBaseExpansion,
		INotifyActorDisposing
	{
		// When ExpansionModeAutoSwitch is true, if the AI fails to find a deploy spot enough time even in CheckBase mode
		// NegativeMaxFailedAttempts is applied to make AI switch bettween modes more frequently until a successful attempt
		const int CRmodPositiveMaxFailedAttempts = 3;
		const int CBmodPositiveMaxFailedAttempts = 2;
		const int NegativeMaxFailedAttempts = 0;

		readonly World world;
		readonly Player player;
		readonly ActorIndex.OwnerAndNamesAndTrait<TransformsInfo> mcvs;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> constructionYards;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> mcvFactories;

		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotRequestUnitProduction[] requestUnitProduction;
		IBotSuggestRefineryProduction[] suggestRefineryProduction;

		readonly Dictionary<Actor, CPos> activeMCVs = [];

		PathFinder pathfinder;
		ResourceMapBotModule resourceMapModule;
		PlayerResources playerResources;
		Actor mustUndeployCoyard;

		int scanInterval;
		int buildMCVInterval;
		int moveConyardInterval;
		bool firstTick = true;
		bool undeployEvenNoBase = false;
		bool allowfallback = true;

		BotMcvExpansionMode mcvExpansionMode;
		int mcvDeploymentMinDeployRadius;
		int mcvDeploymentMaxDeployRadius;
		int mcvDeploymentTryMaintainRange;
		int maxFailedAttempts;

		int failedAttempts;
		CPos? lastFailedCheckSpot;

		// It is unnecessary to respond every tick, we only need to respond once in a while.
		int attackrespondcooldown = 20;

		int pathDistanceSquareFactor;

		public McvExpansionManagerBotModule(Actor self, McvExpansionManagerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			mcvs = new ActorIndex.OwnerAndNamesAndTrait<TransformsInfo>(world, info.McvTypes, player);
			constructionYards = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.ConstructionYardTypes, player);
			mcvFactories = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.McvFactoryTypes, player);
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query player traits from self, which refers
			// for bot modules always to the Player actor.
			notifyPositionsUpdated = self.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			requestUnitProduction = self.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
			suggestRefineryProduction = self.TraitsImplementing<IBotSuggestRefineryProduction>().ToArray();
			pathfinder = world.WorldActor.Trait<PathFinder>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			scanInterval = world.LocalRandom.Next(Info.ScanForNewMcvInterval, Info.ScanForNewMcvInterval << 1);
			buildMCVInterval = world.LocalRandom.Next(Info.BuildMcvInterval, Info.BuildMcvInterval << 1);
			moveConyardInterval = world.LocalRandom.Next(Info.MoveConyardTick, Info.MoveConyardTick << 1);
		}

		void SwitchExpansionMode(BotMcvExpansionMode nextMode)
		{
			mcvExpansionMode = nextMode;
			switch (nextMode)
			{
				case BotMcvExpansionMode.CheckResource:
					mcvDeploymentMinDeployRadius = Info.CRmodeMinDeployRadius;
					mcvDeploymentMaxDeployRadius = Info.CRmodeMaxDeployRadius;
					mcvDeploymentTryMaintainRange = Info.CRmodeTryMaintainRange;
					break;

				case BotMcvExpansionMode.CheckBase:
					mcvDeploymentMinDeployRadius = Info.CBmodeMinDeployRadius;
					mcvDeploymentMaxDeployRadius = Info.CBmodeMaxDeployRadius;
					mcvDeploymentTryMaintainRange = (Info.CBmodeMaxDeployRadius + Info.CBmodeMinDeployRadius) >> 1;
					break;

				case BotMcvExpansionMode.CheckCurrentLocation:
					mcvDeploymentMinDeployRadius = Info.CBmodeMinDeployRadius;
					mcvDeploymentMaxDeployRadius = Info.CBmodeMaxDeployRadius;
					mcvDeploymentTryMaintainRange = 0;
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
					case BotMcvExpansionMode.CheckResource:
						SwitchExpansionMode(BotMcvExpansionMode.CheckBase);
						break;

					case BotMcvExpansionMode.CheckBase:
						SwitchExpansionMode(BotMcvExpansionMode.CheckResource);
						maxFailedAttempts = NegativeMaxFailedAttempts;
						break;

					case BotMcvExpansionMode.CheckCurrentLocation:
						SwitchExpansionMode(BotMcvExpansionMode.CheckResource);
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
				switch (mcvExpansionMode)
				{
					case BotMcvExpansionMode.CheckResource:
						maxFailedAttempts = CRmodPositiveMaxFailedAttempts;
						failedAttempts = -maxFailedAttempts;
						break;

					case BotMcvExpansionMode.CheckBase:
						maxFailedAttempts = CRmodPositiveMaxFailedAttempts;
						failedAttempts = maxFailedAttempts - 1;
						SwitchExpansionMode(BotMcvExpansionMode.CheckResource);
						break;

					case BotMcvExpansionMode.CheckCurrentLocation:
						maxFailedAttempts = CBmodPositiveMaxFailedAttempts;
						failedAttempts = maxFailedAttempts - 1;
						SwitchExpansionMode(BotMcvExpansionMode.CheckBase);
						break;
				}
			}
		}

		public (CPos? ExpandLocation, int Attraction, CPos? CheckSpot) GetExpansionCenter(Actor mcv, Mobile mobile, bool allowfallback)
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
			 * 3). the weight of enemy high threat within range: -indiceSideLengthSquare*8, otherwise -indiceSideLengthSquare/64
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
			var indiceSideLengthSquare = resourceMapModule.GetIndiceSideLength() * resourceMapModule.GetIndiceSideLength();
			switch (mcvExpansionMode)
			{
				/*
				 * CheckBase mode only considers the distance to current MCV, ally construction yard within range and enemy buildings within range.
				 * Attaction has a base value of indiceSideLengthSquare >> 3 (1/8 of the maximum distance weight, 1/(2*sqrt(2))≈ 1/2.8 of the maximum distance in map)
				 */
				case BotMcvExpansionMode.CheckBase:
					var cb_conyardlocs = world.ActorsHavingTrait<Building>()
						.Where(a => a.Owner.IsAlliedWith(player) && Info.ConstructionYardTypes.Contains(a.Info.Name))
						.Select(a => (a.Location, a.Owner == player))
						.ToArray();
					CPos? cb_suitablespot = null;
					CPos? cb_checkspot = null;
					var cb_best = int.MinValue;

					for (var i = 0; i < resourceMapModule.GetIndicesLength(); i++)
					{
						var indiceCenter = resourceMapModule.GetIndice(i).IndiceCenter;

						if (lastFailedCheckSpot == indiceCenter)
							continue;

						var attraction = indiceSideLengthSquare >> 1;

						attraction -= (indiceCenter - mcv.Location).LengthSquared / pathDistanceSquareFactor;

						attraction -= CalculateThreats(indiceSideLengthSquare, i);

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

						foreach (var (othermcv, dest) in activeMCVs)
						{
							if (dest == indiceCenter && othermcv != mcv)
								attraction -= indiceSideLengthSquare << 1;
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
						}
					}

					return (cb_suitablespot ?? mcv.Location, cb_best, cb_checkspot);

				/*
				 * CheckResource mode considers the distance to current MCV, ally construction yard & refinery within range,
				 * Attaction has a base value of:
				 * 1. if not Mobile: indiceSideLengthSquare >> 4 (1/16 of the maximum distance weight, = 0.25 of the maximum euclid distance in map)
				 * 2. if Mobile: indiceSideLengthSquare >> 3 (1/8 of the maximum distance weight, ≈ 0.35 of the maximum euclid distance in map)
				 */
				case BotMcvExpansionMode.CheckResource:

					var cr_refinarylocs = world.ActorsHavingTrait<Refinery>()
						.Where(a => a.Owner == player && resourceMapModule.Info.RefineryTypes.Contains(a.Info.Name))
						.Select(a => (a.Location, a.Owner != player))
						.ToArray();

					var cr_conyardlocs = world.ActorsHavingTrait<Building>()
						.Where(a => a.Owner.IsAlliedWith(player) && Info.ConstructionYardTypes.Contains(a.Info.Name))
						.Select(a => (a.Location, a.Owner != player))
						.ToArray();

					// We only take indice has more than half of average indice value (in weight calculation), to skip the indice with very poor resource
					// when failedAttempts is acceptable.
					var thresholdRes = 0;
					for (var i = 0; i < resourceMapModule.GetIndicesLength(); i++)
					{
						var resourceCellCounts = resourceMapModule.GetIndice(i).ResourceCellsCount;
						thresholdRes += (indiceSideLengthSquare >> 1) - Math.Abs(resourceCellCounts - (indiceSideLengthSquare >> 1));
					}

					thresholdRes = (thresholdRes / resourceMapModule.GetIndicesLength()) >> 1;

					CPos? cr_suitablespot = null;
					CPos? cr_checkspot = null;
					var cr_best = int.MinValue;

					for (var i = 0; i < resourceMapModule.GetIndicesLength(); i++)
					{
						var indice = resourceMapModule.GetIndice(i);
						var indiceCenter = indice.IndiceCenter;
						var resourceCellsCount = indice.ResourceCellsCount;
						var resourceCellsCenter = indice.ResourceCellsCenter;
						var resourceCreatorLocs = indice.ResourceCreatorLocs;

						if ((failedAttempts > maxFailedAttempts >> 1 && resourceCellsCount <= thresholdRes) || lastFailedCheckSpot == indiceCenter)
							continue;

						var attraction = 0;
						if (mobile == null)
						{
							attraction = indiceSideLengthSquare >> 2;
							attraction -= (resourceCellsCenter - mcv.Location).LengthSquared / pathDistanceSquareFactor;
						}
						else
						{
							attraction = indiceSideLengthSquare >> 1;

							var path = pathfinder.FindPathToTargetCells(mcv, mcv.Location, [resourceCellsCenter], BlockedByActor.None);

							if (path == PathFinder.NoPath)
								continue;

							attraction -= path.Count * path.Count / pathDistanceSquareFactor;
						}

						// it is better that resource cells takes only half of the indice cells, which give us the place to place building.
						attraction += ((indiceSideLengthSquare >> 1) - Math.Abs(resourceCellsCount - (indiceSideLengthSquare >> 1))) >> 2;
						attraction += 8 * resourceCreatorLocs.Length;

						var resCenter = resourceCreatorLocs.Length == 0 || world.LocalRandom.Next(2) > 0 ? resourceCellsCenter : resourceCreatorLocs.Random(world.LocalRandom);

						attraction -= CalculateThreats(indiceSideLengthSquare, i);

						foreach (var (location, isAlly) in cr_refinarylocs)
						{
							var sdistance = (resCenter - location).LengthSquared;
							if (sdistance <= Info.CRmodeFriendlyRefineryDislikeRange * Info.CRmodeFriendlyRefineryDislikeRange)
							{
								if (isAlly)
									attraction -= indiceSideLengthSquare;
								else
									attraction -= indiceSideLengthSquare << 1;
							}
						}

						foreach (var (location, isAlly) in cr_conyardlocs)
						{
							var sdistance = (resCenter - location).LengthSquared;
							if (sdistance <= Info.CRmodeFriendlyConyardDislikeRange * Info.CRmodeFriendlyConyardDislikeRange)
							{
								if (isAlly)
									attraction -= indiceSideLengthSquare;
								else
									attraction -= indiceSideLengthSquare << 1;
							}
						}

						foreach (var (othermcv, dest) in activeMCVs)
						{
							if (dest == indiceCenter)
								attraction -= indiceSideLengthSquare << 1;
						}

						if (!allowfallback)
						{
							var sdistance = (resCenter - mcv.Location).LengthSquared;
							if (sdistance <= Info.CRmodeFriendlyConyardDislikeRange * Info.CRmodeFriendlyConyardDislikeRange)
								attraction -= indiceSideLengthSquare << 1;
						}

						if (attraction > cr_best)
						{
							cr_best = attraction;
							cr_checkspot = indiceCenter;
							cr_suitablespot = resCenter;
						}
					}

					if (cr_suitablespot == null)
						return (null, int.MinValue, null);

					return (cr_suitablespot, cr_best, cr_checkspot);

				case BotMcvExpansionMode.CheckCurrentLocation:
					return (mcv.Location, int.MaxValue, null);

				default:
					return (null, int.MinValue, null);
			}
		}

		int CalculateThreats(int indiceSideLengthSquare, int index)
		{
			var baseIndice = resourceMapModule.GetIndice(index);

			var (indiceCount, nearbyEnemyBaseThreat, nearbyEnemyThreat) = resourceMapModule.GetNearbyIndicesThreat(index);

			var indiceEnemyBaseThreat = Math.Max(baseIndice.EnemyBaseCount - baseIndice.FriendlyBaseCount, 0);

			var indiceEnemyUnitThreat = Math.Max(baseIndice.EnemyUnitCount - baseIndice.FriendlyUnitCount, 0);

			if (indiceCount == 0)
				return (indiceEnemyUnitThreat * indiceSideLengthSquare >> 6) + (indiceEnemyBaseThreat * indiceSideLengthSquare << 3);

			return ((indiceEnemyUnitThreat + nearbyEnemyThreat / indiceCount) * indiceSideLengthSquare >> 6) +
							((indiceEnemyBaseThreat + nearbyEnemyBaseThreat / indiceCount) * indiceSideLengthSquare << 3);
		}

		void IBotTick.BotTick(IBot bot)
		{
			attackrespondcooldown--;

			if (firstTick)
			{
				resourceMapModule = bot.Player.PlayerActor.TraitsImplementing<ResourceMapBotModule>().First(t => t.IsTraitEnabled());
				SwitchExpansionMode(Info.InitialExpansionMode);

				pathDistanceSquareFactor = resourceMapModule.GetIndiceRowCount() * resourceMapModule.GetIndiceRowCount()
					+ resourceMapModule.GetIndiceColumnCount() * resourceMapModule.GetIndiceColumnCount();

				DeployMcvs(bot, false);
				firstTick = false;
			}

			if (--scanInterval <= 0)
			{
				foreach (var amcv in activeMCVs.Keys.ToList())
				{
					if (amcv.IsDead || !amcv.IsInWorld)
						activeMCVs.Remove(amcv);
				}

				scanInterval = Info.ScanForNewMcvInterval;
				DeployMcvs(bot, true);
			}

			if (--buildMCVInterval <= 0)
			{
				buildMCVInterval = Info.BuildMcvInterval;
				BuildMCV(bot);
			}

			if (--moveConyardInterval <= 0)
			{
				foreach (var amcv in activeMCVs.Keys.ToList())
				{
					if (amcv.IsDead || !amcv.IsInWorld)
						activeMCVs.Remove(amcv);
				}

				moveConyardInterval = Info.MoveConyardTick;
				UnDeployConyard(bot);
			}
		}

		void BuildMCV(IBot bot)
		{
			if (Info.McvTypes.Count <= 0)
				return;
			if (AIUtils.CountActorByCommonName(mcvFactories) <= 0)
				return;
			var mcvNum = AIUtils.CountActorByCommonName(mcvs);
			var conyardNum = AIUtils.CountActorByCommonName(constructionYards);

			var mcvShouldHave = playerResources.GetCashAndResources() >= Info.BuildAdditionalMCVCashAmount
				? Info.MinimumConstructionYardCount + Info.AdditionalConstructionYardCount : Info.MinimumConstructionYardCount;

			// If we only have 1 MCV and no conyard, we should be allowed to build another MCV.
			// Otherwise, when an mcv is on the move and we should wait.
			if ((conyardNum <= 0 && mcvNum > 1) || (conyardNum > 0 && mcvNum > 0))
				return;

			if (conyardNum + mcvNum >= mcvShouldHave)
				return;

			// We have MCV in production queue, let's wait.
			if (mcvFactories.Actors
				.Any(a => !a.IsDead && a.TraitsImplementing<ProductionQueue>().Any(t => t.Enabled && t.AllQueued().Any(q => Info.McvTypes.Contains(q.Item)))))
				return;

			// We have MCV in production queue, let's wait.
			if (player.PlayerActor.TraitsImplementing<ProductionQueue>()
				.Any(t => t.Enabled && t.AllQueued().Any(q => Info.McvTypes.Contains(q.Item))))
				return;
			var unitBuilder = requestUnitProduction.FirstEnabledTraitOrDefault();
			if (unitBuilder == null)
				return;
			var mcvType = Info.McvTypes.Random(world.LocalRandom);

			// Make sure we only request one MCV at a time.
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
			if (mustUndeployCoyard != null && mustUndeployCoyard.IsInWorld && !mustUndeployCoyard.IsDead && mustUndeployCoyard.Owner == player)
			{
				bot.QueueOrder(new Order("DeployTransform", mustUndeployCoyard, true));
				mustUndeployCoyard = null;

				return;
			}

			if (activeMCVs.Count > 0)
				return;

			var conyards = constructionYards.Actors
				.Where(a => !a.IsDead);

			var moveOldConyardFirst = Info.MoveOldConyardFirst ?? world.LocalRandom.Next(2) > 0;

			if (moveOldConyardFirst)
				conyards = conyards.OrderBy(a => a.ActorID);
			else
				conyards = conyards.OrderByDescending(a => a.ActorID);

			var conyardslist = conyards.ToList();

			if (conyardslist.Count > 1 || undeployEvenNoBase)
			{
				// We don't want to interrupt refinery production, otherwise it may cause a dead loop of deploy/undeploy.
				var movableMCV = conyardslist.FirstOrDefault(a => !a.TraitsImplementing<ProductionQueue>()
				.Any(t => t.Enabled && t.AllQueued().Any(q => resourceMapModule.Info.RefineryTypes.Contains(q.Item))));

				if (movableMCV != null)
					bot.QueueOrder(new Order("DeployTransform", movableMCV, true));

				undeployEvenNoBase = false;
			}
		}

		// Find any MCV and deploy them at a sensible location.
		void DeployMcv(IBot bot, Actor mcv, bool move)
		{
			CPos? desiredLocation = null;
			var transformsInfo = mcv.Info.TraitInfo<TransformsInfo>();
			var actorInfo = world.Map.Rules.Actors[transformsInfo.IntoActor];
			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (bi == null)
				return;

			if (move)
			{
				var (deployLocation, resLoc, checkloc) = ChooseMcvDeployLocation(mcv, actorInfo, bi, transformsInfo.Offset, allowfallback);
				allowfallback = true;
				desiredLocation = deployLocation;
				if (desiredLocation == null)
					return;

				activeMCVs[mcv] = checkloc.Value;
				if (resLoc != null)
				{
					foreach (var srp in suggestRefineryProduction)
						srp.RequestLocation(resLoc.Value, desiredLocation.Value, mcv);
				}

				bot.QueueOrder(new Order("Move", mcv, Target.FromCell(world, desiredLocation.Value), true));
			}
			else
			{
				if (!world.CanPlaceBuilding(mcv.Location + transformsInfo.Offset, actorInfo, bi, mcv))
					return;
				desiredLocation = mcv.Location;
			}

			bot.QueueOrder(new Order("DeployTransform", mcv, true));

			// When we don't have a construction yard, we notify the new location to other traits for defence,
			// If not, we only notify sometimes, because we are not sure if mcv can successfully deploy at the desired location.
			// TODO: This could be addressed via INotifyTransform.
			if (constructionYards.Actors.All(a => a.IsDead) || world.LocalRandom.Next(2) > 0)
			{
				foreach (var n in notifyPositionsUpdated)
				{
					n.UpdatedBaseCenter(desiredLocation.Value);
					n.UpdatedDefenseCenter(desiredLocation.Value);
				}
			}
		}

		// First, find a suitable expansion location according to current mode,
		// Then, find a deployable cell around it.
		(CPos? DeployLoc, CPos? ResourceLoc, CPos? CheckLoc) ChooseMcvDeployLocation(
			Actor mcv,
			ActorInfo transformIntoInfo,
			BuildingInfo transformIntoBuildingInfo,
			CVec offset,
			bool allowfallback)
		{
			if (!mcv.Info.HasTraitInfo<IMoveInfo>())
				return (null, null, null);

			var mobile = mcv.TraitOrDefault<Mobile>();

			var (expandCenter, attraction, checkspot) = GetExpansionCenter(mcv, mobile, allowfallback);

			// Find the deployable cell
			CPos? FindDeployCell(CPos? sourceCell, CPos? targetCell, int minRange, int maxRange, int tryMaintainRange)
			{
				if (!sourceCell.HasValue || !targetCell.HasValue)
					return null;

				var target = targetCell.Value;
				var source = sourceCell.Value;

				var cells = world.Map.FindTilesInAnnulus(target, minRange, maxRange);

				/* First, sort the cells that keep tryMaintainRange to target (meanwhile direction is from center to target) the first to be considered
				 * by using following code. The idea is to use a linear combination of two distances-square for sorting weight.
				 *
				 * See comments in https://github.com/OpenRA/OpenRA/pull/22028#issuecomment-3242518793 for explaination.
				 */
				if (source != target)
				{
					var theta = tryMaintainRange;
					var deta = (target - source).Length - tryMaintainRange;
					cells = cells.OrderBy(c => deta * (c - target).LengthSquared + theta * (c - source).LengthSquared);
				}
				else
					cells = cells.Shuffle(world.LocalRandom);

				CPos? bestcell = null;
				foreach (var cell in cells)
				{
					if (world.CanPlaceBuilding(cell + offset, transformIntoInfo, transformIntoBuildingInfo, mcv))
					{
						bestcell = cell;
						break;
					}
				}

				// If no deployble cell found, return null
				if (bestcell == null)
					return null;

				if (source != target && mobile != null && !pathfinder.PathMightExistForLocomotorBlockedByImmovable(mobile.Locomotor, source, bestcell.Value))
					bestcell = null;

				// If the best deploy cell is not ideal ( >= tryMaintainRange + 2), which means there might be some huge blockers
				// so we fall back to default behavior, which is the directly closest cell to target
				if (!bestcell.HasValue || (source != target && (bestcell.Value - target).LengthSquared >= (tryMaintainRange + 2) * (tryMaintainRange + 2)))
				{
					cells = cells.OrderBy(c => (c - target).LengthSquared);
					foreach (var cell in cells)
					{
						if (world.CanPlaceBuilding(cell + offset, transformIntoInfo, transformIntoBuildingInfo, mcv))
						{
							if (mobile != null && !pathfinder.PathMightExistForLocomotorBlockedByImmovable(mobile.Locomotor, source, cell))
								return null;

							return (!bestcell.HasValue) || (cell - target).LengthSquared < (bestcell.Value - target).LengthSquared ? cell : bestcell;
						}
					}
				}

				return bestcell;
			}

			var bc = FindDeployCell(mcv.Location, expandCenter, mcvDeploymentMinDeployRadius, mcvDeploymentMaxDeployRadius, mcvDeploymentTryMaintainRange);

			// At last, if the attraction of the found expansion location is good enough (>0) and deploy cell found,
			// we consider it as a good expansion, otherwise, we consider it as a bad expansion.
			if (bc.HasValue && attraction > 0)
				FindGoodDeploySpot();
			else
				FindBadDeploySpot(bc.HasValue ? null : checkspot);

			if (mcvExpansionMode == BotMcvExpansionMode.CheckResource && expandCenter.HasValue && bc.HasValue)
				return (bc, expandCenter, checkspot);

			return (bc, null, checkspot);
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (attackrespondcooldown <= 0 && Info.McvTypes.Contains(self.Info.Name))
			{
				attackrespondcooldown = 20;

				DeployMcv(bot, self, false);

				if (AIUtils.CountActorByCommonName(constructionYards) == 0)
				{
					foreach (var n in notifyPositionsUpdated)
						n.UpdatedBaseCenter(self.Location);
				}
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			mcvs.Dispose();
			constructionYards.Dispose();
			mcvFactories.Dispose();
		}

		void IBotBaseExpansion.UpdateExpansionParams(IBot bot, bool fallback, bool undeployEvenNoBase, Actor mustUndeploy)
		{
			moveConyardInterval = 20; // allow some order latency
			allowfallback = fallback;
			this.undeployEvenNoBase = undeployEvenNoBase;
		}
	}
}
