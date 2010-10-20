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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Mods.RA.Orders;

namespace OpenRA.Mods.RA
{
	public class PlaneInfo : AircraftInfo
	{
		public override object Create( ActorInitializer init ) { return new Plane( init, this ); }
	}

	public class Plane : Aircraft, IIssueOrder, IResolveOrder, IOrderVoice, ITick
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

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterOrderTargeter<Building>( "Enter", 5, false, true,
					target => AircraftCanEnter( target ), target => !Reservable.IsReserved( target ) );

				yield return new AircraftMoveOrderTargeter();
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order.OrderID == "Enter" )
				return new Order( order.OrderID, self, target.Actor );

			if( order.OrderID == "Move" )
				return new Order( order.OrderID, self, Util.CellContaining( target.CenterLocation ) );

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
						if (self.Destroyed) return;
						w.Add(new MoveFlash(self.World, order.TargetLocation));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Green);
					});

				self.CancelActivity();
				self.QueueActivity(Fly.ToCell(order.TargetLocation));
			}

			else if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;

				UnReserve();

				var info = self.Info.Traits.Get<PlaneInfo>();

				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (self.Destroyed) return;
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
				// Game.Debug("Unreserve due to unhandled order: {0}".F(order.OrderString));
				UnReserve();
			}
		}
	}

	class AircraftMoveOrderTargeter : IOrderTargeter
	{
		public string OrderID { get { return "Move"; } }
		public int OrderPriority { get { return 4; } }

		public bool CanTargetUnit( Actor self, Actor target, bool forceAttack, bool forceMove, ref string cursor )
		{
			return false;
		}

		public bool CanTargetLocation( Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceMove, ref string cursor )
		{
			cursor = "move";
			return true;
		}
	}
}
