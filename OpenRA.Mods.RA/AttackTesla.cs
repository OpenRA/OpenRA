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

		protected override bool CanAttack( Actor self, Target target )
		{
			return base.CanAttack( self, target ) && ( charges > 0 );
		}

		public override void Tick( Actor self )
		{
			if( --timeToRecharge <= 0 )
				charges = self.Info.Traits.Get<AttackTeslaInfo>().MaxCharges;
			if( charges <= 0 )
			{
				foreach( var w in Weapons )
					w.FireDelay = Math.Max( w.FireDelay, timeToRecharge );

				previousTarget = null;
			}
			base.Tick( self );
		}

		Actor previousTarget;

		public override int FireDelay( Actor self, Target target, AttackBaseInfo info )
		{
			return target.Actor == previousTarget ? 3 : base.FireDelay(self, target, info);
		}

		public void Attacking(Actor self, Target target)
		{
			foreach (var w in Weapons)
				w.FireDelay = 8;

			timeToRecharge = self.Info.Traits.Get<AttackTeslaInfo>().ReloadTime;
			--charges;

			if (target.Actor != previousTarget)
			{
				previousTarget = target.Actor;
				self.Trait<RenderBuildingCharge>().PlayCharge(self);
			}
		}
	}
}
