#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Orders;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SellOrderButtonLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SellOrderButtonLogic(Widget widget, World world)
		{
			var sell = widget as ButtonWidget;
			if (sell != null)
			{
				sell.GetKey = _ => Game.Settings.Keys.SellKey;
				OrderButtonsChromeUtils.BindOrderButton<SellOrderGenerator>(world, sell, "sell");
			}
		}
	}

	public class RepairOrderButtonLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public RepairOrderButtonLogic(Widget widget, World world)
		{
			var repair = widget as ButtonWidget;
			if (repair != null)
			{
				repair.GetKey = _ => Game.Settings.Keys.RepairKey;
				OrderButtonsChromeUtils.BindOrderButton<RepairOrderGenerator>(world, repair, "repair");
			}
		}
	}

	public class PowerdownOrderButtonLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public PowerdownOrderButtonLogic(Widget widget, World world)
		{
			var power = widget as ButtonWidget;
			if (power != null)
			{
				power.GetKey = _ => Game.Settings.Keys.PowerDownKey;
				OrderButtonsChromeUtils.BindOrderButton<PowerDownOrderGenerator>(world, power, "power");
			}
		}
	}

	public class BeaconOrderButtonLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public BeaconOrderButtonLogic(Widget widget, World world)
		{
			var beacon = widget as ButtonWidget;
			if (beacon != null)
			{
				beacon.GetKey = _ => Game.Settings.Keys.PlaceBeaconKey;
				OrderButtonsChromeUtils.BindOrderButton<BeaconOrderGenerator>(world, beacon, "beacon");
			}
		}
	}

	public class OrderButtonsChromeUtils
	{
		public static void BindOrderButton<T>(World world, ButtonWidget w, string icon)
			where T : IOrderGenerator, new()
		{
			w.OnClick = () => world.ToggleInputMode<T>();
			w.IsHighlighted = () => world.OrderGenerator is T;

			w.Get<ImageWidget>("ICON").GetImageName =
				() => world.OrderGenerator is T ? icon + "-active" : icon;
		}
	}
}
