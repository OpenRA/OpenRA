using System.Linq;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class IronCurtainable : IOrder, IDamageModifier, ITick
	{
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

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return null; // IronCurtain order is issued through Chrome.
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "IronCurtain")
			{
				order.Power.OnFireNotification(self, self.Location);
				Game.world.AddFrameEndTask(w => w.Add(new InvulnEffect(self)));
				RemainingTicks = (int)(Rules.General.IronCurtain * 60 * 25);
			}
		}
	}
}
