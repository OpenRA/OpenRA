using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Orders;

namespace OpenRa.Traits
{
	class IronCurtainPowerInfo : SupportPowerInfo
	{
		public readonly float Duration = 0f;
		public override object Create(Actor self) { return new IronCurtainPower(self, this); }
	}

	class IronCurtainPower : SupportPower, IResolveOrder
	{
		public IronCurtainPower(Actor self, IronCurtainPowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { Sound.Play("ironchg1.aud"); }
		protected override void OnFinishCharging() { Sound.Play("ironrdy1.aud"); }
		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new IronCurtainOrderGenerator(this);
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "IronCurtain")
			{
				order.TargetActor.traits.Get<IronCurtainable>().Activate(order.TargetActor,
					(int)((Info as IronCurtainPowerInfo).Duration * 25 * 60));
				Game.controller.CancelInputMode();
				FinishActivate();
			}
		}
	}

	// tag trait for the building
	class IronCurtainInfo : StatelessTraitInfo<IronCurtain> { }
	class IronCurtain { }
}
