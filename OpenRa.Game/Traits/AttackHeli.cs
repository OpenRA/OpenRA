using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class AttackHeli : AttackBase
	{
		public AttackHeli(Actor self) : base(self) { }

		const int facingTolerance = 20;
		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (target == null) return;

			var unit = self.traits.Get<Unit>();
			var facingToTarget = Util.GetFacing(target.CenterLocation - self.CenterLocation, unit.Facing);

			if (Math.Abs(facingToTarget - unit.Facing) % 256 < facingTolerance)
				DoAttack(self);
		}

		protected override void QueueAttack(Actor self, Order order)
		{
			self.CancelActivity();
			self.QueueActivity(new HeliAttack(order.TargetActor));
			target = order.TargetActor;
			// todo: fly home
		}
	}
}
