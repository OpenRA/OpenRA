#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
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

		public PlayerStatistics(Actor self)
		{
			world = self.World;
			player = self.Owner;
		}

		public double MapControl;

		void UpdateMapControl()
		{
			var total = (double)world.Map.Bounds.Width * world.Map.Bounds.Height;
			MapControl = world.Actors
				.Where(a => !a.IsDead() && a.IsInWorld && a.Owner == player && a.HasTrait<RevealsShroud>())
				.SelectMany(a => world.FindTilesInCircle(a.Location, a.Trait<RevealsShroud>().RevealRange))
				.Distinct()
				.Count() / total;
		}

		public void Tick(Actor self)
		{
			if (self.World.FrameNumber % 250 == 1)
			{
				UpdateMapControl();
			}
		}

		public int OrderCount;

		public void ResolveOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "Chat":
				case "TeamChat":
				case "HandshakeResponse":
				case "PauseRequest":
				case "PauseGame":
				case "StartGame":
				case "Disconnected":
				case "ServerError":
				case "SyncInfo":
					return;
			}
			if (order.OrderString.StartsWith("Dev"))
			{
				return;
			}
			OrderCount++;
		}
	}
}
