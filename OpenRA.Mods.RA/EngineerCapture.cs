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

namespace OpenRA.Mods.RA
{
	class EngineerCaptureInfo : TraitInfo<EngineerCapture>
	{
		public readonly int EngineerDamage = 300;
	}

	class EngineerCapture : IIssueOrder, IResolveOrder, IOrderCursor
	{

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (self.Owner.Stances[underCursor.Owner] != Stance.Enemy) return null;
			if (!underCursor.traits.Contains<Building>()) return null;
			var isCapture = underCursor.Health <= self.Info.Traits.Get<EngineerCaptureInfo>().EngineerDamage;

			return new Order(isCapture ? "Capture" : "Infiltrate",
				self, underCursor);
		}

		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Infiltrate") ? "enter" : 
				   (order.OrderString == "Capture") ? "capture" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Infiltrate" || order.OrderString == "Capture")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				self.QueueActivity(new CaptureBuilding(order.TargetActor));
			}
		}
	}
}
