#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Orders
{
	class UnitOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order( World world, int2 xy, MouseInput mi )
		{
			var orders = Game.controller.selection.Actors
				.Select(a => a.Order(xy, mi))
				.Where(o => o != null)
				.ToArray();

			var actorsInvolved = orders.Select(o => o.Subject).Distinct();
			if (actorsInvolved.Any())
				yield return new Order("CreateGroup", actorsInvolved.First().Owner.PlayerActor,
					string.Join(",", actorsInvolved.Select(a => a.ActorID.ToString()).ToArray()));

			foreach (var o in orders)
				yield return o;
		}

		public void Tick( World world ) {}

		public void Render( World world )
		{
			foreach (var a in Game.controller.selection.Actors)
			{
				world.WorldRenderer.DrawSelectionBox(a, Color.White, true);
				if (a.Owner == world.LocalPlayer)
				{
					//if (a.traits.Contains<RenderRangeCircle>())
					//    world.WorldRenderer.DrawRangeCircle(Color.FromArgb(128, Color.Yellow),
					//        a.CenterLocation, (int)a.GetPrimaryWeapon().Range);

					if (a.traits.Contains<DetectCloaked>())
						world.WorldRenderer.DrawRangeCircle(Color.FromArgb(128, Color.LimeGreen),
							a.CenterLocation, a.Info.Traits.Get<DetectCloakedInfo>().Range);
				}
			}
		}

		public string GetCursor( World world, int2 xy, MouseInput mi )
		{
			var c = Order(world, xy, mi)
				.Select(o => o.Subject.traits.WithInterface<IProvideCursor>()
					.Select(pc => pc.CursorForOrderString(o.OrderString, o.Subject, o.TargetLocation)).FirstOrDefault(a => a != null))
				.FirstOrDefault(a => a != null);

			return c ??
				(world.FindUnitsAtMouse(mi.Location)
				.Any(a => a.Info.Traits.Contains<SelectableInfo>())
					? "select" : "default");
		}
	}
}
