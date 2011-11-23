#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA;
using OpenRA.Mods.RA.Orders;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class OrderButtonsChromeLogic
	{
		[ObjectCreator.UseCtor]
		public OrderButtonsChromeLogic(World world)
		{
			var r = Widget.RootWidget;
			var gameRoot = r.GetWidget("INGAME_ROOT");

			var moneybin = gameRoot.GetWidget("INGAME_MONEY_BIN");
			moneybin.IsVisible = () => {
				return world.LocalPlayer.WinState == WinState.Undefined;
			};
			
			var sell = moneybin.GetWidget<OrderButtonWidget>("SELL");
			if (sell != null)
			{
				sell.Pressed = () => world.OrderGenerator is SellOrderGenerator;
				sell.OnMouseDown = mi => world.ToggleInputMode<SellOrderGenerator>();
			}

			var powerdown = moneybin.GetWidget<OrderButtonWidget>("POWER_DOWN");
			if (powerdown != null)
			{
				powerdown.Pressed = () => world.OrderGenerator is PowerDownOrderGenerator;
				powerdown.OnMouseDown = mi => world.ToggleInputMode<PowerDownOrderGenerator>();
			}

			var repair = moneybin.GetWidget<OrderButtonWidget>("REPAIR");
			if (repair != null)
			{
				repair.Enabled = () => { return RepairOrderGenerator.PlayerIsAllowedToRepair( world ); };
				repair.Pressed = () => world.OrderGenerator is RepairOrderGenerator;
				repair.OnMouseDown = mi => world.ToggleInputMode<RepairOrderGenerator>();
				repair.GetLongDesc = () => { return repair.Enabled() ? repair.LongDesc : repair.LongDesc + "\n\nRequires: Construction Yard"; };
			}
		}
	}
}
