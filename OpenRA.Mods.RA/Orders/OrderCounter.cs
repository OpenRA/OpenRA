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

namespace OpenRA.Mods.RA.Orders
{
	public class OrderCounterInfo : TraitInfo<OrderCounter> { }

	public class OrderCounter : IResolveOrder
	{
		public int Orders;

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
			Orders++;
		}

		public static double OrdersPerMinute(OrderCounter counter, World world)
		{
			return world.FrameNumber == 0 ? 0 : counter.Orders / (world.FrameNumber / 1500.0);
		}
	}
}
