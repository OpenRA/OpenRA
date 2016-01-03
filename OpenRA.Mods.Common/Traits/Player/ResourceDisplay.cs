#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ResourceDisplayInfo : ITraitInfo, Requires<PlayerResourcesInfo>
	{
		public object Create(ActorInitializer init) { return new ResourceDisplay(init.Self, this); }
	}

	public class ResourceDisplay : ITick, IResourceDisplay
	{
		const float DisplayFracPerFrame = .07f;
		const int DisplayDeltaPerFrame = 37;

		readonly PlayerResources resources;
		int ActualAmount { get { return resources.Cash + resources.Resources; } }
		int nextCashTickTime = 0;

		public int Amount { get; private set; }
		public int CappedAmount { get { return resources.Resources; } }
		public int Capacity { get { return resources.ResourceCapacity; } }
		public int Earned { get { return resources.Earned; } }
		public int Spent { get { return resources.Spent; } }

		public ResourceDisplay(Actor self, ResourceDisplayInfo info)
		{
			resources = self.Trait<PlayerResources>();
			Amount = ActualAmount;
		}

		public void Tick(Actor self)
		{
			if (nextCashTickTime > 0)
				nextCashTickTime--;

			var diff = Math.Abs(ActualAmount - Amount);
			var move = Math.Min(Math.Max((int)(diff * DisplayFracPerFrame), DisplayDeltaPerFrame), diff);

			if (Amount < ActualAmount)
			{
				Amount += move;
				PlayTickUp(self);
			}
			else if (Amount > ActualAmount)
			{
				Amount -= move;
				PlayTickDown(self);
			}
		}

		public void PlayTickUp(Actor self)
		{
			if (Game.Settings.Sound.CashTicks)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", "CashTickUp", self.Owner.Faction.InternalName);
		}

		public void PlayTickDown(Actor self)
		{
			if (Game.Settings.Sound.CashTicks && nextCashTickTime == 0)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", "CashTickDown", self.Owner.Faction.InternalName);
				nextCashTickTime = 2;
			}
		}
	}
}
