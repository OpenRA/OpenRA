#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Charges up before being able to attack.")]
	class AttackChargeInfo : AttackOmniInfo
	{
		[Desc("How many charges this actor has to attack with, once charged.")]
		public readonly int MaxCharges = 1;

		[Desc("Reload time for all charges (in ticks).")]
		public readonly int ReloadTime = 120;

		[Desc("Delay for initial charge attack (in ticks).")]
		public readonly int InitialChargeDelay = 22;

		[Desc("Delay between charge attacks (in ticks).")]
		public readonly int ChargeDelay = 3;

		public override object Create(ActorInitializer init) { return new AttackCharge(init.Self, this); }
	}

	class AttackCharge : AttackOmni, ITick, INotifyAttack, ISync
	{
		readonly AttackChargeInfo info;

		[Sync] int charges;
		[Sync] int timeToRecharge;

		public AttackCharge(Actor self, AttackChargeInfo info)
			: base(self, info)
		{
			this.info = info;
			charges = info.MaxCharges;
		}

		public void Tick(Actor self)
		{
			if (--timeToRecharge <= 0)
				charges = info.MaxCharges;
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (!IsReachableTarget(target, true))
				return false;

			return base.CanAttack(self, target);
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			--charges;
			timeToRecharge = info.ReloadTime;
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			return new ChargeAttack(this, newTarget);
		}

		public override void ResolveOrder(Actor self, Order order)
		{
			base.ResolveOrder(self, order);

			if (order.OrderString == "Stop")
				self.CancelActivity();
		}

		class ChargeAttack : Activity
		{
			readonly AttackCharge attack;
			readonly Target target;

			public ChargeAttack(AttackCharge attack, Target target) 
			{ 
				this.attack = attack;
				this.target = target; 
			}

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !attack.CanAttack(self, target))
					return NextActivity;

				if (attack.charges == 0)
					return this;

				self.Trait<RenderBuildingCharge>().PlayCharge(self);

				return Util.SequenceActivities(new Wait(attack.info.InitialChargeDelay), new ChargeFire(attack, target), this);
			}
		}

		class ChargeFire : Activity
		{
			readonly AttackCharge attack;
			readonly Target target;

			public ChargeFire(AttackCharge attack, Target target) 
			{ 
				this.attack = attack;
				this.target = target; 
			}

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !attack.CanAttack(self, target))
					return NextActivity;

				if (attack.charges == 0)
					return NextActivity;

				attack.DoAttack(self, target);

				return Util.SequenceActivities(new Wait(attack.info.ChargeDelay), this);
			}
		}
	}
}
