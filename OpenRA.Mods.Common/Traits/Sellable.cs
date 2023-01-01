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

using System;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can be sold")]
	public class SellableInfo : ConditionalTraitInfo
	{
		[Desc("Percentage of units value to give back after selling.")]
		public readonly int RefundPercent = 50;

		[Desc("List of audio clips to play when the actor is being sold.")]
		public readonly string[] SellSounds = Array.Empty<string>();

		[NotificationReference("Speech")]
		[Desc("Speech notification to play.")]
		public readonly string Notification = null;

		[Desc("Text notification to display.")]
		public string TextNotification = null;

		[Desc("Whether to show the cash tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		[Desc("Whether to show the refund text on the tooltip, when actor is hovered over with sell order.")]
		public readonly bool ShowTooltipText = true;

		[Desc("Skip playing (reversed) make animation.")]
		public readonly bool SkipMakeAnimation = false;

		[CursorReference]
		[Desc("Cursor to display when the sell order generator hovers over this actor.")]
		public readonly string Cursor = "sell";

		public override object Create(ActorInitializer init) { return new Sellable(init.Self, this); }
	}

	public class Sellable : ConditionalTrait<SellableInfo>, IResolveOrder, IProvideTooltipInfo
	{
		readonly Actor self;
		readonly Lazy<IHealth> health;
		readonly SellableInfo info;

		public Sellable(Actor self, SellableInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
			health = Exts.Lazy(() => self.TraitOrDefault<IHealth>());
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

			self.CancelActivity();

			foreach (var s in info.SellSounds)
				Game.Sound.PlayToPlayer(SoundType.UI, self.Owner, s, self.CenterPosition);

			foreach (var ns in self.TraitsImplementing<INotifySold>())
				ns.Selling(self);

			if (!info.SkipMakeAnimation)
			{
				var makeAnimation = self.TraitOrDefault<WithMakeAnimation>();
				if (makeAnimation != null)
				{
					makeAnimation.Reverse(self, new Sell(self, info.ShowTicks), false);
					return;
				}
			}

			self.QueueActivity(false, new Sell(self, info.ShowTicks));
		}

		public bool IsTooltipVisible(Player forPlayer)
		{
			if (info.ShowTooltipText && !IsTraitDisabled && self.World.OrderGenerator is SellOrderGenerator)
				return forPlayer == self.Owner;
			return false;
		}

		public string TooltipText
		{
			get
			{
				var sellValue = self.GetSellValue();

				// Cast to long to avoid overflow when multiplying by the health
				var hp = health != null ? (long)health.Value.HP : 1L;
				var maxHP = health != null ? (long)health.Value.MaxHP : 1L;
				var refund = (int)((sellValue * info.RefundPercent * hp) / (100 * maxHP));

				return "Refund: $" + refund;
			}
		}
	}
}
