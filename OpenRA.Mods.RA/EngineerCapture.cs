#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class EngineerCaptureInfo : ITraitInfo
	{
		public string[] CaptureClasses = {"Building"};
		public object Create(ActorInitializer init) { return new EngineerCapture(this); }
	}
	
	class EngineerCapture : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly EngineerCaptureInfo Info;
		public EngineerCapture(EngineerCaptureInfo info)
		{
			Info = info;
		}
		
		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterOrderTargeter<Capturable>( "CaptureBuilding", 5, true, false,
					_ => true, target => target.Info.Traits.Get<CapturableInfo>().CaptureClasses.Intersect(Info.CaptureClasses).Any() );
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "CaptureBuilding" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

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
				self.SetTargetLine(Target.FromOrder(order), Color.Red);
								
				self.CancelActivity();
				self.QueueActivity(new Enter(order.TargetActor));
				self.QueueActivity(new CaptureBuilding(order.TargetActor));
			}
		}
	}
}
