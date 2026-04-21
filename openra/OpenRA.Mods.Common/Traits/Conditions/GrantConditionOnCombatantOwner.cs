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
	[Desc("Grants a condition if the owner is a combatant.")]
	public class GrantConditionOnCombatantOwnerInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnCombatantOwner(this); }
	}

	public class GrantConditionOnCombatantOwner : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantConditionOnCombatantOwnerInfo info;

		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnCombatantOwner(GrantConditionOnCombatantOwnerInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (!self.Owner.NonCombatant)
				conditionToken = self.GrantCondition(info.Condition);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			if (!newOwner.NonCombatant)
				conditionToken = self.GrantCondition(info.Condition);
		}
	}
}
