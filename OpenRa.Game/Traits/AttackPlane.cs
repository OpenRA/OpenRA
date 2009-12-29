using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	// yet another ugly trait that does two things:
	//	- plane-specific attack order dispatch
	//	- forward-facing attack with a tolerance

	class AttackPlane : AttackBase
	{
		const int facingTolerance = 20;

		public AttackPlane(Actor self) : base(self) { }

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
			target = order.TargetActor;
			self.QueueActivity(new FlyAttack(order.TargetActor));
			self.QueueActivity(new ReturnToBase(self, null));
		}
	}
}
