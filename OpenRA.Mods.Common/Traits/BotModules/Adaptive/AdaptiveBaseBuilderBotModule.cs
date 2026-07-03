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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Requests boosted building production for Adaptive AI.")]
	public sealed class AdaptiveBaseBuilderBotModuleInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new AdaptiveBaseBuilderBotModule(init.Self, this); }
	}

	public sealed class AdaptiveBaseBuilderBotModule : ConditionalTrait<AdaptiveBaseBuilderBotModuleInfo>, IBotTick, IBotEnabled
	{
		static readonly string[] BuildingCategories = ["Building", "Defense"];

		AdaptiveCommanderModule commander;
		AdaptiveAILobbySettings lobbySettings;
		PlayerResources playerResources;
		int ticks;

		public AdaptiveBaseBuilderBotModule(Actor self, AdaptiveBaseBuilderBotModuleInfo info)
			: base(info) { }

		void IBotEnabled.BotEnabled(IBot bot)
		{
			commander = bot.Player.PlayerActor.TraitsImplementing<AdaptiveCommanderModule>().FirstOrDefault(t => t.IsTraitEnabled());
			lobbySettings = bot.Player.PlayerActor.World.WorldActor.TraitOrDefault<AdaptiveAILobbySettings>();
			playerResources = bot.Player.PlayerActor.Trait<PlayerResources>();
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (lobbySettings?.CounterBuildEnabled != true)
				return;

			if (++ticks % 90 != 0)
				return;

			var plan = commander?.Plan;
			if (plan == null || plan.BuildingBoosts.Count == 0)
				return;

			if (playerResources.GetCashAndResources() < 500)
				return;

			var queuesByCategory = AIUtils.FindQueuesByCategory(bot.Player);
			var limit = lobbySettings.CounterBuildAggressive ? 2 : 1;
			foreach (var kv in plan.BuildingBoosts.OrderByDescending(kv => kv.Value).Take(limit))
			{
				if (kv.Key == "mslo" && !AdaptiveAIUtils.AllowsSuperweapons(lobbySettings.TechLevel))
					continue;

				foreach (var category in BuildingCategories)
				{
					foreach (var queue in queuesByCategory[category])
					{
						if (queue.AllItems().All(i => i.Name != kv.Key))
							continue;

						if (queue.AllQueued().Any(i => i.Item == kv.Key))
							return;

						bot.QueueOrder(Order.StartProduction(queue.Actor, kv.Key, 1));
						return;
					}
				}
			}
		}
	}
}
