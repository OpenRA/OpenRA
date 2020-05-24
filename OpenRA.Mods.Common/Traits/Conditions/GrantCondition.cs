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

namespace OpenRA.Mods.Common.Traits
{
	#pragma warning disable CS0649
	[Desc("Grants a condition while the trait is active.")]
	class GrantConditionInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Condition to grant.")]
		public readonly GrantedVariableReference<bool> Condition;

		[Desc("Is the condition irrevocable once it has been activated?")]
		public readonly bool GrantPermanently = false;

		public override object Create(ActorInitializer init) { return new GrantCondition(this); }
	}
	#pragma warning restore CS0649

	class GrantCondition : ConditionalTrait<GrantConditionInfo>
	{
		int conditionToken = Actor.InvalidConditionToken;

		public GrantCondition(GrantConditionInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.Grant(Info.Condition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (Info.GrantPermanently || conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
