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
	[Desc("Requests counter-production for Adaptive AI.")]
	public sealed class AdaptiveCounterBuildBotModuleInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new AdaptiveCounterBuildBotModule(init.Self, this); }
	}

	public sealed class AdaptiveCounterBuildBotModule : ConditionalTrait<AdaptiveCounterBuildBotModuleInfo>, IBotTick, IBotEnabled
	{
		AdaptiveCommanderModule commander;
		AdaptiveAILobbySettings lobbySettings;
		IBotRequestUnitProduction[] unitProduction;
		int ticks;

		public AdaptiveCounterBuildBotModule(Actor self, AdaptiveCounterBuildBotModuleInfo info)
			: base(info) { }

		void IBotEnabled.BotEnabled(IBot bot)
		{
			commander = bot.Player.PlayerActor.TraitsImplementing<AdaptiveCommanderModule>().FirstOrDefault(t => t.IsTraitEnabled());
			lobbySettings = bot.Player.PlayerActor.World.WorldActor.TraitOrDefault<AdaptiveAILobbySettings>();
			unitProduction = bot.Player.PlayerActor.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (lobbySettings?.CounterBuildEnabled != true)
				return;

			if (++ticks % 60 != 0)
				return;

			var plan = commander?.Plan;
			if (plan == null)
				return;

			var limit = lobbySettings.CounterBuildAggressive ? 3 : 1;
			foreach (var kv in plan.UnitMixOverrides.OrderByDescending(kv => kv.Value).Take(limit))
			{
				foreach (var up in unitProduction)
					up.RequestUnitProduction(bot, kv.Key);
			}
		}
	}
}
