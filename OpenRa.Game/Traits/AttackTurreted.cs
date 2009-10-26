using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class AttackTurreted : ITick, IOrder
	{
		public Actor target;

		// time (in frames) until each weapon can fire again.
		int primaryFireDelay = 0;
		int secondaryFireDelay = 0;

		public AttackTurreted( Actor self )	{ self.traits.Get<Turreted>(); }

		public void Tick(Actor self)
		{
			if (primaryFireDelay > 0) --primaryFireDelay;
			if (secondaryFireDelay > 0) --secondaryFireDelay;

			if (target != null && target.IsDead) target = null;		/* he's dead, jim. */
			if (target == null) return;

			var turreted = self.traits.Get<Turreted>();
			turreted.desiredFacing = Util.GetFacing(target.CenterLocation - self.CenterLocation, turreted.turretFacing);
			if (turreted.desiredFacing != turreted.turretFacing)
				return;

			if (self.unitInfo.Primary != null && CheckFire(self, self.unitInfo.Primary, ref primaryFireDelay))
			{
				secondaryFireDelay = Math.Max(4, secondaryFireDelay);
				return;
			}
			if (self.unitInfo.Secondary != null && CheckFire(self, self.unitInfo.Secondary, ref secondaryFireDelay))
				return;
		}

		bool CheckFire( Actor self, string weaponName, ref int fireDelay )
		{
			if( fireDelay > 0 ) return false;
			var weapon = Rules.WeaponInfo[ weaponName ];
			if( weapon.Range * weapon.Range < (target.Location - self.Location).LengthSquared ) return false;

			// FIXME: rules specifies ROF in 1/15 sec units; ticks are 1/25 sec
			fireDelay = weapon.ROF;

			Game.world.Add( new Bullet( weaponName, self.Owner, self, 
				self.CenterLocation.ToInt2(), 
				target.CenterLocation.ToInt2() ) );

			return true;
		}

		public Order Order( Actor self, int2 xy, bool lmb, Actor underCursor )
		{
			if( underCursor == null ) return null;

			if( underCursor.Owner == self.Owner ) return null;

			return new AttackOrder( self, underCursor );
		}
	}

	class AttackOrder : Order
	{
		public readonly Actor Attacker;
		public readonly Actor Target;

		public AttackOrder( Actor attacker, Actor target )
		{
			this.Attacker = attacker;
			this.Target = target;
		}
		public override void Apply()
		{
			Attacker.traits.Get<AttackTurreted>().target = Target;
		}
	}
}
