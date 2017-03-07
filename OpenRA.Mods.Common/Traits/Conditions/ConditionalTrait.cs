#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>Use as base class for *Info to subclass of UpgradableTrait. (See UpgradableTrait.)</summary>
	public abstract class ConditionalTraitInfo : IConditionConsumerInfo, IRulesetLoaded
	{
		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition to enable this trait.")]
		public readonly ConditionExpression RequiresCondition = null;

		public abstract object Create(ActorInitializer init);

		// HACK: A shim for all the ActorPreview code that used to query UpgradeMinEnabledLevel directly
		// This can go away after we introduce an InitialConditions ActorInit and have the traits query the
		// condition directly
		public bool EnabledByDefault { get; private set; }

		public virtual void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			EnabledByDefault = RequiresCondition == null || RequiresCondition.Evaluate(EmptyCondition.Instance) > 0;
		}
	}

	/// <summary>
	/// Abstract base for enabling and disabling trait using conditions.
	/// Requires basing *Info on UpgradableTraitInfo and using base(info) constructor.
	/// TraitEnabled will be called at creation if the trait starts enabled or does not use conditions.
	/// </summary>
	public abstract class ConditionalTrait<InfoType> : IConditionConsumer, IDisabledTrait, INotifyCreated, ISync where InfoType : ConditionalTraitInfo
	{
		public readonly InfoType Info;
		protected bool permanent = false;

		IEnumerable<string> IConditionConsumer.Conditions
		{
			get
			{
				if (Info.RequiresCondition != null)
					return Info.RequiresCondition.Variables;

				return Enumerable.Empty<string>();
			}
		}

		[Sync] public bool IsTraitDisabled { get; private set; }

		public ConditionalTrait(InfoType info)
		{
			Info = info;

			// Conditional traits will be enabled (if appropriate) by the ConditionManager
			// calling IConditionConsumer.ConditionsChanged at the end of INotifyCreated.
			IsTraitDisabled = Info.RequiresCondition != null;
		}

		protected virtual void Created(Actor self)
		{
			if (Info.RequiresCondition == null)
				TraitEnabled(self);
		}

		void INotifyCreated.Created(Actor self) { Created(self); }

		void IConditionConsumer.ConditionsChanged(Actor self, ICondition conditions)
		{
			if (Info.RequiresCondition == null)
				return;

			var wasDisabled = IsTraitDisabled;
			if (!wasDisabled && permanent)
				return;

			IsTraitDisabled = Info.RequiresCondition.Evaluate(conditions) <= 0;

			if (IsTraitDisabled != wasDisabled)
			{
				if (wasDisabled)
					TraitEnabled(self);
				else
					TraitDisabled(self);
			}
		}

		// Subclasses can add condition support by querying IsTraitDisabled and/or overriding these methods.
		protected virtual void TraitEnabled(Actor self) { }
		protected virtual void TraitDisabled(Actor self) { }
	}
}
