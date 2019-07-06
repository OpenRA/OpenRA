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

using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor must charge up its armaments before firing.")]
	public class AttackChargesInfo : AttackFollowInfo
	{
		[Desc("Amount of charge required to attack.")]
		public readonly int ChargeLevel = 25;

		[Desc("Amount of charge retained while not attacking.")]
		public readonly int MinChargeLevel = 0;

		[Desc("Amount to increase the charge level each tick with a valid target.")]
		public readonly int ChargeRate = 1;

		[Desc("Amount to decrease the charge level each tick without a valid target.")]
		public readonly int DischargeRate = 1;

		[Desc("How many charges this actor has to attack with, once charged.")]
		public readonly int MaxCharges = 1;

		[Desc("Delay between charge attacks (in ticks).")]
		public readonly int ChargeDelay = 3;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while the charge level is greater than MinChargeLevel.")]
		public readonly string ChargingCondition = null;

		public override object Create(ActorInitializer init) { return new AttackCharges(init.Self, this); }
	}

	public class AttackCharges : AttackFollow, INotifyAttack, INotifySold
	{
		readonly AttackChargesInfo info;
		ConditionManager conditionManager;
		int chargingToken = ConditionManager.InvalidConditionToken;
		bool charging;

		[Sync]
		int charges;

		[Sync]
		int timeToNextCharge;

		public int ChargeLevel { get; private set; }

		public AttackCharges(Actor self, AttackChargesInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();

			ChargeLevel = info.MinChargeLevel;

			base.Created(self);
		}

		protected override void Tick(Actor self)
		{
			// Stop charging when we lose our target
			charging &= (RequestedTarget.Type != TargetType.Invalid) || (OpportunityTarget.Type != TargetType.Invalid);

			var chargePrev = ChargeLevel;

			if (charging)
				ChargeLevel = (ChargeLevel + info.ChargeRate).Clamp(0, info.ChargeLevel);
			else
			{
				if (ChargeLevel < info.MinChargeLevel)
					ChargeLevel = (ChargeLevel + info.ChargeRate).Clamp(0, info.MinChargeLevel);
				else
					ChargeLevel = (ChargeLevel - info.DischargeRate).Clamp(info.MinChargeLevel, info.ChargeLevel);
			}

			if ((ChargeLevel == info.ChargeLevel) && (chargePrev < info.ChargeLevel))
				charges = info.MaxCharges;

			if (timeToNextCharge > 0)
				--timeToNextCharge;

			if (ChargeLevel > info.MinChargeLevel && conditionManager != null && !string.IsNullOrEmpty(info.ChargingCondition)
					&& chargingToken == ConditionManager.InvalidConditionToken)
				chargingToken = conditionManager.GrantCondition(self, info.ChargingCondition);

			if (ChargeLevel <= info.MinChargeLevel && conditionManager != null && chargingToken != ConditionManager.InvalidConditionToken)
				chargingToken = conditionManager.RevokeCondition(self, chargingToken);

			base.Tick(self);
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			charging = base.CanAttack(self, target) && IsReachableTarget(target, true);
			return (charges > 0) && (timeToNextCharge == 0) && charging;
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			--charges;
			ChargeLevel = 0;
			timeToNextCharge = info.ChargeDelay;
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }
		void INotifySold.Selling(Actor self) { ChargeLevel = 0; }
		void INotifySold.Sold(Actor self) { }
	}
}
