#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Building can be sold")]
	public class SellableInfo : TraitInfo<Sellable>
	{
		public readonly int RefundPercent = 50;
	}

	public class Sellable : IResolveOrder
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Sell")
				Sell(self);
		}

		public void Sell(Actor self)
		{
			if (!self.Trait<Building>().Lock())
				return;

			foreach (var ns in self.TraitsImplementing<INotifySold>())
				ns.Selling(self);

			self.CancelActivity();

			var rb = self.TraitOrDefault<RenderBuilding>();
			if (rb != null && self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation)
				self.QueueActivity(new MakeAnimation(self, true, () => rb.PlayCustomAnim(self, "make")));
			self.QueueActivity(new Sell());
		}
	}
}
