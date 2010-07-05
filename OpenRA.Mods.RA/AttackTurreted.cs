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
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackTurretedInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackTurreted( init.self ); }
	}

	class AttackTurreted : AttackBase, INotifyBuildComplete
	{
		public AttackTurreted(Actor self) : base(self) { }

		protected override bool CanAttack( Actor self )
		{
			if( self.traits.Contains<Building>() && !buildComplete )
				return false;

			if (!target.IsValid) return false;
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
				self.QueueActivity( new Follow( order.TargetActor,
					Math.Max( 0, (int)weapon.Range - RangeTolerance ) ) );

			target = Target.FromActor(order.TargetActor);
			
		}

		bool buildComplete = false;
		public void BuildingComplete(Actor self) { buildComplete = true; }
	}
}
