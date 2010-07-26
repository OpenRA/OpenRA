#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using System.Drawing;

namespace OpenRA.Mods.RA
{
	class C4DemolitionInfo : TraitInfo<C4Demolition>
	{
		public readonly float C4Delay = 0;
	}

	class C4Demolition : IIssueOrder, IResolveOrder, IOrderCursor, IOrderVoice
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (underCursor.Owner == self.Owner && !mi.Modifiers.HasModifier(Modifiers.Ctrl)) return null;
			if (!underCursor.traits.Contains<Building>()) return null;

			return new Order("C4", self, underCursor);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "C4")
			{
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.traits.GetOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, order.TargetActor, Color.Red);
					});
				
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor.Location, order.TargetActor));
				self.QueueActivity(new Demolish(order.TargetActor));
				self.QueueActivity(new Move(self.Location, 0));
			}
		}
		
		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "C4") ? "c4" : null;
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "C4") ? "Attack" : null;
		}
	}
}
