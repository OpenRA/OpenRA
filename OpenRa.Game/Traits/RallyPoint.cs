using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RallyPoint : IRender, IOrder, ITick
	{
		public int2 rallyPoint;
		public Animation anim;

		public RallyPoint(Actor self)
		{
			var bi = self.traits.Get<Building>().unitInfo;
			rallyPoint = self.Location + new int2(bi.RallyPoint[0], bi.RallyPoint[1]);
			anim = new Animation("flagfly");
			anim.PlayRepeating("idle");
		}

		public IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var uog = Game.controller.orderGenerator as UnitOrderGenerator;
			if (uog != null && self.Owner == Game.LocalPlayer && uog.selection.Contains(self))
				yield return Util.Centered( self,
					anim.Image, Game.CellSize * (new float2(.5f, .5f) + rallyPoint.ToFloat2()));
		}

		public Order IssueOrder(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if (lmb || underCursor != null) return null;
			return Order.SetRallyPoint(self, xy);
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if( order.OrderString == "SetRallyPoint" )
				rallyPoint = order.TargetLocation;
		}

		public void Tick(Actor self) { anim.Tick(); }
	}
}
