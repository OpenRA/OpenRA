using System.Linq;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class IronCurtainableInfo : ITraitInfo
	{
		public object Create(Actor self) { return new IronCurtain(self); }
	}

	class IronCurtainable : IResolveOrder, IDamageModifier, ITick
	{
		[Sync]
		int RemainingTicks = 0;

		public IronCurtainable(Actor self) { }

		public void Tick(Actor self)
		{
			if (RemainingTicks > 0)
				RemainingTicks--;
		}
		public float GetDamageModifier()
		{
			return (RemainingTicks > 0) ? 0.0f : 1.0f;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "IronCurtain")
			{
				var power = self.Owner.SupportPowers[order.TargetString].Impl;
				power.OnFireNotification(self, self.Location);
				Game.world.AddFrameEndTask(w => w.Add(new InvulnEffect(self)));
				RemainingTicks = (int)(Rules.General.IronCurtain * 60 * 25);
			}
		}
	}
}
