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

using System.Collections.Generic;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>Use as base class for *Info to subclass of PausableConditionalTrait. (See PausableConditionalTrait.)</summary>
	public abstract class PausableConditionalTraitInfo : ConditionalTraitInfo
	{
		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition to pause this trait.")]
		public readonly BooleanExpression PauseOnCondition = null;

		public bool PausedByDefault { get; private set; }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);
			PausedByDefault = PauseOnCondition != null && PauseOnCondition.Evaluate(VariableExpression.NoVariables);
		}
	}

	/// <summary>
	/// Abstract base for enabling and disabling trait using conditions.
	/// Requires basing *Info on PausableConditionalTraitInfo and using base(info) constructor.
	/// TraitResumed will be called at creation if the trait starts not paused or does not have a pause condition.
	/// </summary>
	public abstract class PausableConditionalTrait<InfoType> : ConditionalTrait<InfoType> where InfoType : PausableConditionalTraitInfo
	{
		[Sync]
		public bool IsTraitPaused { get; private set; }

		protected PausableConditionalTrait(InfoType info)
			: base(info)
		{
			IsTraitPaused = info.PausedByDefault;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			if (Info.PauseOnCondition == null)
				TraitResumed(self);
		}

		// Overrides must call `base.GetVariableObservers()` to avoid breaking RequiresCondition or PauseOnCondition.
		public override IEnumerable<VariableObserver> GetVariableObservers()
		{
			foreach (var observer in base.GetVariableObservers())
				yield return observer;

			if (Info.PauseOnCondition != null)
				yield return new VariableObserver(PauseConditionsChanged, Info.PauseOnCondition.Variables);
		}

		void PauseConditionsChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			var wasPaused = IsTraitPaused;
			IsTraitPaused = Info.PauseOnCondition.Evaluate(conditions);

			if (IsTraitPaused != wasPaused)
			{
				if (wasPaused)
					TraitResumed(self);
				else
					TraitPaused(self);
			}
		}

		// Subclasses can add pause support by querying IsTraitPaused and/or overriding these methods.
		protected virtual void TraitResumed(Actor self) { }
		protected virtual void TraitPaused(Actor self) { }
	}
}
