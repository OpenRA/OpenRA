#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	[Desc("Adds metadata for the AI bots.")]
	public class SupportPowerDecision
	{
		[Desc("What is the minimum attractiveness we will use this power for?")]
		public readonly int MinimumAttractiveness = 1;

		[Desc("What support power does this decision apply to?")]
		public readonly string OrderName = "AirstrikePowerInfoOrder";

		[Desc("What is the coarse scan radius of this power?", "For finding the general target area, before doing a detail scan", "Should be 10 or more to avoid lag")]
		public readonly int CoarseScanRadius = 20;

		[Desc("What is the fine scan radius of this power?", "For doing a detailed scan in the general target area.", "Minimum is 1")]
		public readonly int FineScanRadius = 2;

		[FieldLoader.LoadUsing("LoadConsiderations")]
		[Desc("The decisions associated with this power")]
		public readonly List<Consideration> Considerations = new List<Consideration>();

		[Desc("Minimum ticks to wait until next Decision scan attempt.")]
		public readonly int MinimumScanTimeInterval = 250;

		[Desc("Maximum ticks to wait until next Decision scan attempt.")]
		public readonly int MaximumScanTimeInterval = 262;

		public SupportPowerDecision(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);
		}

		static object LoadConsiderations(MiniYaml yaml)
		{
			var ret = new List<Consideration>();
			foreach (var d in yaml.Nodes)
				if (d.Key.Split('@')[0] == "Consideration")
					ret.Add(new Consideration(d.Value));

			return ret;
		}

		/// <summary>Evaluates the attractiveness of a position according to all considerations</summary>
		public int GetAttractiveness(WPos pos, Player firedBy)
		{
			var answer = 0;
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);

			if (!world.Map.Contains(targetTile))
				return 0;

			foreach (var consideration in Considerations)
			{
				var radiusToUse = new WDist(consideration.CheckRadius.Length);

				var checkActors = world.FindActorsInCircle(pos, radiusToUse);
				foreach (var scrutinized in checkActors)
					answer += consideration.GetAttractiveness(scrutinized, firedBy.Stances[scrutinized.Owner], firedBy);
			}

			return answer;
		}

		/// <summary>Evaluates the attractiveness of a group of actors according to all considerations</summary>
		public int GetAttractiveness(IEnumerable<Actor> actors, Player firedBy)
		{
			var answer = 0;

			foreach (var consideration in Considerations)
				foreach (var scrutinized in actors)
					answer += consideration.GetAttractiveness(scrutinized, firedBy.Stances[scrutinized.Owner], firedBy);

			return answer;
		}

		public int GetNextScanTime(HackyAI ai) { return ai.Random.Next(MinimumScanTimeInterval, MaximumScanTimeInterval); }

		/// <summary>Makes up part of a decision, describing how to evaluate a target.</summary>
		public class Consideration
		{
			public enum DecisionMetric { Health, Value, None }

			[Desc("Against whom should this power be used?", "Allowed keywords: Ally, Neutral, Enemy")]
			public readonly Stance Against = Stance.Enemy;

			[Desc("What types should the desired targets of this power be?")]
			public readonly HashSet<string> Types = new HashSet<string> { "Air", "Ground", "Water" };

			[Desc("How attractive are these types of targets?")]
			public readonly int Attractiveness = 100;

			[Desc("Weight the target attractiveness by this property", "Allowed keywords: Health, Value, None")]
			public readonly DecisionMetric TargetMetric = DecisionMetric.None;

			[Desc("What is the check radius of this decision?")]
			public readonly WDist CheckRadius = WDist.FromCells(5);

			public Consideration(MiniYaml yaml)
			{
				FieldLoader.Load(this, yaml);
			}

			/// <summary>Evaluates a single actor according to the rules defined in this consideration</summary>
			public int GetAttractiveness(Actor a, Stance stance, Player firedBy)
			{
				if (stance != Against)
					return 0;

				if (a == null)
					return 0;

				if (!a.IsTargetableBy(firedBy.PlayerActor))
					return 0;

				if (Types.Overlaps(a.GetEnabledTargetTypes()))
				{
					switch (TargetMetric)
					{
						case DecisionMetric.Value:
							var valueInfo = a.Info.TraitInfoOrDefault<ValuedInfo>();
							return (valueInfo != null) ? valueInfo.Cost * Attractiveness : 0;

						case DecisionMetric.Health:
							var health = a.TraitOrDefault<Health>();
							return (health != null) ? (health.HP / health.MaxHP) * Attractiveness : 0;

						default:
							return Attractiveness;
					}
				}

				return 0;
			}
		}
	}
}
