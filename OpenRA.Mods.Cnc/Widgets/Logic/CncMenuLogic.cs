#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Net;
using OpenRA.Mods.RA.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncMenuLogic
	{
		enum MenuType { Main, Multiplayer, Settings, None }

		MenuType menuType = MenuType.Main;
		Widget rootMenu;

		[ObjectCreator.UseCtor]
		public CncMenuLogic(Widget widget, World world)
		{
			world.WorldActor.Trait<CncMenuPaletteEffect>()
				.Fade(CncMenuPaletteEffect.EffectType.Desaturated);

			rootMenu = widget.Get("MENU_BACKGROUND");
			rootMenu.Get<LabelWidget>("VERSION_LABEL").GetText = WidgetUtils.ActiveModVersion;

			// Menu buttons
			var mainMenu = widget.Get("MAIN_MENU");
			mainMenu.IsVisible = () => menuType == MenuType.Main;

			mainMenu.Get<ButtonWidget>("SOLO_BUTTON").OnClick = StartSkirmishGame;
			mainMenu.Get<ButtonWidget>("MULTIPLAYER_BUTTON").OnClick = () => menuType = MenuType.Multiplayer;

			mainMenu.Get<ButtonWidget>("MODS_BUTTON").OnClick = () =>
			{
				menuType = MenuType.None;
				Ui.OpenWindow("MODS_PANEL", new WidgetArgs()
				{
					{ "onExit", () => menuType = MenuType.Main },
					{ "onSwitch", RemoveShellmapUI }
				});
			};

			mainMenu.Get<ButtonWidget>("SETTINGS_BUTTON").OnClick = () => menuType = MenuType.Settings;
			mainMenu.Get<ButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;

			// Multiplayer menu
			var multiplayerMenu = widget.Get("MULTIPLAYER_MENU");
			multiplayerMenu.IsVisible = () => menuType == MenuType.Multiplayer;

			multiplayerMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => menuType = MenuType.Main;
			multiplayerMenu.Get<ButtonWidget>("JOIN_BUTTON").OnClick = () => OpenGamePanel("SERVERBROWSER_PANEL");
			multiplayerMenu.Get<ButtonWidget>("CREATE_BUTTON").OnClick = () => OpenGamePanel("CREATESERVER_PANEL");
			multiplayerMenu.Get<ButtonWidget>("DIRECTCONNECT_BUTTON").OnClick = () => OpenGamePanel("DIRECTCONNECT_PANEL");

			// Settings menu
			var settingsMenu = widget.Get("SETTINGS_MENU");
			settingsMenu.IsVisible = () => menuType == MenuType.Settings;

			settingsMenu.Get<ButtonWidget>("REPLAYS_BUTTON").OnClick = () =>
			{
				menuType = MenuType.None;
				Ui.OpenWindow("REPLAYBROWSER_PANEL", new WidgetArgs()
				{
					{ "onExit", () => menuType = MenuType.Settings },
					{ "onStart", RemoveShellmapUI }
				});
			};

			settingsMenu.Get<ButtonWidget>("MUSIC_BUTTON").OnClick = () =>
			{
				menuType = MenuType.None;
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs()
				{
					{ "onExit", () => menuType = MenuType.Settings },
				});
			};

			settingsMenu.Get<ButtonWidget>("CREDITS_BUTTON").OnClick = () =>
			{
				menuType = MenuType.None;
				Ui.OpenWindow("CREDITS_PANEL", new WidgetArgs()
				{
					{ "onExit", () => menuType = MenuType.Settings },
				});
			};

			settingsMenu.Get<ButtonWidget>("SETTINGS_BUTTON").OnClick = () =>
			{
				menuType = MenuType.None;
				Game.OpenWindow("SETTINGS_PANEL", new WidgetArgs()
				{
					{ "onExit", () => menuType = MenuType.Settings },
				});
			};

			settingsMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => menuType = MenuType.Main;

			rootMenu.Get<ImageWidget>("RECBLOCK").IsVisible = () => world.FrameNumber / 25 % 2 == 0;
		}
		
		void OpenGamePanel(string id)
		{
			menuType = MenuType.None;
			Ui.OpenWindow(id, new WidgetArgs()
			{
				{ "onExit", () => menuType = MenuType.Multiplayer },
				{ "openLobby", () => OpenLobbyPanel(MenuType.Multiplayer, false) }
			});
		}

		void RemoveShellmapUI()
		{
			rootMenu.Parent.RemoveChild(rootMenu);
		}

		void OpenLobbyPanel(MenuType menu, bool addBots)
		{
			menuType = MenuType.None;
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs()
			{
				{ "onExit", () => { Game.Disconnect(); menuType = menu; } },
				{ "onStart", RemoveShellmapUI },
				{ "addBots", addBots }
			});
		}

		void StartSkirmishGame()
		{
			var map = WidgetUtils.ChooseInitialMap(Game.Settings.Server.Map);

			ConnectionLogic.Connect(IPAddress.Loopback.ToString(),
				Game.CreateLocalServer(map),
				"",
				() => OpenLobbyPanel(MenuType.Main, true),
				() => { Game.CloseServer(); menuType = MenuType.Main; });
		}
	}
}
