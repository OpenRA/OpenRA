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
using OpenRA.Mods.RA.Render;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	class SellableInfo : TraitInfo<Sellable> 
	{
		public readonly int RefundPercent = 50;
	}
	
	class Sellable : IResolveOrder
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Sell")
			{
				self.CancelActivity();
				if (self.HasTrait<RenderBuilding>() && self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation)
					self.QueueActivity(new MakeAnimation(self, true));
				
				foreach( var ns in self.TraitsImplementing<INotifySold>() )
					ns.Selling( self );
				
				self.QueueActivity(new Sell());
			}
		}
	}
}
