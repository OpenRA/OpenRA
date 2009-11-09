using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using OpenRa.Game.Graphics;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class RallyPoint : IRender, IOrder, ITick
	{
		public int2 rallyPoint;
		public Animation anim;

		public RallyPoint(Actor self)
		{
			var bi = (UnitInfo.BuildingInfo)self.unitInfo;
			rallyPoint = self.Location + new int2(bi.RallyPoint[0], bi.RallyPoint[1]);
			anim = new Animation("flagfly");
			anim.PlayRepeating("idle");
		}

		public IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			var uog = Game.controller.orderGenerator as UnitOrderGenerator;
			if (uog != null && self.Owner == Game.LocalPlayer && uog.selection.Contains(self))
				yield return Util.Centered(
					anim.Image, Game.CellSize * (new float2(.5f, .5f) + rallyPoint.ToFloat2()));
		}

		public Order Order(Actor self, int2 xy, bool lmb, Actor underCursor)
		{
			if (lmb || underCursor != null) return null;
			return OpenRa.Game.Order.SetRallyPoint(self, xy);
		}

		public void Tick(Actor self) { anim.Tick(); }
	}
}
