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
	[Desc("Logs Adaptive AI strategic state when BotDebug is enabled.")]
	public sealed class AdaptiveBotDebugModuleInfo : ConditionalTraitInfo
	{
		[Desc("Delay (in ticks) between debug log lines.")]
		public readonly int DebugInterval = 125;

		public override object Create(ActorInitializer init) { return new AdaptiveBotDebugModule(init.Self, this); }
	}

	public sealed class AdaptiveBotDebugModule : ConditionalTrait<AdaptiveBotDebugModuleInfo>, IBotTick, IBotEnabled
	{
		readonly Player player;
		BotIntelModule intel;
		AdaptiveCommanderModule commander;
		int ticks;

		public AdaptiveBotDebugModule(Actor self, AdaptiveBotDebugModuleInfo info)
			: base(info)
		{
			player = self.Owner;
		}

		void IBotEnabled.BotEnabled(IBot bot)
		{
			intel = bot.Player.PlayerActor.TraitsImplementing<BotIntelModule>().FirstOrDefault(t => t.IsTraitEnabled());
			commander = bot.Player.PlayerActor.TraitsImplementing<AdaptiveCommanderModule>().FirstOrDefault(t => t.IsTraitEnabled());
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (++ticks % Info.DebugInterval != 0)
				return;

			var plan = commander?.Plan;
			if (plan == null)
				return;

			var threat = intel?.GetPrimaryThreat();
			AIUtils.BotDebug(
				"Adaptive AI ({0}): goal={1} readiness={2:0.00} scout={3} threat air={4} armor={5} inf={6} boosts=[{7}] mix=[{8}]",
				player.ResolvedPlayerName,
				plan.ActiveGoal,
				plan.AttackReadiness,
				plan.ScoutActive,
				threat?.Air ?? 0,
				threat?.Armor ?? 0,
				threat?.Infantry ?? 0,
				string.Join(", ", plan.BuildingBoosts.Select(kv => $"{kv.Key}:{kv.Value}")),
				string.Join(", ", plan.UnitMixOverrides.Select(kv => $"{kv.Key}:{kv.Value}")));
		}
	}
}
