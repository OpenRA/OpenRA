#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	sealed class Sell : Activity
	{
		readonly IHealth health;
		readonly SellableInfo sellableInfo;
		readonly PlayerResources playerResources;
		readonly bool showTicks;

		public Sell(Actor self, bool showTicks)
		{
			this.showTicks = showTicks;
			health = self.TraitOrDefault<IHealth>();
			sellableInfo = self.Info.TraitInfo<SellableInfo>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			IsInterruptible = false;
		}

		public override bool Tick(Actor self)
		{
			var sellValue = self.GetSellValue();

			// Cast to long to avoid overflow when multiplying by the health
			var hp = health != null ? health.HP : 1L;
			var maxHP = health != null ? health.MaxHP : 1L;
			var refund = (int)(sellValue * sellableInfo.RefundPercent * hp / (100 * maxHP));
			refund = playerResources.ChangeCash(refund);

			foreach (var ns in self.TraitsImplementing<INotifySold>())
				ns.Sold(self);

			if (showTicks && refund > 0 && self.Owner.IsAlliedWith(self.World.RenderPlayer))
				self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.OwnerColor(), FloatingText.FormatCashTick(refund), 30)));

			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", sellableInfo.Notification, self.Owner.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(self.Owner, sellableInfo.TextNotification);

			self.Dispose();
			return false;
		}
	}
}
