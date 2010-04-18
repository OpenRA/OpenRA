#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;

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
			timeToRecharge = self.GetPrimaryWeapon().ROF;
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
