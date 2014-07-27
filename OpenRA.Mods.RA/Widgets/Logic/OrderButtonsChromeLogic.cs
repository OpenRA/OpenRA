#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.RA.Orders;
using OpenRA.Widgets;
using OpenRA.Traits;

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
				var blinking = false;
				var lp = world.LocalPlayer;
				options.IsDisabled = () => disableSystemButtons;
				options.OnClick = () =>
				{
					blinking = false;
					OpenMenuPanel(options, new WidgetArgs()
					{
						{ "activePanel", IngameInfoPanel.AutoSelect }
					});
				};
				options.IsHighlighted = () => blinking && Game.LocalTick % 50 < 25;

				if (lp != null)
				{
					Action<Player> StartBlinking = player =>
					{
						if (player == world.LocalPlayer)
							blinking = true;
					};

					var mo = lp.PlayerActor.TraitOrDefault<MissionObjectives>();

					if (mo != null)
						mo.ObjectiveAdded += StartBlinking;
				}
			}

			var diplomacy = widget.GetOrNull<MenuButtonWidget>("DIPLOMACY_BUTTON");
			if (diplomacy != null)
			{
				diplomacy.Visible = world.Players.Any(a => a != world.LocalPlayer && !a.NonCombatant);
				diplomacy.IsDisabled = () => disableSystemButtons;
				diplomacy.OnClick = () => OpenMenuPanel(diplomacy);
			}

			var debug = widget.GetOrNull<MenuButtonWidget>("DEBUG_BUTTON");
			if (debug != null)
			{
				debug.IsVisible = () => world.LobbyInfo.GlobalSettings.AllowCheats;
				debug.IsDisabled = () => disableSystemButtons;
				debug.OnClick = () => OpenMenuPanel(debug, new WidgetArgs()
				{
					{ "activePanel", IngameInfoPanel.Debug }
				});
			}

			var stats = widget.GetOrNull<MenuButtonWidget>("OBSERVER_STATS_BUTTON");
			if (stats != null)
			{
				stats.IsDisabled = () => disableSystemButtons;
				stats.OnClick = () => OpenMenuPanel(stats);
			}
		}

		void OpenMenuPanel(MenuButtonWidget button, WidgetArgs widgetArgs = null)
		{
			disableSystemButtons = true;
			var cachedPause = world.PredictedPaused;

			if (button.HideIngameUI)
				ingameRoot.IsVisible = () => false;

			if (button.Pause && world.LobbyInfo.IsSinglePlayer)
				world.SetPauseState(true);

			widgetArgs = widgetArgs ?? new WidgetArgs();
			widgetArgs.Add("onExit", () =>
			{
				if (button.HideIngameUI)
					ingameRoot.IsVisible = () => true;

				if (button.Pause && world.LobbyInfo.IsSinglePlayer)
					world.SetPauseState(cachedPause);

				disableSystemButtons = false;
			});

			Game.LoadWidget(world, button.MenuContainer, Ui.Root, widgetArgs);
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
