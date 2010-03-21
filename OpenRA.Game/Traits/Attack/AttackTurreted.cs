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
	class AttackTurretedInfo : AttackBaseInfo
	{
		public override object Create(Actor self) { return new AttackTurreted( self ); }
	}

	class AttackTurreted : AttackBase, INotifyBuildComplete
	{
		public AttackTurreted(Actor self) : base(self) { }

		protected override bool CanAttack( Actor self )
		{
			if( self.traits.Contains<Building>() && !buildComplete )
				return false;

			var turreted = self.traits.Get<Turreted>();
			turreted.desiredFacing = Util.GetFacing( target.CenterLocation - self.CenterLocation, turreted.turretFacing );
			if( turreted.desiredFacing != turreted.turretFacing )
				return false;

			return base.CanAttack( self );
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			DoAttack( self );
		}

		protected override void QueueAttack( Actor self, Order order )
		{
			if (self.traits.Contains<Building>() && self.traits.Get<Building>().Disabled) 
				return;
			
			const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
			/* todo: choose the appropriate weapon, when only one works against this target */
			var weapon = order.Subject.GetPrimaryWeapon() ?? order.Subject.GetSecondaryWeapon();

			if (self.traits.Contains<Mobile>())
				self.QueueActivity( new Traits.Activities.Follow( order.TargetActor,
					Math.Max( 0, (int)weapon.Range - RangeTolerance ) ) );

			target = order.TargetActor;
			
		}

		bool buildComplete = false;
		public void BuildingComplete(Actor self) { buildComplete = true; }
	}
}
