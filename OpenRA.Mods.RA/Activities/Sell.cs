#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Sell : Activity
	{
		public override Activity Tick(Actor self)
		{
			var h = self.TraitOrDefault<Health>();
			var si = self.Info.Traits.Get<SellableInfo>();
			var pr = self.Owner.PlayerActor.Trait<PlayerResources>();

			var cost = self.GetSellValue();

			var refund = (cost * si.RefundPercent * (h == null ? 1 : h.HP)) / (100 * (h == null ? 1 : h.MaxHP));
			pr.GiveCash(refund);

			foreach (var ns in self.TraitsImplementing<INotifySold>())
				ns.Sold(self);

			if (refund > 0 && self.Owner.IsAlliedWith(self.World.RenderPlayer))
				self.World.AddFrameEndTask(w => w.Add(new CashTick(self.CenterPosition, self.Owner.Color.RGB, refund)));

			self.Destroy();
			return this;
		}

		// Cannot be cancelled
		public override void Cancel(Actor self) { }
	}
}
