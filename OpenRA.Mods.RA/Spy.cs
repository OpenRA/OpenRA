#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using System.Drawing;

namespace OpenRA.Mods.RA
{
	class SpyInfo : TraitInfo<Spy> { }

	class Spy : IIssueOrder, IResolveOrder, IOrderCursor
	{
		public int OrderPriority(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return 5;
		}
		
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (underCursor.Owner == self.Owner) return null;
			if (!underCursor.HasTrait<IAcceptSpy>()) return null;

			return new Order("Infiltrate", self, underCursor);
		}

		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Infiltrate") ? "enter" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Infiltrate")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask( w =>
					{
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTargetSilently(self, Target.FromActor(order.TargetActor), Color.Green);
					});
				self.QueueActivity(new Infiltrate(order.TargetActor));
			}
		}
	}
}
