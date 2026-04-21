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
	public class GrantConditionOnMovementInfo : ConditionalTraitInfo, Requires<IMoveInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Apply condition on listed movement types. Available options are: None, Horizontal, Vertical, Turn.")]
		public readonly MovementType ValidMovementTypes = MovementType.Horizontal;

		public override object Create(ActorInitializer init) { return new GrantConditionOnMovement(init.Self, this); }
	}

	public class GrantConditionOnMovement : ConditionalTrait<GrantConditionOnMovementInfo>, INotifyMoving
	{
		readonly IMove movement;
		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnMovement(Actor self, GrantConditionOnMovementInfo info)
			: base(info)
		{
			movement = self.Trait<IMove>();
		}

		void UpdateCondition(Actor self, MovementType types)
		{
			var validMovement = !IsTraitDisabled && (types & Info.ValidMovementTypes) != 0;

			if (!validMovement && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
			else if (validMovement && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition);
		}

		void INotifyMoving.MovementTypeChanged(Actor self, MovementType types)
		{
			UpdateCondition(self, types);
		}

		protected override void TraitEnabled(Actor self)
		{
			UpdateCondition(self, movement.CurrentMovementTypes);
		}

		protected override void TraitDisabled(Actor self)
		{
			UpdateCondition(self, movement.CurrentMovementTypes);
		}
	}
}
