using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Traits;

namespace OpenRa.Traits
{
	public class CanPowerDownInfo : ITraitInfo
	{
		public object Create(Actor self) { return new CanPowerDown(self); }
	}

	public class CanPowerDown : IDisable, IPowerModifier, IResolveOrder
	{
		readonly Actor self;
		[Sync]
		bool IsDisabled = false;
		
		public CanPowerDown(Actor self)
		{
			this.self = self;
		}

		public bool Disabled
		{
			get { return IsDisabled; }
			set { IsDisabled = value; }
		}
		
		public float GetPowerModifier() { return (IsDisabled) ? 0.0f : 1.0f; }	
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PowerDown")
			{
				IsDisabled = !IsDisabled;
				if (self.Owner == self.World.LocalPlayer)
					Sound.Play(IsDisabled ? "bleep12.aud" : "bleep11.aud");
			}
		}
	}
}
