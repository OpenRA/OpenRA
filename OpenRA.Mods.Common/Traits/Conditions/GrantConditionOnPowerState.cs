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
	[Desc("Grants condition as long as a valid power state is maintained.")]
	public class GrantConditionOnPowerStateInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("PowerStates at which the condition is granted. Options are Normal, Low and Critical.")]
		public readonly PowerState ValidPowerStates = PowerState.Low | PowerState.Critical;

		public override object Create(ActorInitializer init) { return new GrantConditionOnPowerState(init.Self, this); }
	}

	public class GrantConditionOnPowerState : ConditionalTrait<GrantConditionOnPowerStateInfo>, INotifyOwnerChanged, INotifyPowerLevelChanged
	{
		PowerManager playerPower;
		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		bool validPowerState;

		public GrantConditionOnPowerState(Actor self, GrantConditionOnPowerStateInfo info)
			: base(info)
		{
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			conditionManager = self.TraitOrDefault<ConditionManager>();

			Update(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			Update(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			Update(self);
		}

		void Update(Actor self)
		{
			if (conditionManager == null)
				return;

			validPowerState = !IsTraitDisabled && Info.ValidPowerStates.HasFlag(playerPower.PowerState);

			if (validPowerState && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, Info.Condition);
			else if (!validPowerState && conditionToken != ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);
		}

		void INotifyPowerLevelChanged.PowerLevelChanged(Actor self)
		{
			Update(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerPower = newOwner.PlayerActor.Trait<PowerManager>();
			Update(self);
		}
	}
}
