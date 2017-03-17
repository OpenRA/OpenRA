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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition while the trait is active.")]
	class GrantConditionInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Is the condition irrevocable once it has been activated?")]
		public readonly bool GrantPermanently = false;

		public override object Create(ActorInitializer init) { return new GrantCondition(this); }
	}

	class GrantCondition<TraitInfo> : ConditionalTrait<TraitInfo> where TraitInfo : GrantConditionInfo
	{
		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantCondition(TraitInfo info)
			: base(info) { permanent = info.GrantPermanently; }

		protected override void Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();

			base.Created(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionToken == ConditionManager.InvalidConditionToken)
				return;

			conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}
	}

	class GrantCondition : GrantCondition<GrantConditionInfo>
	{
		public GrantCondition(GrantConditionInfo info) : base(info) { }
	}
}