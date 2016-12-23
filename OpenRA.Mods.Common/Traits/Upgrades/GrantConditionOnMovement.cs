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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class GrantConditionOnMovementInfo : UpgradableTraitInfo, Requires<IMoveInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Apply upgrades on straight vertical movement as well.")]
		public readonly bool ConsiderVerticalMovement = false;

		public override object Create(ActorInitializer init) { return new GrantConditionOnMovement(init.Self, this); }
	}

	public class GrantConditionOnMovement : UpgradableTrait<GrantConditionOnMovementInfo>, ITick
	{
		readonly IMove movement;

		ConditionManager manager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnMovement(Actor self, GrantConditionOnMovementInfo info)
			: base(info)
		{
			movement = self.Trait<IMove>();
		}

		protected override void Created(Actor self)
		{
			manager = self.TraitOrDefault<ConditionManager>();
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (manager == null)
				return;

			var isMovingVertically = Info.ConsiderVerticalMovement ? movement.IsMovingVertically : false;
			var isMoving = !IsTraitDisabled && !self.IsDead && (movement.IsMoving || isMovingVertically);
			if (isMoving && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = manager.GrantCondition(self, Info.Condition);
			else if (!isMoving && conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = manager.RevokeCondition(self, conditionToken);
		}
	}
}
