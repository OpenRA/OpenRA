using System.Collections.Generic;
using OpenRa.Game.Graphics;
using OpenRa.Game.Orders;

namespace OpenRa.Game.Traits
{
	class RallyPoint : IRender, IIssueOrder, IResolveOrder, ITick
	{
		[Sync]
		public int2 rallyPoint;
		public Animation anim;

		public RallyPoint(Actor self)
		{
			var bi = self.traits.Get<Building>().unitInfo;
			rallyPoint = self.Location + new int2(bi.RallyPoint[0], bi.RallyPoint[1]);
			anim = new Animation("flagfly");
			anim.PlayRepeating("idle");
		}

		public IEnumerable<Renderable> Render(Actor self)
		{
			var uog = Game.controller.orderGenerator as UnitOrderGenerator;
			if (uog != null && self.Owner == Game.LocalPlayer && uog.selection.Contains(self))
				yield return Util.Centered(self,
					anim.Image, Util.CenterOfCell(rallyPoint));
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left || underCursor != null) return null;
			return new Order("SetRallyPoint", self, null, xy, null);
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if( order.OrderString == "SetRallyPoint" )
				rallyPoint = order.TargetLocation;
		}

		public void Tick(Actor self) { anim.Tick(); }
	}
}
