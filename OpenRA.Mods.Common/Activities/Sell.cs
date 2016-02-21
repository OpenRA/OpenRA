#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class Sell : Activity
	{
		readonly Health health;
		readonly SellableInfo sellableInfo;
		readonly PlayerResources playerResources;

		public Sell(Actor self)
		{
			health = self.TraitOrDefault<Health>();
			sellableInfo = self.Info.TraitInfo<SellableInfo>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		public override Activity Tick(Actor self)
		{
			var cost = self.GetSellValue();

			var refund = (cost * sellableInfo.RefundPercent * (health == null ? 1 : health.HP)) / (100 * (health == null ? 1 : health.MaxHP));
			playerResources.GiveCash(refund);

			foreach (var ns in self.TraitsImplementing<INotifySold>())
				ns.Sold(self);

			if (refund > 0 && self.Owner.IsAlliedWith(self.World.RenderPlayer))
				self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(refund), 30)));

			self.Dispose();
			return this;
		}

		// Cannot be cancelled
		public override void Cancel(Actor self) { }
	}
}
