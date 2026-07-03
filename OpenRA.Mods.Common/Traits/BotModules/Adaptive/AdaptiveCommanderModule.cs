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
	[Desc("Utility-AI commander for Adaptive AI.")]
	public sealed class AdaptiveCommanderModuleInfo : ConditionalTraitInfo
	{
		[Desc("Delay (in ticks) between strategic decisions.")]
		public readonly int DecisionInterval = 75;

		public override object Create(ActorInitializer init) { return new AdaptiveCommanderModule(init.Self, this); }
	}

	public sealed class AdaptiveCommanderModule : ConditionalTrait<AdaptiveCommanderModuleInfo>, IBotTick, IBotEnabled
	{
		readonly Actor self;
		readonly World world;
		readonly Player player;

		BotIntelModule intel;
		AdaptiveAILobbySettings lobbySettings;
		int decisionTicks;

		public StrategicPlan Plan { get; } = new();

		public AdaptiveCommanderModule(Actor self, AdaptiveCommanderModuleInfo info)
			: base(info)
		{
			this.self = self;
			world = self.World;
			player = self.Owner;
		}

		void IBotEnabled.BotEnabled(IBot bot)
		{
			intel = self.TraitsImplementing<BotIntelModule>().FirstOrDefault(t => t.IsTraitEnabled());
			lobbySettings = world.WorldActor.TraitOrDefault<AdaptiveAILobbySettings>();
			decisionTicks = world.LocalRandom.Next(0, Info.DecisionInterval);

			if (lobbySettings?.CashBonus > 0)
				player.PlayerActor.Trait<PlayerResources>().GiveCash(lobbySettings.CashBonus);
		}

		void IBotTick.BotTick(IBot bot)
		{
			var interval = (int)(Info.DecisionInterval * (lobbySettings?.DecisionIntervalMultiplier ?? 1f));
			if (--decisionTicks > 0)
				return;

			decisionTicks = interval;
			UpdatePlan();
		}

		void UpdatePlan()
		{
			Plan.UnitMixOverrides.Clear();
			Plan.BuildingBoosts.Clear();
			Plan.ScoutActive = false;

			var threat = intel?.GetPrimaryThreat() ?? new ThreatProfile();
			var staleIntel = intel?.TicksSinceLastSighting ?? int.MaxValue;
			var staleThreshold = lobbySettings?.IntelStaleTicks ?? 750;

			if (staleIntel > staleThreshold)
				Plan.ScoutActive = true;

			var scores = new (AdaptiveGoal Goal, float Score)[]
			{
				(AdaptiveGoal.Scout, staleIntel > staleThreshold ? 2f : 0f),
				(AdaptiveGoal.CounterAir, threat.Air * 2f),
				(AdaptiveGoal.CounterArmor, threat.Armor * 1.5f),
				(AdaptiveGoal.Defend, threat.LastContactLocation.HasValue ? 1.5f : 0f),
				(AdaptiveGoal.Expand, (lobbySettings?.ExpansionAggression ?? 1f) * (threat.Air + threat.Armor == 0 ? 1f : 0.3f)),
				(AdaptiveGoal.Attack, ComputeAttackScore(threat)),
				(AdaptiveGoal.Tech, 0.5f),
			};

			Plan.ActiveGoal = scores.MaxBy(s => s.Score).Goal;
			Plan.AttackReadiness = ComputeAttackScore(threat) / 3f * (lobbySettings?.AttackReadinessMultiplier ?? 1f);
			Plan.ExpansionAllowed = Plan.ActiveGoal == AdaptiveGoal.Expand || threat.Air + threat.Armor < 3;

			ApplyGoalEffects(threat);
		}

		float ComputeAttackScore(ThreatProfile threat)
		{
			var armySize = world.Actors.Count(a => a.Owner == player && a.Info.HasTraitInfo<AttackBaseInfo>() && !a.IsDead);
			var enemyPower = threat.Air + threat.Armor + threat.Infantry;
			var aggression = lobbySettings?.CombatAggression ?? 1f;
			return (armySize * 0.2f + (enemyPower == 0 ? 1f : 0f)) * aggression;
		}

		void ApplyGoalEffects(ThreatProfile threat)
		{
			if (lobbySettings?.CounterBuildEnabled != true)
				return;

			switch (Plan.ActiveGoal)
			{
				case AdaptiveGoal.CounterAir:
					Plan.UnitMixOverrides["e3"] = 40;
					Plan.BuildingBoosts["sam"] = 20;
					Plan.BuildingBoosts["agun"] = 10;
					Plan.DefensePriority = "sam";
					break;
				case AdaptiveGoal.CounterArmor:
					Plan.UnitMixOverrides["3tnk"] = 30;
					Plan.UnitMixOverrides["4tnk"] = 20;
					Plan.UnitMixOverrides["e2"] = 15;
					Plan.DefensePriority = "tsla";
					break;
				case AdaptiveGoal.Defend:
					Plan.BuildingBoosts["pbox"] = 15;
					Plan.BuildingBoosts["gun"] = 10;
					Plan.DefensePriority = "pbox";
					break;
				case AdaptiveGoal.Attack:
					Plan.AttackReadiness = Exts.Clamp(Plan.AttackReadiness + 0.3f, 0f, 1f);
					break;
			}

			if (lobbySettings.CounterBuildAggressive && threat.Air > 0)
				Plan.UnitMixOverrides["e3"] = 50;
		}
	}
}
