#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackTeslaInfo : AttackOmniInfo
	{
		public readonly int MaxCharges = 3;
		public readonly int ReloadTime = 120;
		public override object Create(ActorInitializer init) { return new AttackTesla(init.self); }
	}

	class AttackTesla : AttackOmni, ITick, INotifyAttack, ISync
	{
		[Sync] int charges;
		[Sync] int timeToRecharge;

		public AttackTesla( Actor self )
			: base( self )
		{
			charges = self.Info.Traits.Get<AttackTeslaInfo>().MaxCharges;
		}

		public override void Tick( Actor self )
		{
			if( --timeToRecharge <= 0 )
				charges = self.Info.Traits.Get<AttackTeslaInfo>().MaxCharges;

			base.Tick( self );
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			--charges;
			timeToRecharge = self.Info.Traits.Get<AttackTeslaInfo>().ReloadTime;
		}

		public override Activity GetAttackActivity( Actor self, Target newTarget, bool allowMove )
		{
			return new TeslaAttack( newTarget );
		}
		

		public override void ResolveOrder(Actor self, Order order)
		{
			base.ResolveOrder(self, order);

			if (order.OrderString == "Stop")
				self.CancelActivity();
		}

		class TeslaAttack : Activity
		{
			readonly Target target;
			public TeslaAttack( Target target ) { this.target = target; }

			public override Activity Tick( Actor self )
			{
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				var attack = self.Trait<AttackTesla>();
				if( attack.charges == 0 || !attack.CanAttack( self, target ) )
					return this;

				self.Trait<RenderBuildingCharge>().PlayCharge(self);
				return Util.SequenceActivities( new Wait( 22 ), new TeslaZap( target ), this );
			}
		}

		class TeslaZap : Activity
		{
			readonly Target target;
			public TeslaZap( Target target ) { this.target = target; }

			public override Activity Tick( Actor self )
			{
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				var attack = self.Trait<AttackTesla>();
				if( attack.charges == 0 ) return NextActivity;

				attack.DoAttack( self, target );

				return Util.SequenceActivities( new Wait( 3 ), this );
			}
		}
	}
}
