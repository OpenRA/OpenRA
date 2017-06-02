#region Copyright & License Information
/*
 * Modded by Boolbada of Over Powered Mod,
 * Mostly the same as CashTrickler trait.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("Lets the actor generate cash in a set periodic time.")]
	class CargoCashTricklerInfo : ITraitInfo, Requires<CargoInfo>
	{
		[Desc("Number of ticks to wait between giving money.")]
		public readonly int Period = 50;
		[Desc("Amount of money to give each time, per passenger.")]
		public readonly int AmountPerLoad = 15;
		[Desc("Whether to show the cash tick indicators (+$15 rising from actor).")]
		public readonly bool ShowTicks = true;

		public object Create(ActorInitializer init) { return new CargoCashTrickler(init, this); }
	}

	class CargoCashTrickler : ITick, ISync, INotifyPassengerEntered, INotifyPassengerExited
	{
		readonly CargoCashTricklerInfo info;
		readonly Cargo cargo;
		readonly Actor self;
		[Sync] int amount;
		[Sync] int ticks;

		public CargoCashTrickler(ActorInitializer init, CargoCashTricklerInfo info)
		{
			this.info = info;
			self = init.Self;
			cargo = self.Trait<Cargo>();
			amount = 0;
		}

		public void Tick(Actor self)
		{
			if (--ticks < 0)
			{
				ticks = info.Period;
				if (amount != 0)
				{
					// Well, I guess amount can be negative number, if it costs upkeep.
					self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(amount);
					MaybeAddCashTick(self, info.AmountPerLoad, cargo.PassengerCount);
				}
			}
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			UpdateAmount();
		}

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			UpdateAmount();
		}

		void UpdateAmount()
		{
			amount = info.AmountPerLoad * cargo.PassengerCount;
		}

		void MaybeAddCashTick(Actor self, int amount, int cnt)
		{
			if (!info.ShowTicks)
				return;

			for (int i = 0; i < cnt; i++)
			{
				var offset = new WVec(i << 8, -i << 8, 0);
				var pos = self.CenterPosition + offset;
				self.World.AddFrameEndTask(
					w => w.Add(
						new FloatingText(pos, self.Owner.Color.RGB, FloatingText.FormatCashTick(amount), 30)));
			}
		}
	}
}
