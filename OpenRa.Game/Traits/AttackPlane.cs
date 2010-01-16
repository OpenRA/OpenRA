using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class AttackPlaneInfo : AttackBaseInfo
	{
		public override object Create(Actor self) { return new AttackPlane(self); }
	}

	class AttackPlane : AttackFrontal
	{
		public AttackPlane(Actor self) : base(self, 20) { }

		protected override void QueueAttack(Actor self, Order order)
		{
			target = order.TargetActor;
			self.QueueActivity(new FlyAttack(order.TargetActor));
			self.QueueActivity(new ReturnToBase(self, null));
			self.QueueActivity(new Rearm());
		}
	}
}
