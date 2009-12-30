using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class AttackPlane : AttackFrontal
	{
		public AttackPlane(Actor self) : base(self, 20) { }

		protected override void QueueAttack(Actor self, Order order)
		{
			self.CancelActivity();
			target = order.TargetActor;
			self.QueueActivity(new FlyAttack(order.TargetActor));
			self.QueueActivity(new ReturnToBase(self, null));
		}
	}
}
