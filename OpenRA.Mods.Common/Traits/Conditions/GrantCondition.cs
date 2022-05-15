#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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

		[Desc("Weight of the condition to grant.")]
		public readonly int Weight = 1;

		public override object Create(ActorInitializer init) { return new GrantCondition(this); }
	}

	class GrantCondition : ConditionalTrait<GrantConditionInfo>
	{
		int conditionToken = Actor.InvalidConditionToken;

		public GrantCondition(GrantConditionInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition, Info.Weight);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (Info.GrantPermanently || conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
