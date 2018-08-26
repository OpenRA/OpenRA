#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class GrantConditionOnMovementInfo : ConditionalTraitInfo, Requires<IMoveInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Apply condition on straight vertical movement as well.")]
		public readonly bool ConsiderVerticalMovement = false;

		public override object Create(ActorInitializer init) { return new GrantConditionOnMovement(init.Self, this); }
	}

	public class GrantConditionOnMovement : ConditionalTrait<GrantConditionOnMovementInfo>, INotifyMoving
	{
		readonly IMove movement;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnMovement(Actor self, GrantConditionOnMovementInfo info)
			: base(info)
		{
			movement = self.Trait<IMove>();
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			base.Created(self);
		}

		void INotifyMoving.StartedMoving(Actor self)
		{
			if (conditionManager == null)
				return;

			if (!IsTraitDisabled && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);
		}

		void INotifyMoving.StoppedMoving(Actor self)
		{
			if (conditionManager == null || conditionToken == ConditionManager.InvalidConditionToken)
				return;

			if (!Info.ConsiderVerticalMovement || !movement.IsMovingVertically)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}

		void INotifyMoving.StartedMovingVertically(Actor self)
		{
			if (conditionManager == null)
				return;

			if (!IsTraitDisabled && Info.ConsiderVerticalMovement && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);
		}

		void INotifyMoving.StoppedMovingVertically(Actor self)
		{
			if (conditionManager == null || conditionToken == ConditionManager.InvalidConditionToken)
				return;

			if (!movement.IsMoving)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (conditionManager == null)
				return;

			var isMovingVertically = Info.ConsiderVerticalMovement ? movement.IsMovingVertically : false;
			var isMoving = movement.IsMoving || isMovingVertically;
			if (isMoving && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionManager == null || conditionToken == ConditionManager.InvalidConditionToken)
				return;

			conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}
	}
}
