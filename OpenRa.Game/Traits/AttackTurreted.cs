using System;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class AttackTurretedInfo : AttackBaseInfo
	{
		public override object Create(Actor self) { return new AttackTurreted( self ); }
	}

	class AttackTurreted : AttackBase, INotifyBuildComplete
	{
		public AttackTurreted(Actor self) : base(self) { }

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if( !CanAttack( self ) ) return;

			if (self.traits.Contains<Building>() && !buildComplete)
				return;		/* base defenses can't do anything until they finish building !*/

			var turreted = self.traits.Get<Turreted>();
			turreted.desiredFacing = Util.GetFacing( target.CenterLocation - self.CenterLocation, turreted.turretFacing );
			if( turreted.desiredFacing != turreted.turretFacing )
				return;

			DoAttack( self );
		}

		protected override void QueueAttack( Actor self, Order order )
		{
			if (self.traits.Contains<Building>() && self.traits.Get<Building>().Disabled) 
				return;
			
			const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
			/* todo: choose the appropriate weapon, when only one works against this target */
			var weapon = order.Subject.LegacyInfo.Primary ?? order.Subject.LegacyInfo.Secondary;

			if (self.traits.Contains<Mobile>())
				self.QueueActivity( new Traits.Activities.Follow( order.TargetActor,
					Math.Max( 0, (int)Rules.WeaponInfo[ weapon ].Range - RangeTolerance ) ) );

			target = order.TargetActor;
			
		}

		bool buildComplete = false;
		public void BuildingComplete(Actor self) { buildComplete = true; }
	}
}
