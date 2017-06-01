#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Lets the actor generate cash in a set periodic time.")]
	public class CashTricklerInfo : ConditionalTraitInfo
	{
		[Desc("Number of ticks to wait between giving money.")]
		public readonly int Interval = 50;

		[Desc("Amount of money to give each time.")]
		public readonly int Amount = 15;

		[Desc("Whether to show the cash tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		[Desc("How long to show the cash tick indicator when enabled.")]
		public readonly int DisplayDuration = 30;

		public override object Create(ActorInitializer init) { return new CashTrickler(this); }
	}

	public class CashTrickler : ConditionalTrait<CashTricklerInfo>, ITick, ISync, INotifyCreated, INotifyOwnerChanged
	{
		readonly CashTricklerInfo info;
		PlayerResources resources;
		[Sync] int ticks;

		public CashTrickler(CashTricklerInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			resources = self.Owner.PlayerActor.Trait<PlayerResources>();

			base.Created(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			resources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--ticks < 0)
			{
				ticks = info.Interval;
				ModifyCash(self, self.Owner, info.Amount);
			}
		}

		void AddCashTick(Actor self, int amount)
		{
			self.World.AddFrameEndTask(w => w.Add(
				new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(amount), info.DisplayDuration)));
		}

		void ModifyCash(Actor self, Player newOwner, int amount)
		{
			if (amount < 0)
			{
				// Check whether the amount of cash to be removed would exceed available player cash, in that case only remove all the player cash
				var drain = Math.Min(resources.Cash + resources.Resources, -amount);
				resources.TakeCash(drain);

				if (info.ShowTicks)
					AddCashTick(self, -drain);
			}
			else
			{
				resources.GiveCash(amount);
				if (info.ShowTicks)
					AddCashTick(self, amount);
			}
		}
	}
}
