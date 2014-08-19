#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MainMenuLogic
	{
		protected enum MenuType { Main, Singleplayer, Multiplayer, Extras, None }
		protected enum ServerType { Create, Load, Direct, None }

		protected MenuType menuType = MenuType.Main;
		protected ServerType serverMenu = ServerType.None;
		readonly Widget rootMenu;
		readonly ScrollPanelWidget newsPanel;
		readonly Widget newsTemplate;
		readonly LabelWidget newsStatus;
		bool newsHighlighted = false;

		[ObjectCreator.UseCtor]
		public MainMenuLogic(Widget widget, World world)
		{
			rootMenu = widget;
			rootMenu.Get<LabelWidget>("VERSION_LABEL").Text = Game.modData.Manifest.Mod.Version;

			// Menu buttons
			var mainMenu = widget.Get("MAIN_MENU");
			mainMenu.IsVisible = () => menuType == MenuType.Main;

			mainMenu.Get<ButtonWidget>("SINGLEPLAYER_BUTTON").OnClick = () => menuType = MenuType.Singleplayer;

			mainMenu.Get<ButtonWidget>("MULTIPLAYER_BUTTON").OnClick = () => menuType = MenuType.Multiplayer;

			mainMenu.Get<ButtonWidget>("MODS_BUTTON").OnClick = () =>
			{
				Game.Settings.Game.PreviousMod = Game.modData.Manifest.Mod.Id;
				Game.InitializeMod("modchooser", null);
			};

			mainMenu.Get<ButtonWidget>("SETTINGS_BUTTON").OnClick = () =>
			{
				menuType = MenuType.None;
				Game.OpenWindow("SETTINGS_PANEL", new WidgetArgs
				{
					{ "onExit", () => menuType = MenuType.Main }
				});
			};

			mainMenu.Get<ButtonWidget>("EXTRAS_BUTTON").OnClick = () => menuType = MenuType.Extras;

			mainMenu.Get<ButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;

			// Singleplayer menu
			var singleplayerMenu = widget.Get("SINGLEPLAYER_MENU");
			singleplayerMenu.IsVisible = () => menuType == MenuType.Singleplayer;

			var missionsButton = singleplayerMenu.Get<ButtonWidget>("MISSIONS_BUTTON");
			missionsButton.OnClick = () =>
			{
				menuType = MenuType.None;
				Ui.OpenWindow("MISSIONBROWSER_PANEL", new WidgetArgs
				{
					{ "onExit", () => menuType = MenuType.Singleplayer },
					{ "onStart", RemoveShellmapUI }
				});
			};
			missionsButton.Disabled = !Game.modData.Manifest.Missions.Any();

			singleplayerMenu.Get<ButtonWidget>("SKIRMISH_BUTTON").OnClick = StartSkirmishGame;

			singleplayerMenu.Get<ButtonWidget>("LOAD_BUTTON").OnClick = StartLoadGame;

			singleplayerMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => menuType = MenuType.Main;

			//Multiplayer menu
			var multiplayerMenu = widget.Get("MULTIPLAYER_MENU");
			multiplayerMenu.IsVisible = () => menuType == MenuType.Multiplayer;

			var createButton = multiplayerMenu.Get<ButtonWidget>("CREATE_BUTTON");
			var directButton = multiplayerMenu.Get<ButtonWidget>("DIRECTCONNECT_BUTTON");
			var loadButton = multiplayerMenu.Get<ButtonWidget>("LOAD_BUTTON");

			createButton.IsHighlighted = () => serverMenu == ServerType.Create;
			directButton.IsHighlighted = () => serverMenu == ServerType.Direct;
			loadButton.IsHighlighted = () => serverMenu == ServerType.Load;

			createButton.OnClick = () => { OpenWindow(OpenCreateServerPanel, ServerType.Create); };
			directButton.OnClick = () => { OpenWindow(OpenDirectConnectPanel, ServerType.Direct); };
			loadButton.OnClick = () => { OpenWindow(OpenLoadServerPanel, ServerType.Load); };

			multiplayerMenu.Get<ButtonWidget>("JOIN_BUTTON").OnClick = () =>
			{
				if (serverMenu != ServerType.None)
					Ui.CloseWindow();
				serverMenu = ServerType.None;
				menuType = MenuType.None;
				Ui.OpenWindow("SERVERBROWSER_PANEL", new WidgetArgs
				{
					{ "onStart", RemoveShellmapUI },
					{ "onExit", () => menuType = MenuType.Multiplayer }
				});
			};

			multiplayerMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				if (serverMenu != ServerType.None)
					Ui.CloseWindow();
				serverMenu = ServerType.None;
				menuType = MenuType.Main;
			};
			
			// Extras menu
			var extrasMenu = widget.Get("EXTRAS_MENU");
			extrasMenu.IsVisible = () => menuType == MenuType.Extras;

			extrasMenu.Get<ButtonWidget>("REPLAYS_BUTTON").OnClick = () =>
			{
				menuType = MenuType.None;
				Ui.OpenWindow("REPLAYBROWSER_PANEL", new WidgetArgs
				{
					{ "onExit", () => menuType = MenuType.Extras },
					{ "onStart", RemoveShellmapUI }
				});
			};

			extrasMenu.Get<ButtonWidget>("MUSIC_BUTTON").OnClick = () =>
			{
				menuType = MenuType.None;
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs
				{
					{ "onExit", () => menuType = MenuType.Extras },
					{ "world", world }
				});
			};

			var assetBrowserButton = extrasMenu.GetOrNull<ButtonWidget>("ASSETBROWSER_BUTTON");
			if (assetBrowserButton != null)
				assetBrowserButton.OnClick = () =>
				{
					menuType = MenuType.None;
					Game.OpenWindow("ASSETBROWSER_PANEL", new WidgetArgs
					{
						{ "onExit", () => menuType = MenuType.Extras },
					});
				};

			extrasMenu.Get<ButtonWidget>("CREDITS_BUTTON").OnClick = () =>
			{
				menuType = MenuType.None;
				Ui.OpenWindow("CREDITS_PANEL", new WidgetArgs
				{
					{ "onExit", () => menuType = MenuType.Extras },
				});
			};

			extrasMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => menuType = MenuType.Main;

			var newsBG = widget.GetOrNull("NEWS_BG");
			if (newsBG != null)
			{
				newsBG.IsVisible = () => Game.Settings.Game.FetchNews && menuType != MenuType.None;

				newsPanel = Ui.LoadWidget<ScrollPanelWidget>("NEWS_PANEL", null, new WidgetArgs());
				newsTemplate = newsPanel.Get("NEWS_ITEM_TEMPLATE");
				newsPanel.RemoveChild(newsTemplate);

				newsStatus = newsPanel.Get<LabelWidget>("NEWS_STATUS");
				SetNewsStatus("Loading news");

				var cacheFile = Path.Combine(Platform.SupportDir, "news.yaml");
				var currentNews = ParseNews(cacheFile);
				if (currentNews != null)
					DisplayNews(currentNews);

				// Only query for new stories once per day
				var cacheValid = currentNews != null && DateTime.Today.ToUniversalTime() <= Game.Settings.Game.NewsFetchedDate;
				if (!cacheValid)
					new Download(Game.Settings.Game.NewsUrl, cacheFile, e => { }, (e, c) => NewsDownloadComplete(e, c, cacheFile, currentNews));

				var newsButton = newsBG.GetOrNull<DropDownButtonWidget>("NEWS_BUTTON");
				newsButton.OnClick = () =>
				{
					newsButton.AttachPanel(newsPanel);
					newsHighlighted = false;
				};

				newsButton.IsHighlighted = () => newsHighlighted && Game.LocalTick % 50 < 25;
			}
		}

		void SetNewsStatus(string message)
		{
			message = WidgetUtils.WrapText(message, newsStatus.Bounds.Width, Game.Renderer.Fonts[newsStatus.Font]);
			newsStatus.GetText = () => message;
		}

		class NewsItem
		{
			public string Title;
			public string Author;
			public DateTime DateTime;
			public string Content;
		}

		NewsItem[] ParseNews(string path)
		{
			if (!File.Exists(path))
				return null;

			try
			{
				return MiniYaml.FromFile(path).Select(node =>
				{
					var nodesDict = node.Value.ToDictionary();
					return new NewsItem
					{
						Title = nodesDict["Title"].Value,
						Author = nodesDict["Author"].Value,
						DateTime = FieldLoader.GetValue<DateTime>("DateTime", node.Key),
						Content = nodesDict["Content"].Value
					};
				}).ToArray();
			}
			catch (Exception ex)
			{
				SetNewsStatus("Failed to parse news: {0}".F(ex.Message));
			}

			return null;
		}

		void NewsDownloadComplete(AsyncCompletedEventArgs e, bool cancelled, string cacheFile, NewsItem[] oldNews)
		{
			Game.RunAfterTick(() => // run on the main thread
			{
				if (e.Error != null)
				{
					SetNewsStatus("Failed to retrieve news: {0}".F(Download.FormatErrorMessage(e.Error)));
					return;
				}

				var newNews = ParseNews(cacheFile);
				if (newNews == null)
					return;

				DisplayNews(newNews);

				if (oldNews == null || newNews.Any(n => !oldNews.Select(c => c.DateTime).Contains(n.DateTime)))
					newsHighlighted = true;

				Game.Settings.Game.NewsFetchedDate = DateTime.Today.ToUniversalTime();
				Game.Settings.Save();
			});
		}

		void DisplayNews(IEnumerable<NewsItem> newsItems)
		{
			newsPanel.RemoveChildren();
			SetNewsStatus("");

			foreach (var i in newsItems)
			{
				var item = i;

				var newsItem = newsTemplate.Clone();

				var titleLabel = newsItem.Get<LabelWidget>("TITLE");
				titleLabel.GetText = () => item.Title;

				var authorDateTimeLabel = newsItem.Get<LabelWidget>("AUTHOR_DATETIME");
				var authorDateTime = authorDateTimeLabel.Text.F(item.Author, item.DateTime.ToLocalTime());
				authorDateTimeLabel.GetText = () => authorDateTime;

				var contentLabel = newsItem.Get<LabelWidget>("CONTENT");
				var content = item.Content.Replace("\\n", "\n");
				content = WidgetUtils.WrapText(content, contentLabel.Bounds.Width, Game.Renderer.Fonts[contentLabel.Font]);
				contentLabel.GetText = () => content;
				contentLabel.Bounds.Height = Game.Renderer.Fonts[contentLabel.Font].Measure(content).Y;
				newsItem.Bounds.Height += contentLabel.Bounds.Height;

				newsPanel.AddChild(newsItem);
				newsPanel.Layout.AdjustChildren();
			}
		}

		void RemoveShellmapUI()
		{
			rootMenu.Parent.RemoveChild(rootMenu);
		}

		void OpenWindow(Action onConfirm, ServerType type)
		{
			if (serverMenu != type)
			{
				if (serverMenu != ServerType.None)
					Ui.CloseWindow();
				serverMenu = type;
				onConfirm();
			}
		}

		void OpenSkirmishLobbyPanel()
		{
			menuType = MenuType.None;
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onExit", () => { Game.Disconnect(); menuType = MenuType.Main; } },
				{ "onStart", RemoveShellmapUI },
				{ "lobbyType", LobbyLogic.LobbyType.Server },
				{ "skirmishMode", true }
			});
		}

		void OpenSingleLoadLobby()
		{
			menuType = MenuType.None;
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onExit", () => { Game.Disconnect(); serverMenu = ServerType.None; menuType = MenuType.Main; } },
				{ "onStart", RemoveShellmapUI },
				{ "lobbyType", LobbyLogic.LobbyType.Load },
				{ "skirmishMode", true }
			});
		}

		void OpenLoadLobby()
		{
			menuType = MenuType.None;
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onExit", () => { Game.Disconnect(); serverMenu = ServerType.None; menuType = MenuType.Main; } },
				{ "onStart", RemoveShellmapUI },
				{ "lobbyType", LobbyLogic.LobbyType.Load },
				{ "skirmishMode", false }
			});
		}

		void OpenLobby()
		{
			menuType = MenuType.None;
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onExit", () => { Game.Disconnect(); serverMenu = ServerType.None; menuType = MenuType.Main; } },
				{ "onStart", RemoveShellmapUI },
				{ "lobbyType", LobbyLogic.LobbyType.Server },
				{ "skirmishMode", false }
			});
		}

		void OpenLoadServerPanel()
		{
			Ui.OpenWindow("LOADSERVER_PANEL", new WidgetArgs
			{
				{ "openLobby", OpenLoadLobby },
				{ "onExit", () => serverMenu = ServerType.None }
			});
		}

		void OpenCreateServerPanel()
		{
			Ui.OpenWindow("CREATESERVER_PANEL", new WidgetArgs
			{
				{ "openLobby", OpenLobby },
				{ "onExit", () => serverMenu = ServerType.None }
			});
		}

		void OpenDirectConnectPanel()
		{
			Ui.OpenWindow("DIRECTCONNECT_PANEL", new WidgetArgs
			{
				{ "openLobby", OpenLobby },
				{ "onExit", () => serverMenu = ServerType.None }
			});
		}

		void StartSkirmishGame()
		{
			var map = WidgetUtils.ChooseInitialMap(Game.Settings.Server.Map);
			Game.Settings.Server.Map = map;
			Game.Settings.Save();

			ConnectionLogic.Connect(IPAddress.Loopback.ToString(),
				Game.CreateLocalServer(map),
				"",
				OpenSkirmishLobbyPanel,
				() => { Game.CloseServer(); menuType = MenuType.Main; });
		}

		void StartLoadGame()
		{
			var map = WidgetUtils.ChooseInitialMap(Game.Settings.Server.Map);
			Game.Settings.Server.Map = map;
			Game.Settings.Save();

			ConnectionLogic.Connect(IPAddress.Loopback.ToString(),
				Game.CreateLocalServer(map),
				"",
				OpenSingleLoadLobby,
				() => { Game.CloseServer(); menuType = MenuType.Main; });
		}
	}
}
