#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Orders;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SellOrderButtonLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SellOrderButtonLogic(Widget widget, World world)
		{
			if (widget is ButtonWidget sell)
				OrderButtonsChromeUtils.BindOrderButton<SellOrderGenerator>(world, sell, "sell");
		}
	}

	public class RepairOrderButtonLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public RepairOrderButtonLogic(Widget widget, World world)
		{
			if (widget is ButtonWidget repair)
				OrderButtonsChromeUtils.BindOrderButton<RepairOrderGenerator>(world, repair, "repair");
		}
	}

	public class PowerdownOrderButtonLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public PowerdownOrderButtonLogic(Widget widget, World world)
		{
			if (widget is ButtonWidget power)
				OrderButtonsChromeUtils.BindOrderButton<PowerDownOrderGenerator>(world, power, "power");
		}
	}

	public class BeaconOrderButtonLogic : ChromeLogic
	{
		static string beaconCursor;

		[ObjectCreator.UseCtor]
		public BeaconOrderButtonLogic(Widget widget, World world)
		{
			beaconCursor ??= world.LocalPlayer.PlayerActor.Info.TraitInfo<PlaceBeaconInfo>().BeaconCursor;

			if (widget is ButtonWidget beacon)
				OrderButtonsChromeUtils.BindOrderButton<BeaconOrderGenerator>(world, beacon, "beacon", og => og.BeaconCursor = beaconCursor);
		}
	}

	public static class OrderButtonsChromeUtils
	{
		public static void BindOrderButton<T>(World world, ButtonWidget w, string icon, Action<T> initializeOrderGenerator = null)
			where T : IOrderGenerator, new()
		{
			w.OnClick = () => { world.ToggleInputMode<T>(); if (world.OrderGenerator is T t) initializeOrderGenerator?.Invoke(t); };
			w.IsHighlighted = () => world.OrderGenerator is T;

			w.Get<ImageWidget>("ICON").GetImageName =
				() => world.OrderGenerator is T ? icon + "-active" : icon;
		}
	}
}
