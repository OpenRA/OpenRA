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

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Adaptive AI unit production with dynamic army mix.")]
	public sealed class AdaptiveUnitBuilderBotModuleInfo : ConditionalTraitInfo
	{
		public readonly int IdleBaseUnitsMaximum = -1;

		public readonly ImmutableArray<string> UnitQueues = ["Vehicle", "Infantry", "Plane", "Ship", "Aircraft"];

		public readonly FrozenDictionary<string, int> UnitsToBuild = null;

		public readonly FrozenDictionary<string, int> UnitLimits = null;

		public readonly FrozenDictionary<string, int> UnitDelays = null;

		public readonly int ProductionMinCashRequirement = 500;

		public override object Create(ActorInitializer init) { return new AdaptiveUnitBuilderBotModule(init.Self, this); }
	}

	public sealed class AdaptiveUnitBuilderBotModule : ConditionalTrait<AdaptiveUnitBuilderBotModuleInfo>,
		IBotTick, IBotNotifyIdleBaseUnits, IBotRequestUnitProduction, INotifyActorDisposing
	{
		const int FeedbackTime = 30;

		readonly World world;
		readonly Player player;
		readonly List<string> queuedBuildRequests = [];
		readonly ActorIndex.OwnerAndNames unitsToBuild;

		AdaptiveCommanderModule commander;
		AdaptiveAILobbySettings lobbySettings;
		IBotRequestPauseUnitProduction[] requestPause;
		int idleUnitCount;
		int currentQueueIndex;
		PlayerResources playerResources;
		int ticks;

		public AdaptiveUnitBuilderBotModule(Actor self, AdaptiveUnitBuilderBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			unitsToBuild = new ActorIndex.OwnerAndNames(world, info.UnitsToBuild.Keys, player);
		}

		protected override void Created(Actor self)
		{
			requestPause = self.Owner.PlayerActor.TraitsImplementing<IBotRequestPauseUnitProduction>().ToArray();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			commander = self.TraitsImplementing<AdaptiveCommanderModule>().FirstOrDefault(t => t.IsTraitEnabled());
			lobbySettings = world.WorldActor.TraitOrDefault<AdaptiveAILobbySettings>();
		}

		void IBotNotifyIdleBaseUnits.UpdatedIdleBaseUnits(List<Actor> idleUnits)
		{
			idleUnitCount = idleUnits.Count;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (playerResources.GetCashAndResources() < Info.ProductionMinCashRequirement || requestPause.Any(rp => rp.PauseUnitProduction))
				return;

			ticks++;
			if (ticks % FeedbackTime != 0)
				return;

			ILookup<string, ProductionQueue> queuesByCategory = null;
			var buildRequest = queuedBuildRequests.FirstOrDefault();
			if (buildRequest != null)
			{
				queuesByCategory ??= AIUtils.FindQueuesByCategory(player);
				BuildUnit(bot, buildRequest, queuesByCategory);
				queuedBuildRequests.Remove(buildRequest);
			}

			if (Info.IdleBaseUnitsMaximum <= 0 || Info.IdleBaseUnitsMaximum > idleUnitCount)
			{
				queuesByCategory ??= AIUtils.FindQueuesByCategory(player);
				for (var i = 0; i < Info.UnitQueues.Length; i++)
				{
					if (++currentQueueIndex >= Info.UnitQueues.Length)
						currentQueueIndex = 0;

					var category = Info.UnitQueues[currentQueueIndex];
					var queues = queuesByCategory[category].ToArray();
					if (queues.Length != 0)
					{
						BuildRandomUnit(bot, queues);
						break;
					}
				}
			}
		}

		void IBotRequestUnitProduction.RequestUnitProduction(IBot bot, string requestedActor)
		{
			queuedBuildRequests.Add(requestedActor);
		}

		int IBotRequestUnitProduction.RequestedProductionCount(IBot bot, string requestedActor)
		{
			return queuedBuildRequests.Count(r => r == requestedActor);
		}

		void BuildRandomUnit(IBot bot, ProductionQueue[] queues)
		{
			if (Info.UnitsToBuild.Count == 0)
				return;

			var queue = queues.FirstOrDefault(q => !q.AllQueued().Any());
			if (queue == null)
				return;

			var unit = ChooseRandomUnitToBuild(queue);
			if (unit == null)
				return;

			bot.QueueOrder(Order.StartProduction(queue.Actor, unit.Name, 1));
		}

		void BuildUnit(IBot bot, string name, ILookup<string, ProductionQueue> queuesByCategory)
		{
			var actorInfo = world.Map.Rules.Actors[name];
			if (actorInfo == null)
				return;

			var buildableInfo = actorInfo.TraitInfoOrDefault<BuildableInfo>();
			if (buildableInfo == null)
				return;

			ProductionQueue queue = null;
			foreach (var pq in buildableInfo.Queue)
			{
				queue = queuesByCategory[pq].FirstOrDefault(q => !q.AllQueued().Any());
				if (queue != null)
					break;
			}

			if (queue != null)
				bot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));
		}

		ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems().Shuffle(world.LocalRandom).ToArray();
			if (buildableThings.Length == 0)
				return null;

			var allUnits = unitsToBuild.Actors.Where(a => !a.IsDead).ToArray();
			var overrides = commander?.Plan.UnitMixOverrides;
			var techLevel = lobbySettings?.TechLevel ?? "unrestricted";

			ActorInfo desiredUnit = null;
			var desiredError = int.MaxValue;
			foreach (var unit in buildableThings)
			{
				if (!Info.UnitsToBuild.TryGetValue(unit.Name, out var share))
					continue;

				if (overrides != null && overrides.TryGetValue(unit.Name, out var boost))
					share += boost;

				if (Info.UnitDelays != null && Info.UnitDelays.TryGetValue(unit.Name, out var delay) && delay > world.WorldTick)
					continue;

				var unitCount = allUnits.Count(a => a.Info.Name == unit.Name);
				if (Info.UnitLimits != null && Info.UnitLimits.TryGetValue(unit.Name, out var count) && unitCount >= count)
					continue;

				if (!AdaptiveAIUtils.AllowsSuperweapons(techLevel) && unit.Name is "mslo")
					continue;

				var productionMultiplier = lobbySettings?.ProductionMultiplier ?? 1f;
				share = (int)(share * productionMultiplier);

				var error = allUnits.Length > 0 ? unitCount * 100 / allUnits.Length - share : -1;
				if (error < 0)
					return HasAdequateAirUnitReloadBuildings(unit) ? unit : null;

				if (error < desiredError)
				{
					desiredError = error;
					desiredUnit = unit;
				}
			}

			return desiredUnit != null ? (HasAdequateAirUnitReloadBuildings(desiredUnit) ? desiredUnit : null) : null;
		}

		bool HasAdequateAirUnitReloadBuildings(ActorInfo actorInfo)
		{
			var aircraftInfo = actorInfo.TraitInfoOrDefault<AircraftInfo>();
			if (aircraftInfo == null)
				return true;

			var rearmableInfo = actorInfo.TraitInfoOrDefault<RearmableInfo>();
			if (rearmableInfo == null)
				return true;

			var countOwnAir = AIUtils.CountActorsWithNameAndTrait<IPositionable>(actorInfo.Name, player);
			var countBuildings = rearmableInfo.RearmActors.Sum(b => AIUtils.CountActorsWithNameAndTrait<Building>(b, player));
			return countOwnAir < countBuildings;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			unitsToBuild.Dispose();
		}
	}
}
