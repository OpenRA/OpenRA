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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class GrantConditionOnLowPowerInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnLowPower(init.Self, this); }
	}

	public class GrantConditionOnLowPower : ConditionalTrait<GrantConditionOnLowPowerInfo>, ITick, INotifyOwnerChanged
	{
		PowerManager playerPower;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnLowPower(Actor self, GrantConditionOnLowPowerInfo info)
			: base(info)
		{
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (conditionManager == null)
				return;

			bool isLowPower = playerPower.PowerProvided < playerPower.PowerDrained;

			if (!IsTraitDisabled && isLowPower && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);
			else if (IsTraitDisabled || (!isLowPower && conditionToken != ConditionManager.InvalidConditionToken))
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerPower = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
