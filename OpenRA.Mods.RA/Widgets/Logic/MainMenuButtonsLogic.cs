#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MainMenuButtonsLogic
	{
		enum MenuType { Main, None }
		MenuType Menu = MenuType.Main;

		Widget rootMenu;

		[ObjectCreator.UseCtor]
		public MainMenuButtonsLogic(Widget widget)
		{
			rootMenu = widget;
			rootMenu.IsVisible = () => Menu == MenuType.Main;

			Game.modData.WidgetLoader.LoadWidget( new WidgetArgs(), Ui.Root, "PERF_BG" );
			var versionLabel = Ui.Root.GetOrNull<LabelWidget>("VERSION_LABEL");
			if (versionLabel != null)
				versionLabel.GetText = WidgetUtils.ActiveModVersion;

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_JOIN").OnClick = () => OpenGamePanel("JOINSERVER_BG");
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_CREATE").OnClick = () => OpenGamePanel("CREATESERVER_BG");
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_DIRECTCONNECT").OnClick = () => OpenGamePanel("DIRECTCONNECT_BG");

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_SETTINGS").OnClick = () =>
			{
				Menu = MenuType.None;
				Ui.OpenWindow("SETTINGS_MENU", new WidgetArgs()
				{
					{ "onExit", () => Menu = MenuType.Main }
				});
			};

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_MUSIC").OnClick = () =>
			{
				Menu = MenuType.None;
				Ui.OpenWindow("MUSIC_MENU", new WidgetArgs()
				{
					{ "onExit", () => Menu = MenuType.Main }
				});
			};

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_MODS").OnClick = () =>
			{
				Menu = MenuType.None;
				Ui.OpenWindow("MODS_PANEL", new WidgetArgs()
				{
					{ "onExit", () => Menu = MenuType.Main },
					{ "onSwitch", RemoveShellmapUI }
				});
			};

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_REPLAY_VIEWER").OnClick = () =>
			{
				Menu = MenuType.None;
				Ui.OpenWindow("REPLAYBROWSER_BG", new WidgetArgs()
				{
					{ "onExit", () => Menu = MenuType.Main },
					{ "onStart", RemoveShellmapUI }
				});
			};

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_ASSET_BROWSER").OnClick = () =>
			{
				Menu = MenuType.None;
				Game.OpenWindow("ASSETBROWSER_BG", new WidgetArgs()
				{
					{ "onExit", () => Menu = MenuType.Main }
				});
			};

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_QUIT").OnClick = () => Game.Exit();
		}

		void RemoveShellmapUI()
		{
			rootMenu.Parent.RemoveChild(rootMenu);
		}

		void OpenGamePanel(string id)
		{
			Menu = MenuType.None;
			Ui.OpenWindow(id, new WidgetArgs()
			{
				{ "onExit", () => Menu = MenuType.Main },
				{ "openLobby", () => OpenLobbyPanel() }
			});
		}

		void OpenLobbyPanel()
		{
			Menu = MenuType.None;
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs()
			{
				{ "onExit", () => { Game.Disconnect(); Menu = MenuType.Main; } },
				{ "onStart", RemoveShellmapUI },
				{ "addBots", false }
			});
		}
	}
}
