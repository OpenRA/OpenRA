#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CashTricklerInfo : TraitInfo<CashTrickler>
	{
		public readonly int Period = 10;
		public readonly int Amount = 3;
	}

	class CashTrickler : ITick
	{
		[Sync]
		int ticks;

		public void Tick(Actor self)
		{
			if (--ticks < 0)
			{
				var info = self.Info.Traits.Get<CashTricklerInfo>();
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(info.Amount);
				ticks = info.Period;
			}
		}
	}
}
