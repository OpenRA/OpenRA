﻿#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
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
		public override object Create(ActorInitializer init) { return new AttackCharge(init.self); }
	}

	class AttackCharge : AttackOmni, ITick, INotifyAttack, ISync
	{
		[Sync] int charges;
		[Sync] int timeToRecharge;

		public AttackCharge(Actor self)
			: base(self)
		{
			charges = self.Info.Traits.Get<AttackChargeInfo>().MaxCharges;
		}

		public override void Tick( Actor self )
		{
			if( --timeToRecharge <= 0 )
				charges = self.Info.Traits.Get<AttackChargeInfo>().MaxCharges;

			base.Tick( self );
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			--charges;
			timeToRecharge = self.Info.Traits.Get<AttackChargeInfo>().ReloadTime;
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			return new ChargeAttack(newTarget);
		}
		

		public override void ResolveOrder(Actor self, Order order)
		{
			base.ResolveOrder(self, order);

			if (order.OrderString == "Stop")
				self.CancelActivity();
		}

		class ChargeAttack : Activity
		{
			readonly Target target;
			public ChargeAttack(Target target) 
			{ 
				this.target = target; 
			}

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;
				
				var initDelay = self.Info.Traits.Get<AttackChargeInfo>().InitialChargeDelay;
				
				var attack = self.Trait<AttackCharge>();
				if(attack.charges == 0 || !attack.CanAttack(self, target))
					return this;

				self.Trait<RenderBuildingCharge>().PlayCharge(self);
				return Util.SequenceActivities(new Wait(initDelay), new ChargeFire(target), this);
			}
		}

		class ChargeFire : Activity
		{
			readonly Target target;
			public ChargeFire(Target target) 
			{ 
				this.target = target; 
			}

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				var chargeDelay = self.Info.Traits.Get<AttackChargeInfo>().ChargeDelay;

				var attack = self.Trait<AttackCharge>();
				if(attack.charges == 0) 
					return NextActivity;

				attack.DoAttack(self, target);

				return Util.SequenceActivities(new Wait(chargeDelay), this);
			}
		}
	}
}
