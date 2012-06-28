﻿#region Copyright & License Information
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
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
{
	public class GlobalButtonOrderGenerator<T> : IOrderGenerator
	{
		string cursor;
		string order;

		public GlobalButtonOrderGenerator(string cursor, string order)
		{
			this.cursor = cursor;
			this.order = order;
		}

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			return OrderInner(world, xy, mi);
		}

		IEnumerable<Order> OrderInner(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var underCursor = world.FindUnitsAtMouse(mi.Location)
					.Where(a => a.Owner == world.LocalPlayer
						&& a.HasTrait<T>()).FirstOrDefault();

				if (underCursor != null)
					yield return new Order(order, underCursor, false);
			}
		}

		public void Tick(World world)
		{
			if (world.LocalPlayer != null &&
				world.LocalPlayer.WinState != WinState.Undefined)
				world.CancelInputMode();
		}

		public void RenderAfterWorld(WorldRenderer wr, World world) { }
		public void RenderBeforeWorld(WorldRenderer wr, World world) { }

		public string GetCursor(World world, CPos xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return cursor + (OrderInner(world, xy, mi).Any()	? "" : "-blocked");
		}
	}

	public class PowerDownOrderGenerator : GlobalButtonOrderGenerator<CanPowerDown>
	{
		public PowerDownOrderGenerator() : base( "powerdown", "PowerDown" ) { }
	}

	public class SellOrderGenerator : GlobalButtonOrderGenerator<Sellable>
	{
		public SellOrderGenerator() : base( "sell", "Sell" ) { }
	}
}
