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

	public class SupportPowerBotModule : ConditionalTrait<SupportPowerBotModuleInfo>, IBotTick, IGameSaveTraitData
	{
		readonly World world;
		readonly Player player;
		readonly Dictionary<SupportPowerInstance, int> waitingPowers = new Dictionary<SupportPowerInstance, int>();
		readonly Dictionary<string, SupportPowerDecision> powerDecisions = new Dictionary<string, SupportPowerDecision>();
		readonly List<SupportPowerInstance> stalePowers = new List<SupportPowerInstance>();
		SupportPowerManager supportPowerManager;

		public SupportPowerBotModule(Actor self, SupportPowerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		protected override void Created(Actor self)
		{
			supportPowerManager = self.Owner.PlayerActor.Trait<SupportPowerManager>();
		}

		protected override void TraitEnabled(Actor self)
		{
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
						AIUtils.BotDebug("{0} couldn't find powerDecision for {1}", player.PlayerName, sp.Info.OrderName);
						continue;
					}

					var attackLocation = FindCoarseAttackLocationToSupportPower(sp);
					if (attackLocation == null)
					{
						AIUtils.BotDebug("{0} can't find suitable coarse attack location for support power {1}. Delaying rescan.", player.PlayerName, sp.Info.OrderName);
						waitingPowers[sp] += powerDecision.GetNextScanTime(world);

						continue;
					}

					// Found a target location, check for precise target
					attackLocation = FindFineAttackLocationToSupportPower(sp, (CPos)attackLocation);
					if (attackLocation == null)
					{
						AIUtils.BotDebug("{0} can't find suitable final attack location for support power {1}. Delaying rescan.", player.PlayerName, sp.Info.OrderName);
						waitingPowers[sp] += powerDecision.GetNextScanTime(world);

						continue;
					}

					// Valid target found, delay by a few ticks to avoid rescanning before power fires via order
					AIUtils.BotDebug("{0} found new target location {1} for support power {2}.", player.PlayerName, attackLocation, sp.Info.OrderName);
					waitingPowers[sp] += 10;
					bot.QueueOrder(new Order(sp.Key, supportPowerManager.Self, Target.FromCell(world, attackLocation.Value), false) { SuppressVisualFeedback = true });
				}
			}

			// Remove stale powers
			stalePowers.AddRange(waitingPowers.Keys.Where(wp => !supportPowerManager.Powers.ContainsKey(wp.Key)));
			foreach (var p in stalePowers)
				waitingPowers.Remove(p);

			stalePowers.Clear();
		}

		/// <summary>Scans the map in chunks, evaluating all actors in each.</summary>
		CPos? FindCoarseAttackLocationToSupportPower(SupportPowerInstance readyPower)
		{
			var powerDecision = powerDecisions[readyPower.Info.OrderName];
			if (powerDecision == null)
			{
				AIUtils.BotDebug("{0} couldn't find powerDecision for {1}", player.PlayerName, readyPower.Info.OrderName);
				return null;
			}

			var map = world.Map;
			var checkRadius = powerDecision.CoarseScanRadius;
			var suitableLocations = new List<(MPos UV, int Attractiveness)>();
			var totalAttractiveness = 0;

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
					if (consideredAttractiveness < powerDecision.MinimumAttractiveness)
						continue;

					suitableLocations.Add((tl, consideredAttractiveness));
					totalAttractiveness += consideredAttractiveness;
				}
			}

			if (suitableLocations.Count == 0)
				return null;

			// Pick a random location with above average attractiveness.
			var averageAttractiveness = totalAttractiveness / suitableLocations.Count;
			return suitableLocations.Shuffle(world.LocalRandom)
				.First(x => x.Attractiveness >= averageAttractiveness)
				.UV.ToCPos(map);
		}

		/// <summary>Detail scans an area, evaluating positions.</summary>
		CPos? FindFineAttackLocationToSupportPower(SupportPowerInstance readyPower, CPos checkPos, int extendedRange = 1)
		{
			CPos? bestLocation = null;
			var bestAttractiveness = 0;
			var powerDecision = powerDecisions[readyPower.Info.OrderName];
			if (powerDecision == null)
			{
				AIUtils.BotDebug("{0} couldn't find powerDecision for {1}", player.PlayerName, readyPower.Info.OrderName);
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

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			var waitingPowersNodes = waitingPowers
				.Select(kv => new MiniYamlNode(kv.Key.Key, FieldSaver.FormatValue(kv.Value)))
				.ToList();

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("WaitingPowers", "", waitingPowersNodes)
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, List<MiniYamlNode> data)
		{
			if (self.World.IsReplay)
				return;

			var waitingPowersNode = data.FirstOrDefault(n => n.Key == "WaitingPowers");
			if (waitingPowersNode != null)
			{
				foreach (var n in waitingPowersNode.Value.Nodes)
				{
					if (supportPowerManager.Powers.TryGetValue(n.Key, out var instance))
						waitingPowers[instance] = FieldLoader.GetValue<int>("WaitingPowers", n.Value.Value);
				}
			}
		}
	}
}
