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

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Lets the actor generate cash in a set periodic time.")]
	class CashTricklerInfo : ITraitInfo
	{
		[Desc("Number of ticks to wait between giving money.")]
		public readonly int Period = 50;
		[Desc("Amount of money to give each time.")]
		public readonly int Amount = 15;
		[Desc("Whether to show the cash tick indicators (+$15 rising from actor).")]
		public readonly bool ShowTicks = true;
		[Desc("Amount of money awarded for capturing the actor.")]
		public readonly int CaptureAmount = 0;

		public object Create(ActorInitializer init) { return new CashTrickler(this); }
	}

	class CashTrickler : ITick, ISync, INotifyCapture
	{
		readonly CashTricklerInfo info;
		[Sync] int ticks;
		public CashTrickler(CashTricklerInfo info)
		{
			this.info = info;
		}

		public void Tick(Actor self)
		{
			if (--ticks < 0)
			{
				ticks = info.Period;
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(info.Amount);
				MaybeAddCashTick(self, info.Amount);
			}
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (info.CaptureAmount > 0)
			{
				newOwner.PlayerActor.Trait<PlayerResources>().GiveCash(info.CaptureAmount);
				MaybeAddCashTick(self, info.CaptureAmount);
			}
		}

		void MaybeAddCashTick(Actor self, int amount)
		{
			if (info.ShowTicks)
				self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(amount), 30)));
		}
	}
}
