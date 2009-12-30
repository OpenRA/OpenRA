using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	abstract class AttackFrontal : AttackBase
	{
		public AttackFrontal(Actor self, int facingTolerance)
			: base(self) { FacingTolerance = facingTolerance; }

		int FacingTolerance;

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (target == null) return;

			var unit = self.traits.Get<Unit>();
			var facingToTarget = Util.GetFacing(target.CenterLocation - self.CenterLocation, unit.Facing);

			if (Math.Abs(facingToTarget - unit.Facing) % 256 < FacingTolerance)
				DoAttack(self);
		}
	}
}
