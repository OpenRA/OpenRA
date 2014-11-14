#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.Common.Power;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI
{
	class BaseBuilder
	{
		readonly string category;

		readonly HackyAI ai;
		readonly World world;
		readonly Player player;
		readonly PowerManager playerPower;
		readonly PlayerResources playerResources;

		int waitTicks;
		Actor[] playerBuildings;

		public BaseBuilder(HackyAI ai, string category, Player p, PowerManager pm, PlayerResources pr)
		{
			this.ai = ai;
			world = p.World;
			player = p;
			playerPower = pm;
			playerResources = pr;
			this.category = category;
		}

		public void Tick()
		{
			// Only update once per second or so
			if (--waitTicks > 0)
				return;

			playerBuildings = world.ActorsWithTrait<Building>()
				.Where(a => a.Actor.Owner == player)
				.Select(a => a.Actor)
				.ToArray();

			var active = false;
			foreach (var queue in ai.FindQueues(category))
				if (TickQueue(queue))
					active = true;

			waitTicks = active ? ai.Info.StructureProductionActiveDelay : ai.Info.StructureProductionInactiveDelay;
		}

		bool TickQueue(ProductionQueue queue)
		{
			var currentBuilding = queue.CurrentItem();

			// Waiting to build something
			if (currentBuilding == null)
			{
				var item = ChooseBuildingToBuild(queue);
				if (item == null)
					return false;

				HackyAI.BotDebug("AI: {0} is starting production of {1}".F(player, item.Name));
				world.IssueOrder(Order.StartProduction(queue.Actor, item.Name, 1));
			}

			// Production is complete
			else if (currentBuilding.Done)
			{
				// Choose the placement logic
				// HACK: HACK HACK HACK
				var type = BuildingType.Building;
				if (world.Map.Rules.Actors[currentBuilding.Item].Traits.Contains<AttackBaseInfo>())
					type = BuildingType.Defense;
				else if (world.Map.Rules.Actors[currentBuilding.Item].Traits.Contains<OreRefineryInfo>())
					type = BuildingType.Refinery;

				var location = ai.ChooseBuildLocation(currentBuilding.Item, true, type);
				if (location == null)
				{
					HackyAI.BotDebug("AI: {0} has nowhere to place {1}".F(player, currentBuilding.Item));
					world.IssueOrder(Order.CancelProduction(queue.Actor, currentBuilding.Item, 1));
				}
				else
				{
					world.IssueOrder(new Order("PlaceBuilding", player.PlayerActor, false)
					{
						TargetLocation = location.Value,
						TargetString = currentBuilding.Item,
						TargetActor = queue.Actor,
						SuppressVisualFeedback = true
					});

					return true;
				}
			}

			return true;
		}

		ActorInfo GetProducibleBuilding(string commonName, IEnumerable<ActorInfo> buildables, Func<ActorInfo, int> orderBy = null)
		{
			string[] actors;
			if (!ai.Info.BuildingCommonNames.TryGetValue(commonName, out actors))
				throw new InvalidOperationException("Can't find {0} in the HackyAI BuildingCommonNames definition.".F(commonName));

			var available = buildables.Where(actor =>
			{
				// Are we able to build this?
				if (!actors.Contains(actor.Name))
					return false;

				if (!ai.Info.BuildingLimits.ContainsKey(actor.Name))
					return true;

				return playerBuildings.Count(a => a.Info.Name == actor.Name) <= ai.Info.BuildingLimits[actor.Name];
			});

			if (orderBy != null)
				return available.MaxByOrDefault(orderBy);

			return available.RandomOrDefault(ai.random);
		}

		ActorInfo ChooseBuildingToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems();

			// First priority is to get out of a low power situation
			if (playerPower.ExcessPower < 0)
			{
				var power = GetProducibleBuilding("Power", buildableThings, a => a.Traits.Get<PowerInfo>().Amount);
				if (power != null && power.Traits.Get<PowerInfo>().Amount > 0)
				{
					// TODO: Handle the case when of when we actually do need a power plant because we don't have enough but are also suffering from a power outage
					if (playerPower.PowerOutageRemainingTicks <= 0)
					{
						HackyAI.BotDebug("AI: {0} decided to build {1}: Priority override (low power)", queue.Actor.Owner, power.Name);
						return power;
					}
				}
			}

			// Next is to build up a strong economy
			if (!ai.HasAdequateProc() || !ai.HasMinimumProc())
			{
				var refinery = GetProducibleBuilding("Refinery", buildableThings);
				if (refinery != null)
				{
					HackyAI.BotDebug("AI: {0} decided to build {1}: Priority override (refinery)", queue.Actor.Owner, refinery.Name);
					return refinery;
				}
			}

			// Make sure that we can can spend as fast as we are earning
			if (ai.Info.NewProductionCashThreshold > 0 && playerResources.Resources > ai.Info.NewProductionCashThreshold)
			{
				var production = GetProducibleBuilding("Production", buildableThings);
				if (production != null)
				{
					HackyAI.BotDebug("AI: {0} decided to build {1}: Priority override (production)", queue.Actor.Owner, production.Name);
					return production;
				}
			}

			// Create some head room for resource storage if we really need it
			if (playerResources.AlertSilo)
			{
				var silo = GetProducibleBuilding("Silo", buildableThings);
				if (silo != null)
				{
					HackyAI.BotDebug("AI: {0} decided to build {1}: Priority override (silo)", queue.Actor.Owner, silo.Name);
					return silo;
				}
			}

			// Build everything else
			foreach (var frac in ai.Info.BuildingFractions.Shuffle(ai.random))
			{
				var name = frac.Key;

				// Can we build this structure?
				if (!buildableThings.Any(b => b.Name == name))
					continue;

				// Do we want to build this structure?
				var count = playerBuildings.Count(a => a.Info.Name == name);
				if (count > frac.Value * playerBuildings.Length)
					continue;

				if (ai.Info.BuildingLimits.ContainsKey(name) && ai.Info.BuildingLimits[name] <= count)
					continue;

				// Will this put us into low power?
				var actor = world.Map.Rules.Actors[frac.Key];
				var pi = actor.Traits.GetOrDefault<PowerInfo>();
				if (playerPower.ExcessPower < 0 || (pi != null && playerPower.ExcessPower < pi.Amount))
				{
					// Try building a power plant instead
					var power = GetProducibleBuilding("Power", buildableThings, a => a.Traits.Get<PowerInfo>().Amount);
					if (power != null && power.Traits.Get<PowerInfo>().Amount > 0)
					{
						// TODO: Handle the case when of when we actually do need a power plant because we don't have enough but are also suffering from a power outage
						if (playerPower.PowerOutageRemainingTicks > 0)
							HackyAI.BotDebug("AI: {0} is suffering from a power outage; not going to build {1}", queue.Actor.Owner, power.Name);
						else
						{
							HackyAI.BotDebug("{0} decided to build {1}: Priority override (would be low power)", queue.Actor.Owner, power.Name);
							return power;
						}
					}
				}

				// Lets build this
				HackyAI.BotDebug("{0} decided to build {1}: Desired is {2} ({3} / {4}); current is {5} / {4}", queue.Actor.Owner, name, frac.Value, frac.Value * playerBuildings.Length, playerBuildings.Length, count);
				return actor;
			}

			// Too spammy to keep enabled all the time, but very useful when debugging specific issues.
			// HackyAI.BotDebug("{0} couldn't decide what to build for queue {1}.", queue.Actor.Owner, queue.Info.Group);
			return null;
		}
	}
}
