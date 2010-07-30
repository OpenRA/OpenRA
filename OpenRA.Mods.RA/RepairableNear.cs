#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using System.Drawing;

namespace OpenRA.Mods.RA
{
	class RepairableNearInfo : TraitInfo<RepairableNear>, ITraitPrerequisite<HealthInfo>
	{
		[ActorReference]
		public readonly string[] Buildings = { "spen", "syrd" };
	}

	class RepairableNear : IIssueOrder, IResolveOrder, IOrderCursor
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			
			if (underCursor.Owner == self.Owner &&
				self.Info.Traits.Get<RepairableNearInfo>().Buildings.Contains( underCursor.Info.Name ) &&
				self.GetDamageState() < DamageState.Undamaged)
				return new Order("Enter", self, underCursor);

			return null;
		}

		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Enter") ? "enter" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Enter")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask( w =>
					{
						var line = self.traits.GetOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTargetSilently(self, Target.FromActor(order.TargetActor), Color.Green);
					});
				self.QueueActivity(new Repair(order.TargetActor));
			}
		}
	}
}
