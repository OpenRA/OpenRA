#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	[Desc("Manages AI capturing logic.")]
	public class CaptureManagerBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that can capture other actors (via `Captures`).",
			"Leave this empty to disable capturing.")]
		public readonly HashSet<string> CapturingActorTypes = new HashSet<string>();

		[Desc("Actor types that can be targeted for capturing.",
			"Leave this empty to include all actors.")]
		public readonly HashSet<string> CapturableActorTypes = new HashSet<string>();

		[Desc("Minimum delay (in ticks) between trying to capture with CapturingActorTypes.")]
		public readonly int MinimumCaptureDelay = 375;

		[Desc("Maximum number of options to consider for capturing.",
			"If a value less than 1 is given 1 will be used instead.")]
		public readonly int MaximumCaptureTargetOptions = 10;

		[Desc("Should visibility (Shroud, Fog, Cloak, etc) be considered when searching for capturable targets?")]
		public readonly bool CheckCaptureTargetsForVisibility = true;

		[Desc("Player stances that capturers should attempt to target.")]
		public readonly Stance CapturableStances = Stance.Enemy | Stance.Neutral;

		public override object Create(ActorInitializer init) { return new CaptureManagerBotModule(init.Self, this); }
	}

	public class CaptureManagerBotModule : ConditionalTrait<CaptureManagerBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		readonly Func<Actor, bool> isEnemyUnit;
		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly int maximumCaptureTargetOptions;
		int minCaptureDelayTicks;

		// Units that the bot already knows about and has given a capture order. Any unit not on this list needs to be given a new order.
		List<Actor> activeCapturers = new List<Actor>();

		public CaptureManagerBotModule(Actor self, CaptureManagerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;

			if (world.Type == WorldType.Editor)
				return;

			isEnemyUnit = unit =>
				player.Stances[unit.Owner] == Stance.Enemy
					&& !unit.Info.HasTraitInfo<HuskInfo>()
					&& unit.Info.HasTraitInfo<ITargetableInfo>();

			unitCannotBeOrdered = a => a.Owner != player || a.IsDead || !a.IsInWorld;

			maximumCaptureTargetOptions = Math.Max(1, Info.MaximumCaptureTargetOptions);
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			minCaptureDelayTicks = world.LocalRandom.Next(0, Info.MinimumCaptureDelay);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--minCaptureDelayTicks <= 0)
			{
				minCaptureDelayTicks = Info.MinimumCaptureDelay;
				QueueCaptureOrders(bot);
			}
		}

		internal Actor FindClosestEnemy(WPos pos)
		{
			return world.Actors.Where(isEnemyUnit).ClosestTo(pos);
		}

		internal Actor FindClosestEnemy(WPos pos, WDist radius)
		{
			return world.FindActorsInCircle(pos, radius).Where(isEnemyUnit).ClosestTo(pos);
		}

		IEnumerable<Actor> GetVisibleActorsBelongingToPlayer(Player owner)
		{
			foreach (var actor in GetActorsThatCanBeOrderedByPlayer(owner))
				if (actor.CanBeViewedByPlayer(player))
					yield return actor;
		}

		IEnumerable<Actor> GetActorsThatCanBeOrderedByPlayer(Player owner)
		{
			foreach (var actor in world.Actors)
				if (actor.Owner == owner && !actor.IsDead && actor.IsInWorld)
					yield return actor;
		}

		void QueueCaptureOrders(IBot bot)
		{
			if (!Info.CapturingActorTypes.Any() || player.WinState != WinState.Undefined)
				return;

			activeCapturers.RemoveAll(unitCannotBeOrdered);

			var newUnits = world.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == player && !activeCapturers.Contains(a));

			var capturers = newUnits
				.Where(a => a.IsIdle && Info.CapturingActorTypes.Contains(a.Info.Name))
				.Select(a => new TraitPair<CaptureManager>(a, a.TraitOrDefault<CaptureManager>()))
				.Where(tp => tp.Trait != null)
				.ToArray();

			if (capturers.Length == 0)
				return;

			var randPlayer = world.Players.Where(p => !p.Spectating
				&& Info.CapturableStances.HasStance(player.Stances[p])).Random(world.LocalRandom);

			var targetOptions = Info.CheckCaptureTargetsForVisibility
				? GetVisibleActorsBelongingToPlayer(randPlayer)
				: GetActorsThatCanBeOrderedByPlayer(randPlayer);

			var capturableTargetOptions = targetOptions
				.Select(a => new CaptureTarget<CapturableInfo>(a, "CaptureActor"))
				.Where(target =>
				{
					if (target.Info == null)
						return false;

					var captureManager = target.Actor.TraitOrDefault<CaptureManager>();
					if (captureManager == null)
						return false;

					return capturers.Any(tp => captureManager.CanBeTargetedBy(target.Actor, tp.Actor, tp.Trait));
				})
				.OrderByDescending(target => target.Actor.GetSellValue())
				.Take(maximumCaptureTargetOptions);

			if (Info.CapturableActorTypes.Any())
				capturableTargetOptions = capturableTargetOptions.Where(target => Info.CapturableActorTypes.Contains(target.Actor.Info.Name.ToLowerInvariant()));

			if (!capturableTargetOptions.Any())
				return;

			var capturesCapturers = capturers.Where(tp => tp.Actor.Info.HasTraitInfo<CapturesInfo>());
			foreach (var tp in capturesCapturers)
				QueueCaptureOrderFor(bot, tp.Actor, GetCapturerTargetClosestToOrDefault(tp.Actor, capturableTargetOptions));
		}

		void QueueCaptureOrderFor<TTargetType>(IBot bot, Actor capturer, CaptureTarget<TTargetType> target) where TTargetType : class, ITraitInfoInterface
		{
			if (capturer == null)
				return;

			if (target == null)
				return;

			if (target.Actor == null)
				return;

			bot.QueueOrder(new Order(target.OrderString, capturer, Target.FromActor(target.Actor), true));
			AIUtils.BotDebug("AI ({0}): Ordered {1} to capture {2}", player.ClientIndex, capturer, target.Actor);
			activeCapturers.Add(capturer);
		}

		CaptureTarget<TTargetType> GetCapturerTargetClosestToOrDefault<TTargetType>(Actor capturer, IEnumerable<CaptureTarget<TTargetType>> targets)
			where TTargetType : class, ITraitInfoInterface
		{
			return targets.MinByOrDefault(target => (target.Actor.CenterPosition - capturer.CenterPosition).LengthSquared);
		}
	}
}
