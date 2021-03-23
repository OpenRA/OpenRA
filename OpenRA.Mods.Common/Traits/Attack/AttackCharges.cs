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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor must charge up its armaments before firing.")]
	public class AttackChargesInfo : AttackOmniInfo
	{
		[Desc("Amount of charge required to attack.")]
		public readonly int ChargeLevel = 25;

		[Desc("Amount to increase the charge level each tick with a valid target.")]
		public readonly int ChargeRate = 1;

		[Desc("Amount to decrease the charge level each tick without a valid target.")]
		public readonly int DischargeRate = 1;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while the charge level is greater than zero.")]
		public readonly string ChargingCondition = null;

		public override object Create(ActorInitializer init) { return new AttackCharges(init.Self, this); }
	}

	public class AttackCharges : AttackOmni, INotifyAttack, INotifySold
	{
		readonly AttackChargesInfo info;
		int chargingToken = Actor.InvalidConditionToken;
		bool charging;

		public int ChargeLevel { get; private set; }

		public AttackCharges(Actor self, AttackChargesInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override void Tick(Actor self)
		{
			// Stop charging when we lose our target
			charging &= self.CurrentActivity is SetTarget;

			var delta = charging ? info.ChargeRate : -info.DischargeRate;
			ChargeLevel = (ChargeLevel + delta).Clamp(0, info.ChargeLevel);

			if (ChargeLevel > 0 && chargingToken == Actor.InvalidConditionToken)
				chargingToken = self.GrantCondition(info.ChargingCondition);

			if (ChargeLevel == 0 && chargingToken != Actor.InvalidConditionToken)
				chargingToken = self.RevokeCondition(chargingToken);

			base.Tick(self);
		}

		protected override bool CanAttack(Actor self, in Target target)
		{
			charging = base.CanAttack(self, target) && IsReachableTarget(target, true);
			return ChargeLevel >= info.ChargeLevel && charging;
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel) { ChargeLevel = 0; }
		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }
		void INotifySold.Selling(Actor self) { ChargeLevel = 0; }
		void INotifySold.Sold(Actor self) { }
	}
}
