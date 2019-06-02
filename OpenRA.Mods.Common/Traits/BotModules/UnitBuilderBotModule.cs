#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls AI unit production.")]
	public class UnitBuilderBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Production queues AI uses for producing units.")]
		public readonly HashSet<string> UnitQueues = new HashSet<string> { "Vehicle", "Infantry", "Plane", "Ship", "Aircraft" };

		public override object Create(ActorInitializer init) { return new UnitBuilderBotModule(init.Self, this); }
	}

	public class UnitBuilderBotModule : ConditionalTrait<UnitBuilderBotModuleInfo>, IBotTick, IBotRequestUnitProduction,
		IGameSaveTraitData, IBotNotifyProductionConsiderationsUpdated
	{
		public class UnitProductionConsideration
		{
			public int Percentage;
			public int Limit = -1;
			public int Delay = -1;
		}

		public const int FeedbackTime = 30; // ticks; = a bit over 1s. must be >= netlag.

		readonly World world;
		readonly Player player;

		readonly List<string> queuedBuildRequests = new List<string>();

		IBotRequestPauseUnitProduction[] requestPause;
		BotProductionConsideration[] considerations;
		bool considerationsDirty = true;

		readonly Dictionary<string, Dictionary<string, UnitProductionConsideration>> resolvedConsiderations =
			new Dictionary<string, Dictionary<string, UnitProductionConsideration>>();

		int ticks;

		public UnitBuilderBotModule(Actor self, UnitBuilderBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query player traits from self, which refers
			// for bot modules always to the Player actor.
			requestPause = self.TraitsImplementing<IBotRequestPauseUnitProduction>().ToArray();
			considerations = self.TraitsImplementing<BotProductionConsideration>()
				.Where(c => c.Info.Queues.Overlaps(Info.UnitQueues))
				.OrderBy(c => c.Info.Priority)
				.ToArray();
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (considerationsDirty)
			{
				resolvedConsiderations.Clear();
				foreach (var c in considerations)
				{
					if (c.IsTraitDisabled)
						continue;

					foreach (var queue in c.Info.Queues)
					{
						var queueConsideration = resolvedConsiderations.GetOrAdd(queue);
						foreach (var a in c.Info.Actors)
						{
							var queueActorConsideration = queueConsideration.GetOrAdd(a.Key);

							if (a.Value.Percentage >= 0)
								queueActorConsideration.Percentage = a.Value.Percentage;

							if (a.Value.Limit >= 0)
								queueActorConsideration.Limit = a.Value.Limit;

							if (a.Value.Delay >= 0)
								queueActorConsideration.Delay = a.Value.Delay;
						}
					}
				}

				considerationsDirty = false;
			}

			if (requestPause.Any(rp => rp.PauseUnitProduction))
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

				var idleQueues = Info.UnitQueues.Select(queue => AIUtils.FindQueues(player, queue)
					.FirstOrDefault(q => !q.AllQueued().Any()))
					.Where(q => q != null)
					.ToList();

				if (idleQueues.Any())
				{
					var ownedActorCounts = player.World.Actors
						.Where(a => a.Owner == player)
						.Select(a => a.Info.Name)
						.GroupBy(a => a)
						.ToDictionary(a => a.Key, a => a.Count());

					foreach (var q in idleQueues)
						BuildUnit(bot, q, ownedActorCounts);
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

		void BuildUnit(IBot bot, ProductionQueue queue, Dictionary<string, int> ownedActorCounts)
		{
			Dictionary<string, UnitProductionConsideration> queueConsiderations;
			if (!resolvedConsiderations.TryGetValue(queue.Info.Type, out queueConsiderations))
				return;

			// Create a random list of actors that are buildable and valid considerations
			var buildableConsiderations = queue.BuildableItems().Where(ai =>
			{
				UnitProductionConsideration unitConsideratation;
				if (!queueConsiderations.TryGetValue(ai.Name, out unitConsideratation))
					return false;

				if (unitConsideratation.Delay > world.WorldTick || unitConsideratation.Limit == 0)
					return false;

				int ownedActorCount;
				if (unitConsideratation.Limit > 0 && ownedActorCounts.TryGetValue(ai.Name, out ownedActorCount) && ownedActorCount > unitConsideratation.Limit)
					return false;

				return HasAdequateAirUnitReloadBuildings(ai);
			}).Select(b => b.Name).Shuffle(world.LocalRandom).ToList();

			if (!buildableConsiderations.Any())
				return;

			var queueTotal = ownedActorCounts
				.Where(c => queueConsiderations.ContainsKey(c.Key))
				.Select(c => c.Value)
				.Sum();

			// Choose an actor to try and balance out the defined composition percentages
			string build = null;
			foreach (var b in buildableConsiderations)
			{
				int owned = 0;
				ownedActorCounts.TryGetValue(b, out owned);

				var desiredPercentage = queueConsiderations[b].Percentage;
				if (owned * 100 < desiredPercentage * queueTotal)
				{
					AIUtils.BotDebug("{0} decided to build {1}: Desired is {2:F1} current is {3}",
						queue.Actor.Owner, b, desiredPercentage * queueTotal / 100.0, owned);

					build = b;
					break;
				}
			}

			// Pick first random choice if there is no preference
			if (build == null)
			{
				build = buildableConsiderations.First();
				AIUtils.BotDebug("{0} decided to build {1}: Random available consideration",
					queue.Actor.Owner, build);
			}

			bot.QueueOrder(Order.StartProduction(queue.Actor, build, 1));
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
				AIUtils.BotDebug("AI: {0} decided to build {1} (external request)", queue.Actor.Owner, name);
			}
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
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, List<MiniYamlNode> data)
		{
			if (self.World.IsReplay)
				return;

			var queuedBuildRequestsNode = data.FirstOrDefault(n => n.Key == "QueuedBuildRequests");
			if (queuedBuildRequestsNode != null)
			{
				queuedBuildRequests.Clear();
				queuedBuildRequests.AddRange(FieldLoader.GetValue<string[]>("QueuedBuildRequests", queuedBuildRequestsNode.Value.Value));
			}
		}

		void IBotNotifyProductionConsiderationsUpdated.ProductionConsiderationsUpdated(HashSet<string> queues)
		{
			if (queues.Overlaps(Info.UnitQueues))
				considerationsDirty = true;
		}
	}
}
