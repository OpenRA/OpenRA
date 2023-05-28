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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls AI unit production.")]
	public class UnitBuilderBotModuleInfo : ConditionalTraitInfo
	{
		// TODO: Investigate whether this might the (or at least one) reason why bots occasionally get into a state of doing nothing.
		// Reason: If this is less than SquadSize, the bot might get stuck between not producing more units due to this,
		// but also not creating squads since there aren't enough idle units.
		[Desc("If > 0, only produce units as long as there are less than this amount of units idling inside the base.",
			"Beware: if it is less than squad size, e.g. the `SquadSize` from `SquadManagerBotModule`, the bot might get stuck as there aren't enough idle units to create squad.")]
		public readonly int IdleBaseUnitsMaximum = -1;

		[Desc("Production queues AI uses for producing units.")]
		public readonly string[] UnitQueues = { "Vehicle", "Infantry", "Plane", "Ship", "Aircraft" };

		[Desc("What units to the AI should build.", "What relative share of the total army must be this type of unit.")]
		public readonly Dictionary<string, int> UnitsToBuild = null;

		[Desc("What units should the AI have a maximum limit to train.")]
		public readonly Dictionary<string, int> UnitLimits = null;

		[Desc("When should the AI start train specific units.")]
		public readonly Dictionary<string, int> UnitDelays = null;

		[Desc("Only queue construction of a new unit when above this requirement.")]
		public readonly int ProductionMinCashRequirement = 500;

		public override object Create(ActorInitializer init) { return new UnitBuilderBotModule(init.Self, this); }
	}

	public class UnitBuilderBotModule : ConditionalTrait<UnitBuilderBotModuleInfo>, IBotTick, IBotNotifyIdleBaseUnits, IBotRequestUnitProduction, IGameSaveTraitData
	{
		public const int FeedbackTime = 30; // ticks; = a bit over 1s. must be >= netlag.

		readonly World world;
		readonly Player player;

		readonly List<string> queuedBuildRequests = new();

		IBotRequestPauseUnitProduction[] requestPause;
		int idleUnitCount;
		int currentQueueIndex = 0;
		PlayerResources playerResources;

		int ticks;

		public UnitBuilderBotModule(Actor self, UnitBuilderBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		protected override void Created(Actor self)
		{
			requestPause = self.Owner.PlayerActor.TraitsImplementing<IBotRequestPauseUnitProduction>().ToArray();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		void IBotNotifyIdleBaseUnits.UpdatedIdleBaseUnits(List<Actor> idleUnits)
		{
			idleUnitCount = idleUnits.Count;
		}

		void IBotTick.BotTick(IBot bot)
		{
			// PERF: We shouldn't be queueing new units when we're low on cash
			if (playerResources.GetCashAndResources() < Info.ProductionMinCashRequirement || requestPause.Any(rp => rp.PauseUnitProduction))
				return;

			ticks++;

			if (ticks % FeedbackTime == 0)
			{
				var buildRequest = queuedBuildRequests.FirstOrDefault();
				if (buildRequest != null)
				{
					BuildUnit(bot, buildRequest);
					queuedBuildRequests.Remove(buildRequest);
				}

				if (Info.IdleBaseUnitsMaximum <= 0 || Info.IdleBaseUnitsMaximum > idleUnitCount)
				{
					for (var i = 0; i < Info.UnitQueues.Length; i++)
					{
						if (++currentQueueIndex >= Info.UnitQueues.Length)
							currentQueueIndex = 0;

						if (AIUtils.FindQueues(player, Info.UnitQueues[currentQueueIndex]).Any())
						{
							// PERF: We tick only one type of valid queue at a time
							// if AI gets enough cash, it can fill all of its queues with enough ticks
							BuildRandomUnit(bot, Info.UnitQueues[currentQueueIndex]);
							break;
						}
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

		void BuildRandomUnit(IBot bot, string category)
		{
			if (Info.UnitsToBuild.Count == 0)
				return;

			// Pick a free queue
			var queue = AIUtils.FindQueues(player, category).FirstOrDefault(q => !q.AllQueued().Any());
			if (queue == null)
				return;

			var unit = ChooseRandomUnitToBuild(queue);

			if (unit == null)
				return;

			bot.QueueOrder(Order.StartProduction(queue.Actor, unit.Name, 1));
		}

		// In cases where we want to build a specific unit but don't know the queue name (because there's more than one possibility)
		void BuildUnit(IBot bot, string name)
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
				queue = AIUtils.FindQueues(player, pq).FirstOrDefault(q => !q.AllQueued().Any());
				if (queue != null)
					break;
			}

			if (queue != null)
			{
				bot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));
				AIUtils.BotDebug("{0} decided to build {1} (external request)", queue.Actor.Owner, name);
			}
		}

		ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems().Shuffle(world.LocalRandom).ToArray();
			if (buildableThings.Length == 0)
				return null;

			var allUnits = world.Actors.Where(a => a.Owner == player && Info.UnitsToBuild.ContainsKey(a.Info.Name) && !a.IsDead).ToArray();

			ActorInfo desiredUnit = null;
			var desiredError = int.MaxValue;
			foreach (var unit in buildableThings)
			{
				if (!Info.UnitsToBuild.ContainsKey(unit.Name) || (Info.UnitDelays != null && Info.UnitDelays.TryGetValue(unit.Name, out var delay) && delay > world.WorldTick))
					continue;

				var unitCount = allUnits.Count(a => a.Info.Name == unit.Name);
				if (Info.UnitLimits != null && Info.UnitLimits.TryGetValue(unit.Name, out var count) && unitCount >= count)
					continue;

				var error = allUnits.Length > 0 ? unitCount * 100 / allUnits.Length - Info.UnitsToBuild[unit.Name] : -1;
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

		// For mods like RA (number of RearmActors must match the number of aircraft)
		bool HasAdequateAirUnitReloadBuildings(ActorInfo actorInfo)
		{
			var aircraftInfo = actorInfo.TraitInfoOrDefault<AircraftInfo>();
			if (aircraftInfo == null)
				return true;

			// If actor isn't Rearmable, it doesn't need a RearmActor to reload
			var rearmableInfo = actorInfo.TraitInfoOrDefault<RearmableInfo>();
			if (rearmableInfo == null)
				return true;

			var countOwnAir = AIUtils.CountActorsWithTrait<IPositionable>(actorInfo.Name, player);
			var countBuildings = rearmableInfo.RearmActors.Sum(b => AIUtils.CountActorsWithTrait<Building>(b, player));
			if (countOwnAir >= countBuildings)
				return false;

			return true;
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("QueuedBuildRequests", FieldSaver.FormatValue(queuedBuildRequests.ToArray())),
				new MiniYamlNode("IdleUnitCount", FieldSaver.FormatValue(idleUnitCount))
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, ImmutableArray<MiniYamlNode> data)
		{
			if (self.World.IsReplay)
				return;

			var queuedBuildRequestsNode = data.FirstOrDefault(n => n.Key == "QueuedBuildRequests");
			if (queuedBuildRequestsNode != null)
			{
				queuedBuildRequests.Clear();
				queuedBuildRequests.AddRange(FieldLoader.GetValue<string[]>("QueuedBuildRequests", queuedBuildRequestsNode.Value.Value));
			}

			var idleUnitCountNode = data.FirstOrDefault(n => n.Key == "IdleUnitCount");
			if (idleUnitCountNode != null)
				idleUnitCount = FieldLoader.GetValue<int>("IdleUnitCount", idleUnitCountNode.Value.Value);
		}
	}
}
