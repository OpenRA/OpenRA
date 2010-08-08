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
using System.Linq;

namespace OpenRA.Mods.RA
{
	class PassengerInfo : TraitInfo<Passenger>
	{
		public readonly string CargoType = null;
		public readonly PipType PipType = PipType.Green;
	}

	class Passenger : IIssueOrder, IResolveOrder, IOrderCursor, IOrderVoice
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{		
			if (mi.Button != MouseButton.Right) 
			    return null;

			if (underCursor == null || underCursor.Owner != self.Owner)
			    return null;

			var cargo = underCursor.traits.GetOrDefault<Cargo>();
			if (cargo == null)
			    return null;
			
			var pi = self.Info.Traits.Get<PassengerInfo>();
			var ci = underCursor.Info.Traits.Get<CargoInfo>();
			if (!ci.Types.Contains(pi.CargoType))
				return null;
			
			return new Order("EnterTransport", self, underCursor);
		}
		
		bool CanEnter(Actor self, Actor a)
		{
			var cargo = a.traits.GetOrDefault<Cargo>();
			return (cargo != null && !cargo.IsFull(a));
		}
		
		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterTransport") return null;
			return CanEnter(self, order.TargetActor) ? "enter" : "enter-blocked";
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterTransport" ||
				!CanEnter(self, order.TargetActor)) return null;
			return "Move";
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "EnterTransport")
			{
				if (!CanEnter(self, order.TargetActor)) return;
				
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.traits.GetOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Green);
					});
				
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor.Location, 1));
				self.QueueActivity(new EnterTransport(self, order.TargetActor));
			}
		}

		public PipType ColorOfCargoPip( Actor self )
		{
			return self.Info.Traits.Get<PassengerInfo>().PipType;
		}
	}
}
