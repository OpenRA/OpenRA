#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
		readonly IHealth health;
		readonly SellableInfo sellableInfo;
		readonly PlayerResources playerResources;
		bool showTicks;

		public Sell(Actor self, bool showTicks)
		{
			this.showTicks = showTicks;
			health = self.TraitOrDefault<IHealth>();
			sellableInfo = self.Info.TraitInfo<SellableInfo>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			IsInterruptible = false;
		}

		public override Activity Tick(Actor self)
		{
			var sellValue = self.GetSellValue();

			// Cast to long to avoid overflow when multiplying by the health
			var hp = health != null ? (long)health.HP : 1L;
			var maxHP = health != null ? (long)health.MaxHP : 1L;
			var refund = (int)((sellValue * sellableInfo.RefundPercent * hp) / (100 * maxHP));
			refund = playerResources.ChangeCash(refund);

			foreach (var ns in self.TraitsImplementing<INotifySold>())
				ns.Sold(self);

			if (showTicks && refund > 0 && self.Owner.IsAlliedWith(self.World.RenderPlayer))
				self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(refund), 30)));

			self.Dispose();
			return this;
		}
	}
}
