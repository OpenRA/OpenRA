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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Sends scouts to explore fog for Adaptive AI.")]
	public sealed class ScoutBotModuleInfo : ConditionalTraitInfo
	{
		[ActorReference]
		public readonly FrozenSet<string> ScoutUnitTypes = FrozenSet<string>.Empty;

		public override object Create(ActorInitializer init) { return new ScoutBotModule(init.Self, this); }
	}

	public sealed class ScoutBotModule : ConditionalTrait<ScoutBotModuleInfo>, IBotTick, IBotEnabled
	{
		readonly World world;
		readonly Player player;

		AdaptiveCommanderModule commander;
		AdaptiveAILobbySettings lobbySettings;
		BotIntelModule intel;
		readonly HashSet<Actor> activeScouts = [];
		int assignTicks;

		public ScoutBotModule(Actor self, ScoutBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		void IBotEnabled.BotEnabled(IBot bot)
		{
			commander = bot.Player.PlayerActor.TraitsImplementing<AdaptiveCommanderModule>().FirstOrDefault(t => t.IsTraitEnabled());
			intel = bot.Player.PlayerActor.TraitsImplementing<BotIntelModule>().FirstOrDefault(t => t.IsTraitEnabled());
			lobbySettings = world.WorldActor.TraitOrDefault<AdaptiveAILobbySettings>();
		}

		void IBotTick.BotTick(IBot bot)
		{
			activeScouts.RemoveWhere(a => a == null || a.IsDead || !a.IsInWorld);

			if (--assignTicks > 0)
				return;

			assignTicks = 50;

			if (commander?.Plan.ScoutActive != true && (intel?.TicksSinceLastSighting ?? 0) < (lobbySettings?.IntelStaleTicks ?? 750))
				return;

			var maxScouts = lobbySettings?.MaxScouts ?? 2;
			if (maxScouts <= 0 || activeScouts.Count >= maxScouts)
				return;

			var scout = world.ActorsHavingTrait<Mobile>()
				.Where(a => a.Owner == player && Info.ScoutUnitTypes.Contains(a.Info.Name) && !activeScouts.Contains(a))
				.FirstOrDefault(a => a.CurrentActivity == null);

			if (scout == null)
				return;

			var target = FindUnexploredCellNearBase();
			if (target == null)
				return;

			activeScouts.Add(scout);
			bot.QueueOrder(new Order("Move", scout, Target.FromCell(world, target.Value), false));
		}

		CPos? FindUnexploredCellNearBase()
		{
			var shroud = player.Shroud;
			if (shroud == null || shroud.Disabled)
				return null;

			var baseCell = world.ActorsHavingTrait<Building>()
				.FirstOrDefault(a => a.Owner == player)?.Location ?? default;

			var candidates = world.Map.FindTilesInCircle(baseCell, 20)
				.Where(c => shroud.IsExplored(c) && world.Map.Contains(c))
				.SelectMany(c => world.Map.FindTilesInCircle(c, 3))
				.Where(c => world.Map.Contains(c) && !shroud.IsExplored(c))
				.ToList();

			return candidates.Count > 0 ? candidates.Random(world.LocalRandom) : null;
		}
	}
}
