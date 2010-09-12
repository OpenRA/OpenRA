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
			var orders = world.Selection.Actors
				.Select(a => a.Order(xy, mi, UnderCursor(world, mi)))
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

		public void RenderBeforeWorld(World world)
		{
			foreach (var a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPreRenderSelection>())
						t.RenderBeforeWorld(a);

			Game.Renderer.Flush();
		}

		public void RenderAfterWorld( World world )
		{
			foreach (var a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPostRenderSelection>())
						t.RenderAfterWorld(a);

			Game.Renderer.Flush();
		}

		public Actor UnderCursor(World world, MouseInput mi)
		{
			return world.FindUnitsAtMouse(mi.Location)
				.Where(a => a.Info.Traits.Contains<TargetableInfo>())
				.OrderByDescending(a => a.Info.Traits.Contains<SelectableInfo>() ? a.Info.Traits.Get<SelectableInfo>().Priority : int.MinValue)
				.FirstOrDefault();
		}
		
		public string GetCursor( World world, int2 xy, MouseInput mi )
		{		
			if (mi.Modifiers.HasModifier(Modifiers.Shift) || !world.Selection.Actors.Any())
				if (UnderCursor(world, mi) != null)
					return "select";
						
			var c = Order(world, xy, mi)
				.Select(o => o.Subject.TraitsImplementing<IOrderCursor>()
					.Select(pc => pc.CursorForOrder(o.Subject, o)).FirstOrDefault(a => a != null))
				.FirstOrDefault(a => a != null);

			return c ?? "default";
		}
	}
}
