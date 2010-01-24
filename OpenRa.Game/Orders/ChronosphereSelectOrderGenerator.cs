using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.GameRules;
using OpenRa.Traits;

namespace OpenRa.Orders
{
	class ChronosphereSelectOrderGenerator : IOrderGenerator
	{
		public ChronosphereSelectOrderGenerator() {}
		
		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				Game.controller.CancelInputMode();

			return OrderInner(world, xy, mi);
		}

		IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var loc = mi.Location + Game.viewport.Location;
				var underCursor = world.FindUnits(loc, loc)
					.Where(a => a.Owner == world.LocalPlayer
						&& a.traits.Contains<Chronoshiftable>()
						&& a.traits.Contains<Selectable>()).FirstOrDefault();

				if (underCursor != null)
					yield return new Order("ChronosphereSelect", world.LocalPlayer.PlayerActor, underCursor);
			}

			yield break;
		}

		public void Tick( World world )
		{
			var hasChronosphere = world.Actors
				.Any(a => a.Owner == world.LocalPlayer && a.traits.Contains<Chronosphere>());

			if (!hasChronosphere)
				Game.controller.CancelInputMode();
		}

		public void Render( World world ) { }

		public Cursor GetCursor(World world, int2 xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, xy, mi).Any()
				? Cursor.ChronoshiftSelect : Cursor.MoveBlocked;
		}
	}
}
