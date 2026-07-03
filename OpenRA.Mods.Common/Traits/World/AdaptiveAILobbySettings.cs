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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Runtime Adaptive AI lobby settings read at game start.")]
	public sealed class AdaptiveAILobbySettingsInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new AdaptiveAILobbySettings(this); }
	}

	public sealed class AdaptiveAILobbySettings : INotifyCreated
	{
		public float ProductionMultiplier = 1f;
		public float AttackReadinessMultiplier = 1f;
		public float DecisionIntervalMultiplier = 1f;
		public int CashBonus;
		public float CounterBuildSpeed = 1f;

		public int MaxScouts = 2;
		public int IntelStaleTicks = 750;

		public float ExpansionAggression = 1f;
		public float CombatAggression = 1f;
		public float SupportPowerMultiplier = 1f;

		public bool CounterBuildEnabled = true;
		public bool CounterBuildAggressive;

		public string TechLevel = "unrestricted";

		public AdaptiveAILobbySettings(AdaptiveAILobbySettingsInfo info) { }

		void INotifyCreated.Created(Actor self)
		{
			var gs = self.World.LobbyInfo.GlobalSettings;
			ApplyDifficulty(gs.OptionOrDefault("adaptive-difficulty", "medium"));
			ApplyScouting(gs.OptionOrDefault("adaptive-scouting", "normal"));
			ApplyCounterBuild(gs.OptionOrDefault("adaptive-counterbuild", "balanced"));
			ApplyExpansion(gs.OptionOrDefault("adaptive-expansion", "balanced"));
			ApplyAggression(gs.OptionOrDefault("adaptive-aggression", "balanced"));
			ApplySupportPowers(gs.OptionOrDefault("adaptive-supportpowers", "normal"));

			TechLevel = gs.OptionOrDefault("techlevel", "unrestricted");
		}

		void ApplyDifficulty(string value)
		{
			switch (value)
			{
				case "easy":
					ProductionMultiplier = 0.75f;
					AttackReadinessMultiplier = 0.6f;
					DecisionIntervalMultiplier = 1.5f;
					CounterBuildSpeed = 0.5f;
					break;
				case "hard":
					ProductionMultiplier = 1.25f;
					AttackReadinessMultiplier = 1.4f;
					DecisionIntervalMultiplier = 0.75f;
					CashBonus = 500;
					CounterBuildSpeed = 1.5f;
					break;
				case "brutal":
					ProductionMultiplier = 1.5f;
					AttackReadinessMultiplier = 1.8f;
					DecisionIntervalMultiplier = 0.5f;
					CashBonus = 1000;
					CounterBuildSpeed = 2f;
					break;
				default:
					ProductionMultiplier = 1f;
					AttackReadinessMultiplier = 1f;
					DecisionIntervalMultiplier = 1f;
					CounterBuildSpeed = 1f;
					break;
			}
		}

		void ApplyScouting(string value)
		{
			switch (value)
			{
				case "off":
					MaxScouts = 0;
					IntelStaleTicks = int.MaxValue;
					break;
				case "minimal":
					MaxScouts = 1;
					IntelStaleTicks = 1500;
					break;
				case "aggressive":
					MaxScouts = 3;
					IntelStaleTicks = 375;
					break;
				default:
					MaxScouts = 2;
					IntelStaleTicks = 750;
					break;
			}
		}

		void ApplyCounterBuild(string value)
		{
			CounterBuildEnabled = value != "off";
			CounterBuildAggressive = value == "aggressive";
		}

		void ApplyExpansion(string value)
		{
			ExpansionAggression = value switch
			{
				"defensive" => 0.5f,
				"greedy" => 1.5f,
				_ => 1f
			};
		}

		void ApplyAggression(string value)
		{
			CombatAggression = value switch
			{
				"passive" => 0.6f,
				"rush" => 1.4f,
				_ => 1f
			};
		}

		void ApplySupportPowers(string value)
		{
			SupportPowerMultiplier = value switch
			{
				"never" => 0f,
				"conservative" => 0.5f,
				"aggressive" => 1.5f,
				_ => 1f
			};
		}
	}
}
