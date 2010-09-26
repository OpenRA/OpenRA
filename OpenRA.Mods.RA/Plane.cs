#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class PlaneInfo : AircraftInfo
	{
		public override object Create( ActorInitializer init ) { return new Plane( init, this ); }
	}

	public class Plane : Aircraft, IIssueOrder, IResolveOrder, IOrderCursor, IOrderVoice, ITick
	{
		public IDisposable reservation;

		public Plane( ActorInitializer init, PlaneInfo info ) : base( init, info ) { }

		bool firstTick = true;
		public void Tick(Actor self)
		{
			if (firstTick)
			{
				firstTick = false;
				if (self.Trait<IMove>().Altitude == 0)
				{	
					/* not spawning in the air, so try to assoc. with our afld. this is a hack. */
					var res = self.World.FindUnits(self.CenterLocation, self.CenterLocation)
						.Select( a => a.TraitOrDefault<Reservable>() ).FirstOrDefault( a => a != null );

					if (res != null)
						reservation = res.Reserve(self);
				}
			}
		}

		public int OrderPriority(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			// Force move takes precidence
			return mi.Modifiers.HasModifier(Modifiers.Alt) ? int.MaxValue : 0;
		}
		
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;
			if (underCursor != null && AircraftCanEnter(self, underCursor)
				&& underCursor.Owner == self.Owner)
				return new Order("Enter", self, underCursor);
			
			return new Order("Move", self, xy);
		}
		
		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move") return "move";
			if (order.OrderString == "Enter")
				return Reservable.IsReserved(order.TargetActor) ? "enter-blocked" : "enter";
			
			return null;
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Move" || order.OrderString == "Enter") ? "Move" : null;
		}

		public void UnReserve()
		{
			if (reservation != null)
			{
				reservation.Dispose();
				reservation = null;
			}
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				UnReserve();

				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						w.Add(new MoveFlash(self.World, order.TargetLocation));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Green);
					});

				self.CancelActivity();
				self.QueueActivity(Fly.ToPx(Util.CenterOfCell(order.TargetLocation)));
			}

			else if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;

				UnReserve();

				var info = self.Info.Traits.Get<PlaneInfo>();

				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Green);
					});

				self.CancelActivity();
				self.QueueActivity(new ReturnToBase(self, order.TargetActor));
				self.QueueActivity(
					info.RearmBuildings.Contains(order.TargetActor.Info.Name)
						? (IActivity)new Rearm() : new Repair(order.TargetActor));
			}
			else
			{
				Game.Debug("Unreserve due to unhandled order: {0}".F(order.OrderString));
				UnReserve();
			}
		}
	}
}
