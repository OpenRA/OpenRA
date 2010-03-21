using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class AttackTeslaInfo : AttackOmniInfo
	{
		public readonly int MaxCharges = 3;

		public override object Create( Actor self )
		{
			return new AttackTesla( self );
		}
	}

	class AttackTesla : AttackOmni, ITick
	{
		int charges;
		int timeToRecharge;

		public AttackTesla( Actor self )
			: base( self )
		{
			charges = self.Info.Traits.Get<AttackTeslaInfo>().MaxCharges;
		}

		protected override bool CanAttack( Actor self )
		{
			return base.CanAttack( self ) && ( charges > 0 );
		}

		public override void Tick( Actor self )
		{
			if( --timeToRecharge <= 0 )
				charges = self.Info.Traits.Get<AttackTeslaInfo>().MaxCharges;
			if( charges <= 0 )
			{
				primaryFireDelay = Math.Max( primaryFireDelay, timeToRecharge );
				secondaryFireDelay = Math.Max( secondaryFireDelay, timeToRecharge );
				sameTarget = null;
			}
			base.Tick( self );
		}

		Actor sameTarget;
		public override int FireDelay( Actor self, AttackBaseInfo info )
		{
			primaryFireDelay = 8;
			timeToRecharge = Rules.WeaponInfo[ info.PrimaryWeapon ].ROF;
			--charges;

			if( target != sameTarget )
			{
				sameTarget = target;
				self.traits.Get<RenderBuildingCharge>().PlayCharge( self );
				return base.FireDelay( self, info );
			}
			else
				return 3;
		}
	}
}
