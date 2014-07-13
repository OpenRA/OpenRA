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
		readonly World world;
		readonly Widget ingameRoot;
		bool disableSystemButtons;

		[ObjectCreator.UseCtor]
		public OrderButtonsChromeLogic(Widget widget, World world)
		{
			this.world = world;
			ingameRoot = Ui.Root.Get("INGAME_ROOT");

			// Order Buttons
			var sell = widget.GetOrNull<ButtonWidget>("SELL_BUTTON");
			if (sell != null)
			{
				sell.GetKey = _ => Game.Settings.Keys.SellKey;
				BindOrderButton<SellOrderGenerator>(world, sell, "sell");
			}

			var repair = widget.GetOrNull<ButtonWidget>("REPAIR_BUTTON");
			if (repair != null)
			{
				repair.GetKey = _ => Game.Settings.Keys.RepairKey;
				BindOrderButton<RepairOrderGenerator>(world, repair, "repair");
			}

			var beacon = widget.GetOrNull<ButtonWidget>("BEACON_BUTTON");
			if (beacon != null)
			{
				beacon.GetKey = _ => Game.Settings.Keys.PlaceBeaconKey;
				BindOrderButton<BeaconOrderGenerator>(world, beacon, "beacon");
			}

			var power = widget.GetOrNull<ButtonWidget>("POWER_BUTTON");
			if (power != null)
			{
				power.GetKey = _ => Game.Settings.Keys.PowerDownKey;
				BindOrderButton<PowerDownOrderGenerator>(world, power, "power");
			}

			// System buttons
			var options = widget.GetOrNull<MenuButtonWidget>("OPTIONS_BUTTON");
			if (options != null)
			{
				options.IsDisabled = () => disableSystemButtons;
				options.OnClick = () => OpenMenuPanel(options);
			}

		}

		void OpenMenuPanel(MenuButtonWidget button)
		{
			disableSystemButtons = true;
			var cachedPause = world.PredictedPaused;

			if (button.HideIngameUI)
				ingameRoot.IsVisible = () => false;

			if (button.Pause && world.LobbyInfo.IsSinglePlayer)
				world.SetPauseState(true);

			Game.LoadWidget(world, button.MenuContainer, Ui.Root, new WidgetArgs()
			{
				{ "transient", true },
				{ "onExit", () =>
					{
						if (button.HideIngameUI)
							ingameRoot.IsVisible = () => true;

						if (button.Pause && world.LobbyInfo.IsSinglePlayer)
							world.SetPauseState(cachedPause);

						disableSystemButtons = false;
					}
				}
			});
		}

		static void BindOrderButton<T>(World world, ButtonWidget w, string icon)
			where T : IOrderGenerator, new()
		{
			w.OnClick = () => world.ToggleInputMode<T>();
			w.IsHighlighted = () => world.OrderGenerator is T;

			w.Get<ImageWidget>("ICON").GetImageName =
				() => world.OrderGenerator is T ? icon + "-active" : icon;
		}
	}
}
