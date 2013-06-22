#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class PlayerStatisticsInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new PlayerStatistics(init.self); }
	}

	public class PlayerStatistics : ITick, IResolveOrder
	{
		World world;
		Player player;

		public double MapControl;
		public int OrderCount;

		public int EarnedThisMinute 
		{
			get
			{
				return player.PlayerActor.Trait<PlayerResources>().Earned - earnedAtBeginningOfMinute;
			}
		}
		public Queue<int> EarnedSamples = new Queue<int>(100);
		int earnedAtBeginningOfMinute;

		public int KillsCost;
		public int DeathsCost;

		public int UnitsKilled;
		public int UnitsDead;

		public int BuildingsKilled;
		public int BuildingsDead;

		public PlayerStatistics(Actor self)
		{
			world = self.World;
			player = self.Owner;
		}

		void UpdateMapControl()
		{
			var total = (double)world.Map.Bounds.Width * world.Map.Bounds.Height;
			MapControl = world.Actors
				.Where(a => !a.IsDead() && a.IsInWorld && a.Owner == player && a.HasTrait<RevealsShroud>())
				.SelectMany(a => world.FindTilesInCircle(a.Location, a.Trait<RevealsShroud>().Range.Clamp(0, 50)))
				.Distinct()
				.Count() / total;
		}

		void UpdateEarnedThisMinute()
		{
			EarnedSamples.Enqueue(EarnedThisMinute);
			earnedAtBeginningOfMinute = player.PlayerActor.Trait<PlayerResources>().Earned;
			if (EarnedSamples.Count > 100)
				EarnedSamples.Dequeue();
		}

		public void Tick(Actor self)
		{
			if (self.World.FrameNumber % 1500 == 1)
				UpdateEarnedThisMinute();
			if (self.World.FrameNumber % 250 == 0)
				UpdateMapControl();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "Chat":
				case "TeamChat":
				case "HandshakeResponse":
				case "PauseGame":
				case "StartGame":
				case "Disconnected":
				case "ServerError":
				case "SyncInfo":
					return;
			}
			if (order.OrderString.StartsWith("Dev"))
				return;
			OrderCount++;
		}
	}

	public class UpdatesPlayerStatisticsInfo : TraitInfo<UpdatesPlayerStatistics> { }

	public class UpdatesPlayerStatistics : INotifyKilled
	{
		public void Killed(Actor self, AttackInfo e)
		{
			var attackerStats = e.Attacker.Owner.PlayerActor.Trait<PlayerStatistics>();
			var defenderStats = self.Owner.PlayerActor.Trait<PlayerStatistics>();
			if (self.HasTrait<Building>())
			{
				attackerStats.BuildingsKilled++;
				defenderStats.BuildingsDead++;
			}
			else if (self.HasTrait<IMove>())
			{
				attackerStats.UnitsKilled++;
				defenderStats.UnitsDead++;
			}
			if (self.HasTrait<Valued>())
			{
				var cost = self.Info.Traits.Get<ValuedInfo>().Cost;
				attackerStats.KillsCost += cost;
				defenderStats.DeathsCost += cost;
			}
		}
	}
}
