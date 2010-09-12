#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class EngineerCaptureInfo : TraitInfo<EngineerCapture> {}
	class EngineerCapture : IIssueOrder, IResolveOrder, IOrderCursor, IOrderVoice
	{
		public int OrderPriority(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return 5;
		}
		
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (self.Owner.Stances[underCursor.Owner] == Stance.Ally) return null;
			if (!underCursor.HasTrait<Building>() || !underCursor.Info.Traits.Get<BuildingInfo>().Capturable) return null;

			return new Order("CaptureBuilding", self, underCursor);
		}

		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "CaptureBuilding") ? "capture" : null;
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "CaptureBuilding") ? "Attack" : null;			
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "CaptureBuilding")
			{
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Red);
					});
				
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor.Location, order.TargetActor));
				self.QueueActivity(new CaptureBuilding(order.TargetActor));
			}
		}
	}
}
