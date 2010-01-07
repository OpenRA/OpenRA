using OpenRa.Game.Traits;
using OpenRa.Game.Orders;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRa.Game.Effects;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class IronCurtainable: IOrder, IDamageModifier, ITick, IRenderModifier
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
			return null; // Chronoshift order is issued through Chrome.
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "IronCurtain")
			{
				Game.controller.CancelInputMode();
				RemainingTicks = (int)(Rules.General.IronCurtain * 60 * 25);
				Sound.Play("ironcur9.aud");
				// Play active anim
				var ironCurtain = Game.world.Actors.Where(a => a.Owner == order.Subject.Owner && a.traits.Contains<IronCurtain>()).FirstOrDefault();
				if (ironCurtain != null)
					ironCurtain.traits.Get<RenderBuilding>().PlayCustomAnim(ironCurtain, "active");
			}
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> rs)
		{		
			if (RemainingTicks <= 0)
				return rs;
			
			List<Renderable> nrs = new List<Renderable>(rs);
			foreach(var r in rs)
			{
				nrs.Add(r.WithPalette(PaletteType.Invuln));
			}			
			return nrs;
		}
	}
}
