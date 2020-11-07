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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Manages AI legacy bridge repair logic.")]
	public class BridgeRepairBotModuleInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[Desc("Actor types that can repair bridges (via `RepairsBridges`).",
			"Leave this empty to disable the trait.")]
		public readonly HashSet<string> RepairActorTypes = new HashSet<string>();

		[Desc("Avoid enemy actors nearby when searching for bridges in need of repair.",
			"Should be somewhere near the max weapon range.")]
		public readonly WDist EnemyAvoidanceRadius = WDist.FromCells(8);

		[Desc("Minimum delay (in ticks) between trying to scan for repair targets.")]
		public readonly int MinimumWaitDelay = 300;

		public override object Create(ActorInitializer init) { return new BridgeRepairBotModule(init.Self, this); }
	}

	public class BridgeRepairBotModule : ConditionalTrait<BridgeRepairBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsIdle;

		// Units that the bot already knows about and has given a repair order. Any unit not on this list needs to be given a new order.
		readonly List<Actor> activeRepairers = new List<Actor>();

		int waitDelayTicks;

		IPathFinder pathfinder;
		DomainIndex domainIndex;

		IBotRequestUnitProduction[] requestUnitProduction;

		public BridgeRepairBotModule(Actor self, BridgeRepairBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;

			if (world.Type == WorldType.Editor)
				return;

			unitCannotBeOrderedOrIsIdle = a => a.Owner != player || a.IsDead || !a.IsInWorld || a.IsIdle;
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			waitDelayTicks = world.LocalRandom.Next(Info.MinimumWaitDelay);

			pathfinder = world.WorldActor.Trait<IPathFinder>();
			domainIndex = world.WorldActor.Trait<DomainIndex>();

			requestUnitProduction = player.PlayerActor.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--waitDelayTicks <= 0)
			{
				waitDelayTicks = Info.MinimumWaitDelay;
				QueueRepairOrders(bot);
			}
		}

		void QueueRepairOrders(IBot bot)
		{
			if (!Info.RepairActorTypes.Any() || player.WinState != WinState.Undefined)
				return;

			activeRepairers.RemoveAll(unitCannotBeOrderedOrIsIdle);

			var newUnits = world.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == player && !activeRepairers.Contains(a));

			var repairers = newUnits
				.Where(a => a.IsIdle && Info.RepairActorTypes.Contains(a.Info.Name) && a.Info.HasTraitInfo<RepairsBridgesInfo>())
				.Select(a => new TraitPair<RepairsBridges>(a, a.TraitOrDefault<RepairsBridges>()))
				.Where(tp => tp.Trait != null)
				.ToArray();

			var targetOptions = world.ActorsWithTrait<LegacyBridgeHut>().Where(
				b => b.Trait.BridgeDamageState != DamageState.Undamaged);

			if (!targetOptions.Any())
				return;

			foreach (var repairer in repairers)
			{
				var nearestTargets = targetOptions.OrderBy(target => (target.Actor.CenterPosition - repairer.Actor.CenterPosition).LengthSquared);
				foreach (var nearestTarget in nearestTargets)
				{
					if (activeRepairers.Contains(repairer.Actor))
						continue;

					var safeTarget = SafePath(repairer.Actor, nearestTarget.Actor);
					if (safeTarget.Type == TargetType.Invalid)
						continue;

					bot.QueueOrder(new Order("RepairBridge", repairer.Actor, safeTarget, true));
					AIUtils.BotDebug("AI ({0}): Ordered {1} to repair {2}", player.ClientIndex, repairer.Actor, nearestTarget.Actor);
					activeRepairers.Add(repairer.Actor);
				}
			}

			// Request a new repairer on demand.
			var unitBuilder = requestUnitProduction.FirstOrDefault(Exts.IsTraitEnabled);
			if (unitBuilder != null)
			{
				var engineers = AIUtils.CountActorByCommonName(Info.RepairActorTypes, player);
				if (engineers == 0)
					unitBuilder.RequestUnitProduction(bot, Info.RepairActorTypes.Random(world.LocalRandom));
			}
		}

		Target SafePath(Actor repairer, Actor target)
		{
			var locomotor = repairer.Trait<Mobile>().Locomotor;

			if (!domainIndex.IsPassable(repairer.Location, target.Location, locomotor))
				return Target.Invalid;

			var path = pathfinder.FindPath(
				PathSearch.FromPoint(world, locomotor, repairer, repairer.Location, target.Location, BlockedByActor.None)
					.WithCustomCost(loc => world.FindActorsInCircle(world.Map.CenterOfCell(loc), Info.EnemyAvoidanceRadius)
						.Where(u => !u.IsDead && repairer.Owner.Stances[u.Owner] == Stance.Enemy && repairer.IsTargetableBy(u))
						.Sum(u => Math.Max(WDist.Zero.Length, Info.EnemyAvoidanceRadius.Length - (world.Map.CenterOfCell(loc) - u.CenterPosition).Length)))
					.FromPoint(repairer.Location));

			if (path.Count == 0)
				return Target.Invalid;

			return Target.FromActor(target);
		}
	}
}
