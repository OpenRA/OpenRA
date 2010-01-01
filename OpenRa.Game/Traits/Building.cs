using OpenRa.Game.GameRules;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class Building : INotifyDamage, IOrder
	{
		public readonly BuildingInfo unitInfo;

		public Building(Actor self)
		{
			unitInfo = (BuildingInfo)self.Info;
			self.CenterLocation = Game.CellSize 
				* ((float2)self.Location + .5f * (float2)unitInfo.Dimensions);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				Sound.Play("kaboom22.aud");
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return null; // sell/repair orders are issued through Chrome, not here.
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Sell")
			{
				self.CancelActivity();
				self.QueueActivity(new Sell());
			}
		}
	}
}
