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
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	public class PlayerResourcesInfo : ITraitInfo
	{
		public readonly int[] SelectableCash = { 2500, 5000, 10000, 20000 };
		public readonly int DefaultCash = 5000;
		public readonly int AdviceInterval = 250;

		public object Create(ActorInitializer init) { return new PlayerResources(init.Self, this); }
	}

	public class PlayerResources : ITick, ISync
	{
		const float DisplayCashFracPerFrame = .07f;
		const int DisplayCashDeltaPerFrame = 37;
		readonly Player owner;
		int adviceInterval;

		public PlayerResources(Actor self, PlayerResourcesInfo info)
		{
			owner = self.Owner;

			Cash = self.World.LobbyInfo.GlobalSettings.StartingCash;
			adviceInterval = info.AdviceInterval;
		}

		[Sync] public int Cash;

		[Sync] public int Resources;
		[Sync] public int ResourceCapacity;

		public int DisplayCash;
		public int DisplayResources;
		public bool AlertSilo;

		public int Earned;
		public int Spent;

		public readonly string[] ResourceTypes = {
			"cash",
			"resources",
		};

		public bool CanGiveResource(string resource, int amount)
		{
			if (!ResourceTypes.Contains(resource))
				throw new InvalidOperationException("Invalid resource type {0}!".F(resource));

			if (resource == "cash")
				return true;
			else
				return Resources + amount <= ResourceCapacity;
		}

		public bool CanGiveResources(IReadOnlyDictionary<string, int> resources)
		{
			return resources.All(p => CanGiveResource(p.Key, p.Value));
		}

		public void GiveResource(string resource, int num)
		{
			if (!ResourceTypes.Contains(resource))
				throw new InvalidOperationException("Invalid resource type {0}!".F(resource));

			if (resource == "cash")
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
			else
			{
				Resources += num;
				Earned += num;

				if (Resources > ResourceCapacity)
				{
					nextSiloAdviceTime = 0;

					Earned -= Resources - ResourceCapacity;
					Resources = ResourceCapacity;
				}
			}
		}

		public void GiveResources(IReadOnlyDictionary<string, int> resources)
		{
			foreach (var p in resources)
				GiveResource(p.Key, p.Value);
		}

		public bool TakeResource(string resource, int num)
		{
			if (!ResourceTypes.Contains(resource))
				throw new InvalidOperationException("Invalid resource type {0}!".F(resource));

			if (resource == "cash")
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
			else
			{
				if (Resources < num)
					return false;
				Resources -= num;
				Spent += num;

				return true;
			}
		}

		public bool TakeResources(IReadOnlyDictionary<string, int> resources)
		{
			Dictionary<string, int> done = new Dictionary<string, int>();
			foreach (var p in resources)
			{
				if (!TakeResource(p.Key, p.Value))
				{
					foreach (var q in done)
						GiveResource(q.Key, q.Value);

					return false;
				}

				done.Add(p.Key, p.Value);
			}

			return true;
		}

		int nextSiloAdviceTime = 0;
		int nextCashTickTime = 0;

		public void Tick(Actor self)
		{
			if (nextCashTickTime > 0)
				nextCashTickTime--;

			ResourceCapacity = self.World.ActorsWithTrait<IStoreResources>()
				.Where(a => a.Actor.Owner == owner)
				.Sum(a => a.Trait.Capacity);

			if (Resources > ResourceCapacity)
				Resources = ResourceCapacity;

			if (--nextSiloAdviceTime <= 0)
			{
				if (Resources > 0.8 * ResourceCapacity)
				{
					Game.Sound.PlayNotification(self.World.Map.Rules, owner, "Speech", "SilosNeeded", owner.Faction.InternalName);
					AlertSilo = true;
				}
				else
					AlertSilo = false;

				nextSiloAdviceTime = adviceInterval;
			}

			var diff = Math.Abs(Cash - DisplayCash);
			var move = Math.Min(Math.Max((int)(diff * DisplayCashFracPerFrame), DisplayCashDeltaPerFrame), diff);

			if (DisplayCash < Cash)
			{
				DisplayCash += move;
				PlayCashTickUp(self);
			}
			else if (DisplayCash > Cash)
			{
				DisplayCash -= move;
				PlayCashTickDown(self);
			}

			diff = Math.Abs(Resources - DisplayResources);
			move = Math.Min(Math.Max((int)(diff * DisplayCashFracPerFrame),
					DisplayCashDeltaPerFrame), diff);

			if (DisplayResources < Resources)
			{
				DisplayResources += move;
				PlayCashTickUp(self);
			}
			else if (DisplayResources > Resources)
			{
				DisplayResources -= move;
				PlayCashTickDown(self);
			}
		}

		public void PlayCashTickUp(Actor self)
		{
			if (Game.Settings.Sound.CashTicks)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", "CashTickUp", self.Owner.Faction.InternalName);
		}

		public void PlayCashTickDown(Actor self)
		{
			if (Game.Settings.Sound.CashTicks && nextCashTickTime == 0)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", "CashTickDown", self.Owner.Faction.InternalName);
				nextCashTickTime = 2;
			}
		}
	}
}
