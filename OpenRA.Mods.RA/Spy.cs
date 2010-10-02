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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using System.Collections.Generic;

namespace OpenRA.Mods.RA
{
	class SpyInfo : TraitInfo<Spy> { }

	class Spy : IIssueOrder, IResolveOrder
	{
		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new UnitTraitOrderTargeter<IAcceptSpy>( "SpyInfiltrate", 5, "enter", true, false ); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order.OrderID == "SpyInfiltrate" )
				return new Order( order.OrderID, self, target.Actor );

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SpyInfiltrate")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask( w =>
					{
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTargetSilently(self, Target.FromActor(order.TargetActor), Color.Green);
					});
				self.QueueActivity(new Infiltrate(order.TargetActor));
			}
		}
	}
}
