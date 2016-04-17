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

using System;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can be sold")]
	public class SellableInfo : UpgradableTraitInfo
	{
		public readonly int RefundPercent = 50;
		public readonly string[] SellSounds = { };

		public override object Create(ActorInitializer init) { return new Sellable(init.Self, this); }
	}

	public class Sellable : UpgradableTrait<SellableInfo>, IResolveOrder, IProvideTooltipInfo
	{
		readonly Actor self;
		readonly Lazy<Health> health;
		readonly SellableInfo info;

		public Sellable(Actor self, SellableInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
			health = Exts.Lazy(() => self.TraitOrDefault<Health>());
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Sell")
				Sell(self);
		}

		public void Sell(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var building = self.TraitOrDefault<Building>();
			if (building != null && !building.Lock())
				return;

			self.CancelActivity();

			foreach (var s in info.SellSounds)
				Game.Sound.PlayToPlayer(self.Owner, s, self.CenterPosition);

			foreach (var ns in self.TraitsImplementing<INotifySold>())
				ns.Selling(self);

			var makeAnimation = self.TraitOrDefault<WithMakeAnimation>();
			if (makeAnimation != null)
				makeAnimation.Reverse(self, new Sell(self), false);
			else
				self.QueueActivity(false, new Sell(self));
		}

		public bool IsTooltipVisible(Player forPlayer)
		{
			if (self.World.OrderGenerator is SellOrderGenerator)
				return forPlayer == self.Owner;
			return false;
		}

		public string TooltipText
		{
			get
			{
				var sellValue = self.GetSellValue() * info.RefundPercent / 100;
				if (health.Value != null)
				{
					sellValue *= health.Value.HP;
					sellValue /= health.Value.MaxHP;
				}

				return "Refund: $" + sellValue;
			}
		}
	}
}
