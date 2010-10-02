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
using OpenRA.Mods.RA.Orders;
using System.Collections.Generic;

namespace OpenRA.Mods.RA
{
	class EngineerCaptureInfo : TraitInfo<EngineerCapture> {}
	class EngineerCapture : IIssueOrder2, IResolveOrder, IOrderVoice
	{
		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterOrderTargeter<Building>( "CaptureBuilding", 5, true, false,
					_ => true, target => target.Info.Traits.Get<BuildingInfo>().Capturable );
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order.OrderID == "CaptureBuilding" )
				return new Order( order.OrderID, self, target.Actor );

			return null;
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
