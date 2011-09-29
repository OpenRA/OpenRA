#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Air
{
	public class PlaneInfo : AircraftInfo
	{
		public override object Create( ActorInitializer init ) { return new Plane( init, this ); }
	}

	public class Plane : Aircraft, IResolveOrder, ITick, ISync
	{
		[Sync] public int2 RTBPathHash;

		public Plane( ActorInitializer init, PlaneInfo info )
			: base( init, info ) { }

		bool firstTick = true;
		public void Tick(Actor self)
		{
			if (firstTick)
			{
				firstTick = false;
				ReserveSpawnBuilding(self);
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				UnReserve();

				var target = self.World.ClampToWorld(order.TargetLocation);
				self.SetTargetLine(Target.FromCell(target), Color.Green);
				self.CancelActivity();
				self.QueueActivity(Fly.ToCell(target));
				self.QueueActivity(new FlyCircle());
			}

			else if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;

				UnReserve();

				self.SetTargetLine(Target.FromOrder(order), Color.Green);

				self.CancelActivity();
				self.QueueActivity(new ReturnToBase(self, order.TargetActor));

				QueueResupplyActivities(order.TargetActor);
			}
			else if (order.OrderString == "Stop")
			{
				UnReserve();
				self.CancelActivity();
			}
			else if (order.OrderString == "ReturnToBase")
			{
				UnReserve();
				self.CancelActivity();
				self.QueueActivity(new ReturnToBase(self,null));
			}
			else
			{
				// Game.Debug("Unreserve due to unhandled order: {0}".F(order.OrderString));
				UnReserve();
			}
		}
	}
}
