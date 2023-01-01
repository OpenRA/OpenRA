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

		[Desc("Player relationships that capturers should attempt to target.")]
		public readonly PlayerRelationship CapturableRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral;

		public override object Create(ActorInitializer init) { return new CaptureManagerBotModule(init.Self, this); }
	}

	public class CaptureManagerBotModule : ConditionalTrait<CaptureManagerBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		readonly Func<Actor, bool> isEnemyUnit;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsIdle;
		readonly int maximumCaptureTargetOptions;
		int minCaptureDelayTicks;

		// Units that the bot already knows about and has given a capture order. Any unit not on this list needs to be given a new order.
		readonly List<Actor> activeCapturers = new List<Actor>();

		public CaptureManagerBotModule(Actor self, CaptureManagerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;

			if (world.Type == WorldType.Editor)
				return;

			isEnemyUnit = unit =>
				player.RelationshipWith(unit.Owner) == PlayerRelationship.Enemy
					&& !unit.Info.HasTraitInfo<HuskInfo>()
					&& unit.Info.HasTraitInfo<ITargetableInfo>();

			unitCannotBeOrderedOrIsIdle = a => a.Owner != player || a.IsDead || !a.IsInWorld || a.IsIdle;

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
			if (Info.CapturingActorTypes.Count == 0 || player.WinState != WinState.Undefined)
				return;

			activeCapturers.RemoveAll(unitCannotBeOrderedOrIsIdle);

			var newUnits = world.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == player && !activeCapturers.Contains(a));

			var capturers = newUnits
				.Where(a => a.IsIdle && Info.CapturingActorTypes.Contains(a.Info.Name) && a.Info.HasTraitInfo<CapturesInfo>())
				.Select(a => new TraitPair<CaptureManager>(a, a.TraitOrDefault<CaptureManager>()))
				.Where(tp => tp.Trait != null)
				.ToArray();

			if (capturers.Length == 0)
				return;

			var randPlayer = world.Players.Where(p => !p.Spectating
				&& Info.CapturableRelationships.HasRelationship(player.RelationshipWith(p))).Random(world.LocalRandom);

			var targetOptions = Info.CheckCaptureTargetsForVisibility
				? GetVisibleActorsBelongingToPlayer(randPlayer)
				: GetActorsThatCanBeOrderedByPlayer(randPlayer);

			var capturableTargetOptions = targetOptions
				.Where(target =>
				{
					var captureManager = target.TraitOrDefault<CaptureManager>();
					if (captureManager == null)
						return false;

					return capturers.Any(tp => captureManager.CanBeTargetedBy(target, tp.Actor, tp.Trait));
				})
				.OrderByDescending(target => target.GetSellValue())
				.Take(maximumCaptureTargetOptions);

			if (Info.CapturableActorTypes.Count > 0)
				capturableTargetOptions = capturableTargetOptions.Where(target => Info.CapturableActorTypes.Contains(target.Info.Name.ToLowerInvariant()));

			if (!capturableTargetOptions.Any())
				return;

			foreach (var capturer in capturers)
			{
				var targetActor = capturableTargetOptions.MinByOrDefault(target => (target.CenterPosition - capturer.Actor.CenterPosition).LengthSquared);
				if (targetActor == null)
					continue;

				bot.QueueOrder(new Order("CaptureActor", capturer.Actor, Target.FromActor(targetActor), true));
				AIUtils.BotDebug("AI ({0}): Ordered {1} to capture {2}", player.ClientIndex, capturer.Actor, targetActor);
				activeCapturers.Add(capturer.Actor);
			}
		}
	}
}
