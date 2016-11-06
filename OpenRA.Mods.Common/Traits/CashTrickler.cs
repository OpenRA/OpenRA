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

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Lets the actor generate cash in a set periodic time.")]
	class CashTricklerInfo : ConditionalTraitInfo
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

	class CashTrickler : ConditionalTrait<CashTricklerInfo>, ITick, ISync
	{
		readonly CashTricklerInfo info;
		[Sync] int ticks;

		public CashTrickler(CashTricklerInfo info)
			: base(info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--ticks < 0)
			{
				ticks = info.Interval;
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(info.Amount);

				if (info.ShowTicks)
					AddCashTick(self, info.Amount);
			}
		}

		void AddCashTick(Actor self, int amount)
		{
			self.World.AddFrameEndTask(w => w.Add(
				new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(amount), info.DisplayDuration)));
		}
	}
}
