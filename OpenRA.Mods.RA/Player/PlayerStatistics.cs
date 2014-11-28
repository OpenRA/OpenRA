#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Attach this to the player actor to collect observer stats.")]
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
				.Where(a => !a.IsDead && a.IsInWorld && a.Owner == player && a.HasTrait<RevealsShroud>())
				.SelectMany(a => world.Map.FindTilesInCircle(
					a.Location,
					a.Trait<RevealsShroud>().Range.Clamp(WRange.Zero, WRange.FromCells(Map.MaxTilesInCircleRange)).Range / 1024))
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
			if (self.World.WorldTick % 1500 == 1)
				UpdateEarnedThisMinute();
			if (self.World.WorldTick % 250 == 0)
				UpdateMapControl();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			switch (order.ID)
			{
				case OrderCode.Chat:
				case OrderCode.TeamChat:
				case OrderCode.HandshakeResponse:
				case OrderCode.PauseGame:
				case OrderCode.StartGame:
				case OrderCode.Disconnected:
				case OrderCode.ServerError:
				case OrderCode.AuthenticationError:
				case OrderCode.SyncLobbyInfo:
				case OrderCode.SyncClientInfo:
				case OrderCode.SyncLobbySlots:
				case OrderCode.SyncLobbyGlobalSettings:
				case OrderCode.SyncClientPing:
				case OrderCode.Ping:
				case OrderCode.Pong:
					return;
			}
			if (order.ID.ToString().StartsWith("Dev"))
				return;
			OrderCount++;
		}
	}

	[Desc("Attach this to a unit to update observer stats.")]
	public class UpdatesPlayerStatisticsInfo : TraitInfo<UpdatesPlayerStatistics> { }

	public class UpdatesPlayerStatistics : INotifyKilled
	{
		public void Killed(Actor self, AttackInfo e)
		{
			if (self.Owner.WinState != WinState.Undefined)
				return;

			var attackerStats = e.Attacker.Owner.PlayerActor.Trait<PlayerStatistics>();
			var defenderStats = self.Owner.PlayerActor.Trait<PlayerStatistics>();
			if (self.HasTrait<Building>())
			{
				attackerStats.BuildingsKilled++;
				defenderStats.BuildingsDead++;
			}
			else if (self.HasTrait<IPositionable>())
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
