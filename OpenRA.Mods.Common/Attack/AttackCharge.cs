#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class AttackChargeInfo : AttackOmniInfo
	{
		public readonly int MaxCharges = 1;
		[Desc("Reload time (for all charges).")]
		public readonly int ReloadTime = 120;
		[Desc("Delay for first charge. Needs to match FireDelay for Obelisk.")]
		public readonly int InitialChargeDelay = 22;
		[Desc("Delay for additional charges if MaxCharge is larger than 1.")]
		public readonly int ChargeDelay = 3;
		public override object Create(ActorInitializer init) { return new AttackCharge(init.self, this); }
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
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				if (attack.charges == 0 || !attack.CanAttack(self, target))
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
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				if (attack.charges == 0)
					return NextActivity;

				attack.DoAttack(self, target);

				return Util.SequenceActivities(new Wait(attack.info.ChargeDelay), this);
			}
		}
	}
}
