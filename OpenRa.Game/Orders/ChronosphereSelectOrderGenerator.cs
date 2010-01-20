using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.GameRules;
using OpenRa.Traits;
using OpenRa.SupportPowers;

namespace OpenRa.Orders
{
	class ChronosphereSelectOrderGenerator : IOrderGenerator
	{
		SupportPower power;
		public ChronosphereSelectOrderGenerator(SupportPower power)
		{
			this.power = power;
		}
		
		public IEnumerable<Order> Order(int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				Game.controller.CancelInputMode();

			return OrderInner(xy, mi);
		}

		IEnumerable<Order> OrderInner(int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var loc = mi.Location + Game.viewport.Location;
				var underCursor = Game.world.FindUnits(loc, loc)
					.Where(a => a.Owner == Game.world.LocalPlayer
						&& a.traits.Contains<Chronoshiftable>()
						&& a.traits.Contains<Selectable>()).FirstOrDefault();
				
				if (underCursor != null)
					yield return new Order("ChronosphereSelect", underCursor, null, int2.Zero, power.Name);
			}
		}

		public void Tick()
		{
			var hasChronosphere = Game.world.Actors
				.Any(a => a.Owner == Game.world.LocalPlayer && a.traits.Contains<Chronosphere>());

			if (!hasChronosphere)
				Game.controller.CancelInputMode();
		}

		public void Render() { }

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(xy, mi).Any()
				? Cursor.ChronoshiftSelect : Cursor.MoveBlocked;
		}
	}
}
