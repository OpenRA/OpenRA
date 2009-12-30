using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class AttackHeli : AttackFrontal
	{
		public AttackHeli(Actor self) : base(self, 20) { }

		protected override void QueueAttack(Actor self, Order order)
		{
			self.CancelActivity();
			self.QueueActivity(new HeliAttack(order.TargetActor));
			target = order.TargetActor;
			// todo: fly home
		}
	}
}
