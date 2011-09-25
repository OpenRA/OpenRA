#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Mods.RA.Effects;

namespace OpenRA.Mods.RA
{
	class CashTricklerInfo : ITraitInfo
	{
		public readonly int Period = 50;
		public readonly int Amount = 15;
		public readonly bool ShowTicks = true;
		public readonly int TickLifetime = 30;
		public readonly int TickVelocity = 1;

		public object Create (ActorInitializer init) { return new CashTrickler(this); }
	}

	class CashTrickler : ITick, ISync
	{
		[Sync]
		int ticks;
		CashTricklerInfo Info;
		public CashTrickler(CashTricklerInfo info)
		{
			Info = info;
		}

		public void Tick(Actor self)
		{
			if (--ticks < 0)
			{
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(Info.Amount);
				ticks = Info.Period;
				if (Info.ShowTicks)
					self.World.AddFrameEndTask(w => w.Add(new CashTick(Info.Amount, Info.TickLifetime, Info.TickVelocity, self.CenterLocation, self.Owner.ColorRamp.GetColor(0))));
			}
		}
	}
}
