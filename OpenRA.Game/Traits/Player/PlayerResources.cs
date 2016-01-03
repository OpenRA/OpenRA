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

namespace OpenRA.Traits
{
	public class PlayerResourcesInfo : ITraitInfo
	{
		public readonly int[] SelectableCash = { 2500, 5000, 10000, 20000 };
		public readonly int DefaultCash = 5000;

		public object Create(ActorInitializer init) { return new PlayerResources(init.Self, this); }
	}

	public class PlayerResources : ITick, ISync
	{
		readonly Player owner;

		public PlayerResources(Actor self, PlayerResourcesInfo info)
		{
			owner = self.Owner;

			Cash = self.World.LobbyInfo.GlobalSettings.StartingCash;
		}

		[Sync] public int Cash;

		[Sync] public int Resources;
		[Sync] public int ResourceCapacity;

		public int Earned;
		public int Spent;

		public bool CanGiveResources(int amount)
		{
			return Resources + amount <= ResourceCapacity;
		}

		public void GiveResources(int num)
		{
			Resources += num;
			Earned += num;

			if (Resources > ResourceCapacity)
			{
				Earned -= Resources - ResourceCapacity;
				Resources = ResourceCapacity;
			}
		}

		public bool TakeResources(int num)
		{
			if (Resources < num) return false;
			Resources -= num;
			Spent += num;

			return true;
		}

		public void GiveCash(int num)
		{
			if (Cash < int.MaxValue)
			{
				try
				{
					checked
					{
						Cash += num;
					}
				}
				catch (OverflowException)
				{
					Cash = int.MaxValue;
				}
			}

			if (Earned < int.MaxValue)
			{
				try
				{
					checked
					{
						Earned += num;
					}
				}
				catch (OverflowException)
				{
					Earned = int.MaxValue;
				}
			}
		}

		public bool TakeCash(int num)
		{
			if (Cash + Resources < num) return false;

			// Spend ore before cash
			Resources -= num;
			Spent += num;
			if (Resources < 0)
			{
				Cash += Resources;
				Resources = 0;
			}

			return true;
		}

		public void Tick(Actor self)
		{
			ResourceCapacity = self.World.ActorsWithTrait<IStoreResources>()
				.Where(a => a.Actor.Owner == owner)
				.Sum(a => a.Trait.Capacity);

			if (Resources > ResourceCapacity)
				Resources = ResourceCapacity;
		}
	}
}
