#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	class SellableInfo : TraitInfo<Sellable> 
	{
		public readonly int RefundPercent = 50;
	}
	
	class Sellable : IIssueOrder, IResolveOrder
	{
		public IEnumerable<IOrderTargeter> Orders 
		{
			get { yield return new PaletteOnlyOrderTargeter("Sell"); }
		}
		
		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			/* todo: make this work */
			throw new NotImplementedException();
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Sell")
			{
				self.CancelActivity();
				self.QueueActivity(new Sell());
			}
		}
	}
}
