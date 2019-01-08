#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public class GrantConditionWhileAimingInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant while aiming.")]
		public readonly string Condition = null;

		object ITraitInfo.Create(ActorInitializer init) { return new GrantConditionWhileAiming(this); }
	}

	public class GrantConditionWhileAiming : INotifyCreated, INotifyAiming
	{
		readonly GrantConditionWhileAimingInfo info;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionWhileAiming(GrantConditionWhileAimingInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void INotifyAiming.StartedAiming(Actor self, AttackBase attack)
		{
			if (conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, info.Condition);
		}

		void INotifyAiming.StoppedAiming(Actor self, AttackBase attack)
		{
			if (conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}
	}
}
