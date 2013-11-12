#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Orders;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class OrderButtonsChromeLogic
	{
		[ObjectCreator.UseCtor]
		public OrderButtonsChromeLogic(World world)
		{
			/* TODO: attach this to the correct widget, to remove the lookups below */
			var r = Ui.Root;
			var gameRoot = r.Get("INGAME_ROOT");

			var moneybin = gameRoot.Get("INGAME_MONEY_BIN");
			moneybin.IsVisible = () => {
				return world.LocalPlayer.WinState == WinState.Undefined;
			};

			BindOrderButton<SellOrderGenerator>(world, moneybin, "SELL");
			BindOrderButton<PowerDownOrderGenerator>(world, moneybin, "POWER_DOWN");
			BindOrderButton<RepairOrderGenerator>(world, moneybin, "REPAIR");
		}

		static void BindOrderButton<T>(World world, Widget parent, string button)
			where T : IOrderGenerator, new()
		{
			var w = parent.GetOrNull<OrderButtonWidget>(button);
			if (w != null)
			{
				w.Pressed = () => world.OrderGenerator is T;
				w.OnMouseDown = mi => world.ToggleInputMode<T>();
				w.OnKeyPress = ki => world.ToggleInputMode<T>();
			}
		}
	}
}
