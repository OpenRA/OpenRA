#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	[Desc("Manages AI capturing logic.")]
	public class AICaptureModuleInfo : IAIModuleInfo
	{
		[Desc("Name for identification purposes.")]
		public readonly string Name = "default-capture-module";

		[Desc("Minimum delay (in ticks) between trying to capture with CapturingActorTypes.")]
		public readonly int MinimumCaptureDelay = 375;

		[Desc("Maximum number of options to consider for capturing.",
			"If a value less than 1 is given 1 will be used instead.")]
		public readonly int MaximumCaptureTargetOptions = 10;

		[Desc("Should visibility (Shroud, Fog, Cloak, etc) be considered when searching for capturable targets?")]
		public readonly bool CheckCaptureTargetsForVisibility = true;

		[Desc("Player stances that capturers should attempt to target.")]
		public readonly Stance CapturableStances = Stance.Enemy | Stance.Neutral;

		[Desc("Actor types that can capture other actors (via `Captures` or `ExternalCaptures`).",
			"Leave this empty to disable capturing.")]
		public HashSet<string> CapturingActorTypes = new HashSet<string>();

		[Desc("Actor types that can be targeted for capturing.",
			"Leave this empty to include all actors.")]
		public HashSet<string> CapturableActorTypes = new HashSet<string>();

		public object Create(ActorInitializer init) { return new AICaptureModule(init.Self, this); }
	}

	public class AICaptureModule : IAIModule
	{
		public readonly AICaptureModuleInfo Info;
		readonly int maximumCaptureTargetOptions;
		HackyAI ai;
		Player player;
		World world;
		int minCaptureDelayTicks;

		public AICaptureModule(Actor self, AICaptureModuleInfo info)
		{
			Info = info;
			maximumCaptureTargetOptions = Math.Max(1, info.MaximumCaptureTargetOptions);
		}

		string IAIModule.Name { get { return Info.Name; } }

		void IAIModule.Activate(HackyAI ai)
		{
			this.ai = ai;
			player = ai.Player;
			world = player.World;
			minCaptureDelayTicks = ai.Random.Next(0, Info.MinimumCaptureDelay);
		}

		void IAIModule.Tick()
		{
			if (!Info.CapturingActorTypes.Any() || player.WinState != WinState.Undefined)
				return;

			if (--minCaptureDelayTicks > 0)
				return;

			minCaptureDelayTicks = Info.MinimumCaptureDelay;

			var capturers = ai.UnitsHangingAroundTheBase
				.Where(a => a.IsIdle && Info.CapturingActorTypes.Contains(a.Info.Name))
				.Select(a => new TraitPair<CaptureManager>(a, a.TraitOrDefault<CaptureManager>()))
				.Where(tp => tp.Trait != null)
				.ToArray();

			if (capturers.Length == 0)
				return;

			var randPlayer = world.Players.Where(p => !p.Spectating
				&& Info.CapturableStances.HasStance(player.Stances[p])).Random(ai.Random);

			var targetOptions = Info.CheckCaptureTargetsForVisibility
				? AIUtils.GetVisibleActorsBelongingToPlayer(player, randPlayer)
				: AIUtils.GetActorsThatCanBeOrderedByPlayer(randPlayer);

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
				QueueCaptureOrderFor(tp.Actor, GetCapturerTargetClosestToOrDefault(tp.Actor, capturableTargetOptions));
		}

		void QueueCaptureOrderFor<TTargetType>(Actor capturer, CaptureTarget<TTargetType> target) where TTargetType : class, ITraitInfoInterface
		{
			if (capturer == null)
				return;

			if (target == null)
				return;

			if (target.Actor == null)
				return;

			ai.QueueOrder(new Order(target.OrderString, capturer, Target.FromActor(target.Actor), true));
			HackyAI.BotDebug("AI ({0}): Ordered {1} to capture {2}", player.ClientIndex, capturer, target.Actor);
			ai.RemoveActorFromActiveUnits(capturer);
		}

		CaptureTarget<TTargetType> GetCapturerTargetClosestToOrDefault<TTargetType>(Actor capturer, IEnumerable<CaptureTarget<TTargetType>> targets)
			where TTargetType : class, ITraitInfoInterface
		{
			return targets.MinByOrDefault(target => (target.Actor.CenterPosition - capturer.CenterPosition).LengthSquared);
		}
	}
}
