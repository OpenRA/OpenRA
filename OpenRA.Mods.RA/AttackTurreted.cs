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
			if( self.HasTrait<Building>() && !buildComplete )
				return false;

			if (!target.IsValid) return false;
			var turreted = self.Trait<Turreted>();
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
			if (self.HasTrait<Building>() && self.Trait<Building>().Disabled) 
				return;
			
			const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
			var weapon = ChooseWeaponForTarget(Target.FromOrder(order));
			if (weapon == null)
				return;

			target = Target.FromOrder(order);

			if (self.HasTrait<Mobile>() && !self.Info.Traits.Get<MobileInfo>().OnRails)
				self.QueueActivity( new Follow( target,
					Math.Max( 0, (int)weapon.Info.Range - RangeTolerance ) ) );
		}

		bool buildComplete = false;
		public void BuildingComplete(Actor self) { buildComplete = true; }
	}
}
