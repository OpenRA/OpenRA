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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Manages bot support power handling.")]
	public class SupportPowerBotModuleInfo : ConditionalTraitInfo, Requires<SupportPowerManagerInfo>
	{
		[Desc("Tells the AI how to use its support powers.")]
		[FieldLoader.LoadUsing("LoadDecisions")]
		public readonly List<SupportPowerDecision> Decisions = new List<SupportPowerDecision>();

		static object LoadDecisions(MiniYaml yaml)
		{
			var ret = new List<SupportPowerDecision>();
			var decisions = yaml.Nodes.FirstOrDefault(n => n.Key == "Decisions");
			if (decisions != null)
				foreach (var d in decisions.Value.Nodes)
					ret.Add(new SupportPowerDecision(d.Value));

			return ret;
		}

		public override object Create(ActorInitializer init) { return new SupportPowerBotModule(init.Self, this); }
	}

	public class SupportPowerBotModule : ConditionalTrait<SupportPowerBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		SupportPowerManager supportPowerManager;
		Dictionary<SupportPowerInstance, int> waitingPowers = new Dictionary<SupportPowerInstance, int>();
		Dictionary<string, SupportPowerDecision> powerDecisions = new Dictionary<string, SupportPowerDecision>();

		public SupportPowerBotModule(Actor self, SupportPowerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		protected override void TraitEnabled(Actor self)
		{
			supportPowerManager = player.PlayerActor.Trait<SupportPowerManager>();
			foreach (var decision in Info.Decisions)
				powerDecisions.Add(decision.OrderName, decision);
		}

		void IBotTick.BotTick(IBot bot)
		{
			foreach (var sp in supportPowerManager.Powers.Values)
			{
				if (sp.Disabled)
					continue;

				// Add power to dictionary if not in delay dictionary yet
				if (!waitingPowers.ContainsKey(sp))
					waitingPowers.Add(sp, 0);

				if (waitingPowers[sp] > 0)
					waitingPowers[sp]--;

				// If we have recently tried and failed to find a use location for a power, then do not try again until later
				var isDelayed = waitingPowers[sp] > 0;
				if (sp.Ready && !isDelayed && powerDecisions.ContainsKey(sp.Info.OrderName))
				{
					var powerDecision = powerDecisions[sp.Info.OrderName];
					if (powerDecision == null)
					{
						AIUtils.BotDebug("Bot Bug: FindAttackLocationToSupportPower, couldn't find powerDecision for {0}", sp.Info.OrderName);
						continue;
					}

					var attackLocation = FindCoarseAttackLocationToSupportPower(sp);
					if (attackLocation == null)
					{
						AIUtils.BotDebug("AI: {1} can't find suitable coarse attack location for support power {0}. Delaying rescan.", sp.Info.OrderName, player.PlayerName);
						waitingPowers[sp] += powerDecision.GetNextScanTime(world);

						continue;
					}

					// Found a target location, check for precise target
					attackLocation = FindFineAttackLocationToSupportPower(sp, (CPos)attackLocation);
					if (attackLocation == null)
					{
						AIUtils.BotDebug("AI: {1} can't find suitable final attack location for support power {0}. Delaying rescan.", sp.Info.OrderName, player.PlayerName);
						waitingPowers[sp] += powerDecision.GetNextScanTime(world);

						continue;
					}

					// Valid target found, delay by a few ticks to avoid rescanning before power fires via order
					AIUtils.BotDebug("AI: {2} found new target location {0} for support power {1}.", attackLocation, sp.Info.OrderName, player.PlayerName);
					waitingPowers[sp] += 10;
					bot.QueueOrder(new Order(sp.Key, supportPowerManager.Self, Target.FromCell(world, attackLocation.Value), false) { SuppressVisualFeedback = true });
				}
			}
		}

		/// <summary>Scans the map in chunks, evaluating all actors in each.</summary>
		CPos? FindCoarseAttackLocationToSupportPower(SupportPowerInstance readyPower)
		{
			CPos? bestLocation = null;
			var bestAttractiveness = 0;
			var powerDecision = powerDecisions[readyPower.Info.OrderName];
			if (powerDecision == null)
			{
				AIUtils.BotDebug("Bot Bug: FindAttackLocationToSupportPower, couldn't find powerDecision for {0}", readyPower.Info.OrderName);
				return null;
			}

			var map = world.Map;
			var checkRadius = powerDecision.CoarseScanRadius;
			for (var i = 0; i < map.MapSize.X; i += checkRadius)
			{
				for (var j = 0; j < map.MapSize.Y; j += checkRadius)
				{
					var tl = new MPos(i, j);
					var br = new MPos(i + checkRadius, j + checkRadius);
					var region = new CellRegion(map.Grid.Type, tl, br);

					// HACK: The AI code should not be messing with raw coordinate transformations
					var wtl = world.Map.CenterOfCell(tl.ToCPos(map));
					var wbr = world.Map.CenterOfCell(br.ToCPos(map));
					var targets = world.ActorMap.ActorsInBox(wtl, wbr);

					var frozenTargets = player.FrozenActorLayer != null ? player.FrozenActorLayer.FrozenActorsInRegion(region) : Enumerable.Empty<FrozenActor>();
					var consideredAttractiveness = powerDecision.GetAttractiveness(targets, player) + powerDecision.GetAttractiveness(frozenTargets, player);
					if (consideredAttractiveness <= bestAttractiveness || consideredAttractiveness < powerDecision.MinimumAttractiveness)
						continue;

					bestAttractiveness = consideredAttractiveness;
					bestLocation = new MPos(i, j).ToCPos(map);
				}
			}

			return bestLocation;
		}

		/// <summary>Detail scans an area, evaluating positions.</summary>
		CPos? FindFineAttackLocationToSupportPower(SupportPowerInstance readyPower, CPos checkPos, int extendedRange = 1)
		{
			CPos? bestLocation = null;
			var bestAttractiveness = 0;
			var powerDecision = powerDecisions[readyPower.Info.OrderName];
			if (powerDecision == null)
			{
				AIUtils.BotDebug("Bot Bug: FindAttackLocationToSupportPower, couldn't find powerDecision for {0}", readyPower.Info.OrderName);
				return null;
			}

			var checkRadius = powerDecision.CoarseScanRadius;
			var fineCheck = powerDecision.FineScanRadius;
			for (var i = 0 - extendedRange; i <= (checkRadius + extendedRange); i += fineCheck)
			{
				var x = checkPos.X + i;

				for (var j = 0 - extendedRange; j <= (checkRadius + extendedRange); j += fineCheck)
				{
					var y = checkPos.Y + j;
					var pos = world.Map.CenterOfCell(new CPos(x, y));
					var consideredAttractiveness = 0;
					consideredAttractiveness += powerDecision.GetAttractiveness(pos, player);

					if (consideredAttractiveness <= bestAttractiveness || consideredAttractiveness < powerDecision.MinimumAttractiveness)
						continue;

					bestAttractiveness = consideredAttractiveness;
					bestLocation = new CPos(x, y);
				}
			}

			return bestLocation;
		}
	}
}
