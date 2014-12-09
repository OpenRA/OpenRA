#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{

	[Desc("Actor can be sold")]
	public class SellableInfo : UpgradableTraitInfo, ITraitInfo
	{
		public readonly int RefundPercent = 50;
		public readonly string[] SellSounds = { };

		public object Create(ActorInitializer init) { return new Sellable(this); }
	}

	public class Sellable : UpgradableTrait<SellableInfo>, IResolveOrder
	{
		public Sellable(SellableInfo info)
			: base(info) { }

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

			foreach (var s in Info.SellSounds)
				Sound.PlayToPlayer(self.Owner, s, self.CenterPosition);

			foreach (var ns in self.TraitsImplementing<INotifySold>())
				ns.Selling(self);

			var makeAnimation = self.TraitOrDefault<WithMakeAnimation>();
			if (makeAnimation != null)
				makeAnimation.Reverse(self, new Sell());
			else
				self.QueueActivity(new Sell());
		}
	}
}
