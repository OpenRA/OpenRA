using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class HeliAttack : IActivity
	{
		Actor target;
		const int CruiseAltitude = 20;
		public HeliAttack( Actor target ) { this.target = target; }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead)
				return NextActivity;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return NextActivity;

			var unit = self.traits.Get<Unit>();

			if (unit.Altitude != CruiseAltitude)
			{
				unit.Altitude += Math.Sign(CruiseAltitude - unit.Altitude);
				return this;
			}

			var range = Rules.WeaponInfo[ self.Info.Traits.WithInterface<AttackBaseInfo>().First().PrimaryWeapon ].Range - 1;
			var dist = target.CenterLocation - self.CenterLocation;

			var desiredFacing = Util.GetFacing(dist, unit.Facing);
			Util.TickFacing(ref unit.Facing, desiredFacing, self.Info.Traits.Get<HelicopterInfo>().ROT);

			if (!float2.WithinEpsilon(float2.Zero, dist, range * Game.CellSize))
			{
				var rawSpeed = .2f * Util.GetEffectiveSpeed(self);
				self.CenterLocation += (rawSpeed / dist.Length) * dist;
				self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();
			}

			/* todo: maintain seperation wrt other helis */
			return this;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
