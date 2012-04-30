#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
		Widget rootMenu;

		[ObjectCreator.UseCtor]
		public MainMenuButtonsLogic(Widget widget)
		{
			rootMenu = widget;

			Game.modData.WidgetLoader.LoadWidget( new WidgetArgs(), Ui.Root, "PERF_BG" );
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_JOIN").OnClick = () => OpenGamePanel("JOINSERVER_BG");
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_CREATE").OnClick = () => OpenGamePanel("CREATESERVER_BG");
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_DIRECTCONNECT").OnClick = () => OpenGamePanel("DIRECTCONNECT_BG");
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_SETTINGS").OnClick = () => Ui.OpenWindow("SETTINGS_MENU");
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_MUSIC").OnClick = () => Ui.OpenWindow("MUSIC_MENU");

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_MODS").OnClick = () =>
				Ui.OpenWindow("MODS_PANEL", new WidgetArgs()
				{
					{ "onExit", () => {} },
					{ "onSwitch", RemoveShellmapUI }
				});

			widget.Get<ButtonWidget>("MAINMENU_BUTTON_REPLAY_VIEWER").OnClick = () =>
				Ui.OpenWindow("REPLAYBROWSER_BG", new WidgetArgs()
				{
					{ "onExit", () => {} },
					{ "onStart", RemoveShellmapUI }
				});
			widget.Get<ButtonWidget>("MAINMENU_BUTTON_QUIT").OnClick = () => Game.Exit();
		}

		void RemoveShellmapUI()
		{
			rootMenu.Parent.RemoveChild(rootMenu);
		}

		void OpenGamePanel(string id)
		{
			Ui.OpenWindow(id, new WidgetArgs()
			{
				{ "onExit", () => {} },
				{ "openLobby", () => OpenLobbyPanel() }
			});
		}

		void OpenLobbyPanel()
		{
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs()
			{
				{ "onExit", () => { Game.Disconnect(); } },
				{ "onStart", RemoveShellmapUI },
				{ "addBots", false }
			});
		}
	}
}
