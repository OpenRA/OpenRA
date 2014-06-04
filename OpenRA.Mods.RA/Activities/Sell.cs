#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA.Activities
{
	class Sell : Activity
	{
		public bool PlayAnim;

		public Sell() { PlayAnim = true; }
		public Sell(bool playAnim) { PlayAnim = playAnim; }

		public override Activity Tick(Actor self)
		{
			if (PlayAnim)
			{
				var rb = self.TraitOrDefault<RenderBuilding>();
				if (rb != null && self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation)
					self.QueueActivity(new MakeAnimation(self, true, () => rb.PlayCustomAnim(self, "make")));
			}

			var h = self.TraitOrDefault<Health>();
			var si = self.Info.Traits.Get<SellableInfo>();
			var pr = self.Owner.PlayerActor.Trait<PlayerResources>();
			var cost = self.GetSellValue();

			var refund = (cost * si.RefundPercent * (h == null ? 1 : h.HP)) / (100 * (h == null ? 1 : h.MaxHP));

			var mods = self.TraitsImplementing<Modular>().Where(p => p.IsUpgraded);
			foreach (var mod in mods)
			{
				var umi = mod.UpgradeActor.Info.Traits.Get<UpgradeModuleInfo>();
				if (umi.DestroyOrSell == DestroyOrSell.Sell)
				{
					var umh = mod.UpgradeActor.TraitOrDefault<Health>();
					var umsi = mod.UpgradeActor.Info.Traits.GetOrDefault<SellableInfo>();
					var umc = mod.UpgradeActor.GetSellValue();

					var cashBack = umsi == null ? 50 : umsi.RefundPercent;
					var hasHealth = umh != null;

					refund += (umc * cashBack * (hasHealth ? 1 : umh.MaxHP)) / (100 * (hasHealth ? 1 : h.MaxHP));

					foreach (var ns in mod.UpgradeActor.TraitsImplementing<INotifySold>())
						ns.Sold(mod.UpgradeActor);
				}

				mod.UpgradeActor.Destroy();
			}

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
