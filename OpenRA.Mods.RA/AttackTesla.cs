#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	class AttackTeslaInfo : AttackOmniInfo
	{
		public readonly int MaxCharges = 3;
		public readonly int ReloadTime = 120;
		public override object Create(ActorInitializer init) { return new AttackTesla(init.self); }
	}

	class AttackTesla : AttackOmni, ITick, INotifyAttack
	{
		int charges;
		int timeToRecharge;

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

		public void Attacking(Actor self, Target target)
		{
			--charges;
			timeToRecharge = self.Info.Traits.Get<AttackTeslaInfo>().ReloadTime;
		}

		public override IActivity GetAttackActivity( Actor self, Target newTarget, bool allowMove )
		{
			return new TeslaAttack( newTarget );
		}

		class TeslaAttack : CancelableActivity
		{
			readonly Target target;
			public TeslaAttack( Target target ) { this.target = target; }

			public override IActivity Tick( Actor self )
			{
				if( IsCanceled || !target.IsValid ) return NextActivity;

				var attack = self.Trait<AttackTesla>();
				if( attack.charges == 0 || !attack.CanAttack( self, target ) )
					return this;

				self.Trait<RenderBuildingCharge>().PlayCharge(self);
				return Util.SequenceActivities( new Wait( 8 ), new TeslaZap( target ), this );
			}
		}

		class TeslaZap : CancelableActivity
		{
			readonly Target target;
			public TeslaZap( Target target ) { this.target = target; }

			public override IActivity Tick( Actor self )
			{
				if( IsCanceled || !target.IsValid ) return NextActivity;

				var attack = self.Trait<AttackTesla>();
				if( attack.charges == 0 ) return NextActivity;

				attack.DoAttack( self, target );

				return Util.SequenceActivities( new Wait( 3 ), this );
			}
		}
	}
}
