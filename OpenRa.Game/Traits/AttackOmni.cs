using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits
{
	class AttackOmniInfo : AttackBaseInfo
	{
		public override object Create(Actor self) { return new AttackOmni(self); }
	}

	class AttackOmni : AttackBase, INotifyBuildComplete
	{
		bool buildComplete = false;
		public void BuildingComplete(Actor self) { buildComplete = true; }

		public AttackOmni(Actor self) : base(self) { }

		public override void Tick(Actor self)
		{
			base.Tick(self);
			
			if (!CanAttack(self)) return;
			if (self.traits.Contains<Building>() && !buildComplete) return;

			DoAttack(self);
		}

		protected override void QueueAttack(Actor self, Order order)
		{
			target = order.TargetActor;
		}
	}
}
