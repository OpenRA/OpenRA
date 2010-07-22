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

	class EngineerCapture : IIssueOrder, IResolveOrder, IProvideCursor
	{

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (!underCursor.traits.Contains<Building>()) return null;
			
			// todo: other bits
			if (underCursor.Owner == null) return null;	// don't allow capturing of bridges, etc.

			var isCapture = underCursor.Health <= self.Info.Traits.Get<EngineerCaptureInfo>().EngineerDamage &&
				self.Owner.Stances[underCursor.Owner] != Stance.Ally;

			var isHeal = self.Owner.Stances[underCursor.Owner] == Stance.Ally;
			return new Order(isCapture ? "Capture" :
			                 isHeal ? "Repair" : "Infiltrate",
				self, underCursor);
		}

		public string CursorForOrderString(string s, Actor a, int2 location)
		{
			return (s == "Infiltrate") ? "enter" : 
				   (s == "Repair") ? "goldwrench" :
				   (s == "Capture") ? "capture" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Infiltrate" || order.OrderString == "Capture" || order.OrderString == "Repair")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				self.QueueActivity(new CaptureBuilding(order.TargetActor));
			}
		}
	}
}
