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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum BotSupportPowerTriggerMode { Periodically, OnAttacked }

	public enum BotSupportPowerTargetLocationMode { CheckCenter, ActorLocation, ActorCenter, CellCanBeEnteredByTargetedActor }

	[Desc("Adds metadata for the AI bots.")]
	public class SupportPowerDecision
	{
		[Desc("What is the minimum attractiveness we will use this power for?")]
		public readonly int MinimumAttractiveness = 1;

		[Desc("What support power does this decision apply to?")]
		public readonly string OrderName = "AirstrikePowerInfoOrder";
		[Desc(
			"What is the coarse scan radius of this power?",
			$"For finding the general target area, before doing a detail scan when {nameof(DecisionMode)} is Periodically",
			$"For picking the area around attacked actor, before doing a detail scan when {nameof(DecisionMode)} is OnAttacked",
			$"Should be 10 or more to avoid lag, when {nameof(DecisionMode)} is Periodically")]
		public readonly int CoarseScanRadius = 20;

		[Desc(
			"What is the fine scan radius of this power?",
			"For doing a detailed scan in the general target area.",
			"Minimum is 1")]
		public readonly int FineScanRadius = 2;

		[FieldLoader.LoadUsing(nameof(LoadConsiderations))]
		[Desc("The decisions associated with this power")]
		public readonly FrozenDictionary<string, SupportPowerStrategy> Considerations = FrozenDictionary<string, SupportPowerStrategy>.Empty;

		[Desc("Minimum ticks to wait until next Decision scan attempt.")]
		public readonly int MinimumScanTimeInterval = 250;

		[Desc("Maximum ticks to wait until next Decision scan attempt.")]
		public readonly int MaximumScanTimeInterval = 262;

		[Desc("Extra data that put into support power order.")]
		public readonly uint ExtraData = uint.MaxValue;

		[Desc("The way of triggering the order.")]
		public readonly BotSupportPowerTriggerMode DecisionMode = BotSupportPowerTriggerMode.Periodically;

		[Desc("What kind of the location should the bot find as target?")]
		public readonly BotSupportPowerTargetLocationMode TargetLocationMode = BotSupportPowerTargetLocationMode.CheckCenter;

		[Desc("What kind of the location should the bot find as extra location?", "Extra location is used for support power act like Chronoshift.")]
		public readonly BotSupportPowerTargetLocationMode ExtraLocationMode = BotSupportPowerTargetLocationMode.CheckCenter;

		[Desc("When set to true, the bot will find target location earlier than extra location, and vice versa",
			"when earlier check is ActorLocation or ActorCenter and later check is CellCanBeEnteredByTargetedActor, the targetted actor will be passed from early check to later check.")]
		public readonly bool CheckTargetLocationFirst = true;

		[Desc("Visibility check")]
		public readonly bool VisibilityCheck = true;

		public SupportPowerDecision(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);
		}

		static object LoadConsiderations(MiniYaml yaml)
		{
			var ret = new Dictionary<string, SupportPowerStrategy>();
			foreach (var d in yaml.Nodes)
			{
				if (d.Key.Split('@')[0] == "Consideration")
				{
					var consideration = new Consideration(d.Value);
					if (!ret.ContainsKey(consideration.StrategyName))
						ret.Add(consideration.StrategyName, new SupportPowerStrategy());

					var strategy = ret[consideration.StrategyName];
					if (consideration.AsExtraLocation)
						strategy.ExtraPositionConsiderations.Add(consideration);
					else
						strategy.TargetPositionConsiderations.Add(consideration);
				}
			}

			return ret.ToFrozenDictionary();
		}

		/// <summary>Get a random strategy consists of a set considerations.</summary>
		public string GetRandomStrategy(int randomNumber)
		{
			randomNumber = Math.Abs(randomNumber);
			var names = Considerations.Keys.ToList();
			if (names.Count == 0)
				return null;
			return names[randomNumber % names.Count];
		}

		public bool NeedsConsiderTargetPosition(string strategyName)
		{
			return strategyName != null && Considerations.TryGetValue(strategyName, out var strategy) && strategy.TargetPositionConsiderations.Count > 0;
		}

		public bool NeedsConsiderExtraLocation(string strategyName)
		{
			return strategyName != null && Considerations.TryGetValue(strategyName, out var strategy) && strategy.ExtraPositionConsiderations.Count > 0;
		}

		/// <summary>Evaluates the attractiveness of a position according to all considerations.</summary>
		public (int Attractiveness, Actor TargetActor) GetAttractiveness(
			WPos pos,
			Player firedBy,
			string considerationName,
			bool asExtraPos = false)
		{
			var answer = 0;
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);

			if (!Considerations.TryGetValue(considerationName, out var strategy) && !world.Map.Contains(targetTile))
				return (0, null);

			var considerations = asExtraPos ? strategy.ExtraPositionConsiderations : strategy.TargetPositionConsiderations;
			var goodActors = new List<Actor>();
			foreach (var consideration in considerations)
			{
				var radiusToUse = new WDist(consideration.CheckRadius.Length);

				var checkActors = world.FindActorsInCircle(pos, radiusToUse);
				foreach (var scrutinized in checkActors)
				{
					var attractiveness = consideration.GetAttractiveness(scrutinized, firedBy, VisibilityCheck);

					if (attractiveness > 0)
						goodActors.Add(scrutinized);

					answer += attractiveness;
				}

				if (!VisibilityCheck)
					continue;

				var delta = new WVec(radiusToUse, radiusToUse, WDist.Zero);
				var tl = world.Map.CellContaining(pos - delta);
				var br = world.Map.CellContaining(pos + delta);
				var checkFrozen = firedBy.FrozenActorLayer.FrozenActorsInRegion(new CellRegion(world.Map.Grid.Type, tl, br));

				// IsValid check filters out Frozen Actors that have not initialized their Owner
				foreach (var scrutinized in checkFrozen)
					answer += consideration.GetAttractiveness(scrutinized, firedBy);
			}

			return (answer, goodActors.RandomOrDefault(world.LocalRandom));
		}

		/// <summary>Evaluates the attractiveness of an actor according to all considerations.</summary>
		public int GetAttractiveness(Actor actor, Player firedBy, string strategyName, bool asExtraPos = false)
		{
			var answer = 0;

			if (!Considerations.TryGetValue(strategyName, out var strategy))
				return 0;

			var considerations = asExtraPos ? strategy.ExtraPositionConsiderations : strategy.TargetPositionConsiderations;

			foreach (var consideration in considerations)
				answer += consideration.GetAttractiveness(actor, firedBy, VisibilityCheck);

			return answer;
		}

		/// <summary>Evaluates the attractiveness of a group of actors according to all considerations.</summary>
		public int GetAttractiveness(IEnumerable<Actor> actors, Player firedBy, string considerationName, bool asExtraPos = false)
		{
			var answer = 0;

			if (!Considerations.TryGetValue(considerationName, out var strategy))
				return 0;

			var considerations = asExtraPos ? strategy.ExtraPositionConsiderations : strategy.TargetPositionConsiderations;

			foreach (var consideration in considerations)
				foreach (var scrutinized in actors)
					answer += consideration.GetAttractiveness(scrutinized, firedBy, VisibilityCheck);

			return answer;
		}

		public int GetAttractiveness(IEnumerable<FrozenActor> frozenActors, Player firedBy, string considerationName, bool asExtraPos = false)
		{
			if (!VisibilityCheck)
				return 0;

			if (!Considerations.TryGetValue(considerationName, out var strategy))
				return 0;

			var answer = 0;

			var considerations = asExtraPos ? strategy.ExtraPositionConsiderations : strategy.TargetPositionConsiderations;
			foreach (var consideration in considerations)
				foreach (var scrutinized in frozenActors)
					if (scrutinized.IsValid && scrutinized.Visible)
						answer += consideration.GetAttractiveness(scrutinized, firedBy);

			return answer;
		}

		public int GetNextScanTime(World world, int minScanTime)
		{
			return Math.Max(world.LocalRandom.Next(MinimumScanTimeInterval, MaximumScanTimeInterval), minScanTime);
		}

		public class SupportPowerStrategy
		{
			public string Name;
			public List<Consideration> TargetPositionConsiderations = [];
			public List<Consideration> ExtraPositionConsiderations = [];
		}

		/// <summary>Makes up part of a decision, describing how to evaluate a target.</summary>
		public class Consideration
		{
			public enum DecisionMetric { Health, HealthLoss, Value, None }

			[Desc("The strategy name of the consideration", "Considerations of the same strategy will be considered together in one check run.")]
			public readonly string StrategyName = "primary";

			[Desc("This consideration is used as extra location in order", "Used for support power needs extra location.")]
			public readonly bool AsExtraLocation = false;

			[Desc("Against whom should this power be used?", "Allowed keywords: Ally, Neutral, Enemy.")]
			public readonly PlayerRelationship Against = PlayerRelationship.Enemy;

			[Desc("Only target actors belongs to this bot.")]
			public readonly bool AgainstOwnActors = false;

			[Desc("What target types should the desired targets of this power be?")]
			public readonly BitSet<TargetableType> ValidTargetTypes = [];

			[Desc("What target types should the undesired targets of this power be?")]
			public readonly BitSet<TargetableType> InvalidTargetTypes = [];

			[Desc("What types of actor should the desired targets of this power be?")]
			public readonly FrozenSet<string> ValidActorTypes = FrozenSet<string>.Empty;

			[Desc("What types of actor should the undesired targets of this power be?")]
			public readonly FrozenSet<string> InvalidActorTypes = FrozenSet<string>.Empty;

			[Desc("How attractive are these types of targets?")]
			public readonly int Attractiveness = 100;

			[Desc("Weight the target attractiveness by this property", "Allowed keywords: Health, HealthLoss, Value, None")]
			public readonly DecisionMetric TargetMetric = DecisionMetric.None;

			[Desc("What is the check radius of this decision?")]
			public readonly WDist CheckRadius = WDist.FromCells(5);

			public Consideration(MiniYaml yaml)
			{
				FieldLoader.Load(this, yaml);
			}

			/// <summary>Evaluates a single actor according to the rules defined in this consideration.</summary>
			public int GetAttractiveness(Actor a, Player firedBy, bool visibilityCheck = true)
			{
				if (a == null || a.IsDead)
					return 0;

				if ((ValidActorTypes.Count > 0 && !ValidActorTypes.Contains(a.Info.Name)) || (InvalidActorTypes.Count > 0 && InvalidActorTypes.Contains(a.Info.Name)))
					return 0;

				if ((AgainstOwnActors && a.Owner != firedBy) || (!AgainstOwnActors && !Against.HasRelationship(firedBy.RelationshipWith(a.Owner))))
					return 0;

				if (visibilityCheck && !a.CanBeViewedByPlayer(firedBy))
					return 0;

				if ((!a.IsTargetableBy(firedBy.PlayerActor)) && ((!ValidTargetTypes.IsEmpty) || !InvalidTargetTypes.IsEmpty))
					return 0;

				if ((ValidTargetTypes.IsEmpty || ValidTargetTypes.Overlaps(a.GetEnabledTargetTypes())) &&
					(InvalidTargetTypes.IsEmpty || !InvalidTargetTypes.Overlaps(a.GetEnabledTargetTypes())))
				{
					switch (TargetMetric)
					{
						case DecisionMetric.Value:
							var valueInfo = a.Info.TraitInfoOrDefault<ValuedInfo>();
							return (valueInfo != null) ? valueInfo.Cost * Attractiveness : 0;

						case DecisionMetric.Health:
						case DecisionMetric.HealthLoss:

							var health = a.TraitOrDefault<IHealth>();

							if (health == null)
								return 0;

							var healthneed = TargetMetric == DecisionMetric.Health ? health.HP : (health.MaxHP - health.HP);

							// Cast to long to avoid overflow when multiplying by the health
							return (int)((long)healthneed * Attractiveness / health.MaxHP);

						default:
							return Attractiveness;
					}
				}

				return 0;
			}

			public int GetAttractiveness(FrozenActor fa, Player firedBy)
			{
				if ((AgainstOwnActors && fa.Owner != firedBy) || (!AgainstOwnActors && !Against.HasRelationship(firedBy.RelationshipWith(fa.Owner))))
					return 0;

				if (fa == null || !fa.IsValid || !fa.Visible)
					return 0;

				if ((ValidActorTypes.Count > 0 && !ValidActorTypes.Contains(fa.Info.Name)) || (InvalidActorTypes.Count > 0 && InvalidActorTypes.Contains(fa.Info.Name)))
					return 0;

				if ((ValidTargetTypes.IsEmpty || ValidTargetTypes.Overlaps(fa.TargetTypes)) && (InvalidTargetTypes.IsEmpty || !InvalidTargetTypes.Overlaps(fa.TargetTypes)))
				{
					switch (TargetMetric)
					{
						case DecisionMetric.Value:
							var valueInfo = fa.Info.TraitInfoOrDefault<ValuedInfo>();
							return (valueInfo != null) ? valueInfo.Cost * Attractiveness : 0;

						case DecisionMetric.Health:
							var healthInfo = fa.Info.TraitInfoOrDefault<IHealthInfo>();
							return (healthInfo != null) ? fa.HP * Attractiveness / healthInfo.MaxHP : 0;

						default:
							return Attractiveness;
					}
				}

				return 0;
			}
		}
	}
}
