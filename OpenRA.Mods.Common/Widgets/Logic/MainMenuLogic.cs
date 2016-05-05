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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MainMenuLogic : ChromeLogic
	{
		protected enum MenuType { Main, Singleplayer, Extras, MapEditor, None }

		protected MenuType menuType = MenuType.Main;
		readonly Widget rootMenu;
		readonly ScrollPanelWidget newsPanel;
		readonly Widget newsTemplate;
		readonly LabelWidget newsStatus;

		// Update news once per game launch
		static bool fetchedNews;

		void SwitchMenu(MenuType type)
		{
			menuType = type;

			// Update button mouseover
			Game.RunAfterTick(Ui.ResetTooltips);
		}

		[ObjectCreator.UseCtor]
		public MainMenuLogic(Widget widget, World world, ModData modData)
		{
			rootMenu = widget;
			rootMenu.Get<LabelWidget>("VERSION_LABEL").Text = modData.Manifest.Mod.Version;

			// Menu buttons
			var mainMenu = widget.Get("MAIN_MENU");
			mainMenu.IsVisible = () => menuType == MenuType.Main;

			mainMenu.Get<ButtonWidget>("SINGLEPLAYER_BUTTON").OnClick = () => SwitchMenu(MenuType.Singleplayer);

			mainMenu.Get<ButtonWidget>("MULTIPLAYER_BUTTON").OnClick = () =>
			{
				SwitchMenu(MenuType.None);
				Ui.OpenWindow("MULTIPLAYER_PANEL", new WidgetArgs
				{
					{ "onStart", RemoveShellmapUI },
					{ "onExit", () => SwitchMenu(MenuType.Main) },
					{ "directConnectHost", null },
					{ "directConnectPort", 0 },
				});
			};

			mainMenu.Get<ButtonWidget>("MODS_BUTTON").OnClick = () =>
			{
				// Switching mods changes the world state (by disposing it),
				// so we can't do this inside the input handler.
				Game.RunAfterTick(() =>
				{
					Game.Settings.Game.PreviousMod = modData.Manifest.Mod.Id;
					Game.InitializeMod("modchooser", null);
				});
			};

			mainMenu.Get<ButtonWidget>("SETTINGS_BUTTON").OnClick = () =>
			{
				SwitchMenu(MenuType.None);
				Game.OpenWindow("SETTINGS_PANEL", new WidgetArgs
				{
					{ "onExit", () => SwitchMenu(MenuType.Main) }
				});
			};

			mainMenu.Get<ButtonWidget>("EXTRAS_BUTTON").OnClick = () => SwitchMenu(MenuType.Extras);

			mainMenu.Get<ButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;

			// Singleplayer menu
			var singleplayerMenu = widget.Get("SINGLEPLAYER_MENU");
			singleplayerMenu.IsVisible = () => menuType == MenuType.Singleplayer;

			var missionsButton = singleplayerMenu.Get<ButtonWidget>("MISSIONS_BUTTON");
			missionsButton.OnClick = () =>
			{
				SwitchMenu(MenuType.None);
				Game.OpenWindow("MISSIONBROWSER_PANEL", new WidgetArgs
				{
					{ "onExit", () => SwitchMenu(MenuType.Singleplayer) },
					{ "onStart", RemoveShellmapUI }
				});
			};

			var hasCampaign = modData.Manifest.Missions.Any();
			var hasMissions = modData.MapCache
				.Any(p => p.Status == MapStatus.Available && p.Visibility.HasFlag(MapVisibility.MissionSelector));

			missionsButton.Disabled = !hasCampaign && !hasMissions;

			singleplayerMenu.Get<ButtonWidget>("SKIRMISH_BUTTON").OnClick = StartSkirmishGame;

			singleplayerMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => SwitchMenu(MenuType.Main);

			// Extras menu
			var extrasMenu = widget.Get("EXTRAS_MENU");
			extrasMenu.IsVisible = () => menuType == MenuType.Extras;

			extrasMenu.Get<ButtonWidget>("REPLAYS_BUTTON").OnClick = () =>
			{
				SwitchMenu(MenuType.None);
				Ui.OpenWindow("REPLAYBROWSER_PANEL", new WidgetArgs
				{
					{ "onExit", () => SwitchMenu(MenuType.Extras) },
					{ "onStart", RemoveShellmapUI }
				});
			};

			extrasMenu.Get<ButtonWidget>("MUSIC_BUTTON").OnClick = () =>
			{
				SwitchMenu(MenuType.None);
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs
				{
					{ "onExit", () => SwitchMenu(MenuType.Extras) },
					{ "world", world }
				});
			};

			extrasMenu.Get<ButtonWidget>("MAP_EDITOR_BUTTON").OnClick = () => SwitchMenu(MenuType.MapEditor);

			var assetBrowserButton = extrasMenu.GetOrNull<ButtonWidget>("ASSETBROWSER_BUTTON");
			if (assetBrowserButton != null)
				assetBrowserButton.OnClick = () =>
				{
					SwitchMenu(MenuType.None);
					Game.OpenWindow("ASSETBROWSER_PANEL", new WidgetArgs
					{
						{ "onExit", () => SwitchMenu(MenuType.Extras) },
					});
				};

			extrasMenu.Get<ButtonWidget>("CREDITS_BUTTON").OnClick = () =>
			{
				SwitchMenu(MenuType.None);
				Ui.OpenWindow("CREDITS_PANEL", new WidgetArgs
				{
					{ "onExit", () => SwitchMenu(MenuType.Extras) },
				});
			};

			extrasMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => SwitchMenu(MenuType.Main);

			// Map editor menu
			var mapEditorMenu = widget.Get("MAP_EDITOR_MENU");
			mapEditorMenu.IsVisible = () => menuType == MenuType.MapEditor;

			var onSelect = new Action<string>(uid =>
			{
				RemoveShellmapUI();
				LoadMapIntoEditor(modData.MapCache[uid].Uid);
			});

			var newMapButton = widget.Get<ButtonWidget>("NEW_MAP_BUTTON");
			newMapButton.OnClick = () =>
			{
				SwitchMenu(MenuType.None);
				Game.OpenWindow("NEW_MAP_BG", new WidgetArgs()
				{
					{ "onSelect", onSelect },
					{ "onExit", () => SwitchMenu(MenuType.MapEditor) }
				});
			};

			var loadMapButton = widget.Get<ButtonWidget>("LOAD_MAP_BUTTON");
			loadMapButton.OnClick = () =>
			{
				SwitchMenu(MenuType.None);
				Game.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
				{
					{ "initialMap", null },
					{ "initialTab", MapClassification.User },
					{ "onExit", () => SwitchMenu(MenuType.MapEditor) },
					{ "onSelect", onSelect },
					{ "filter", MapVisibility.Lobby | MapVisibility.Shellmap | MapVisibility.MissionSelector },
				});
			};

			mapEditorMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => SwitchMenu(MenuType.Extras);

			var newsBG = widget.GetOrNull("NEWS_BG");
			if (newsBG != null)
			{
				newsBG.IsVisible = () => Game.Settings.Game.FetchNews && menuType != MenuType.None;

				newsPanel = Ui.LoadWidget<ScrollPanelWidget>("NEWS_PANEL", null, new WidgetArgs());
				newsTemplate = newsPanel.Get("NEWS_ITEM_TEMPLATE");
				newsPanel.RemoveChild(newsTemplate);

				newsStatus = newsPanel.Get<LabelWidget>("NEWS_STATUS");
				SetNewsStatus("Loading news");

				var cacheFile = Platform.ResolvePath("^", "news.yaml");
				var currentNews = ParseNews(cacheFile);
				if (currentNews != null)
					DisplayNews(currentNews);

				var newsButton = newsBG.GetOrNull<DropDownButtonWidget>("NEWS_BUTTON");

				if (newsButton != null)
				{
					if (!fetchedNews)
						new Download(Game.Settings.Game.NewsUrl + SysInfoQuery(), cacheFile, e => { },
							(e, c) => NewsDownloadComplete(e, cacheFile, currentNews,
							() => newsButton.AttachPanel(newsPanel)));

					newsButton.OnClick = () => newsButton.AttachPanel(newsPanel);
				}
			}

			Game.OnRemoteDirectConnect += OnRemoteDirectConnect;
		}

		void OnRemoteDirectConnect(string host, int port)
		{
			SwitchMenu(MenuType.None);
			Ui.OpenWindow("MULTIPLAYER_PANEL", new WidgetArgs
			{
				{ "onStart", RemoveShellmapUI },
				{ "onExit", () => SwitchMenu(MenuType.Main) },
				{ "directConnectHost", host },
				{ "directConnectPort", port },
			});
		}

		void LoadMapIntoEditor(string uid)
		{
			ConnectionLogic.Connect(IPAddress.Loopback.ToString(),
				Game.CreateLocalServer(uid),
				"",
				() => { Game.LoadEditor(uid); },
				() => { Game.CloseServer(); SwitchMenu(MenuType.MapEditor); });
		}

		string SysInfoQuery()
		{
			if (!Game.Settings.Debug.SendSystemInformation)
				return null;

			return "?id={0}&platform={1}&os={2}&runtime={3}&gl={4}&lang={5}&version={6}&mod={7}&modversion={8}&haswritepermission={9}".F(
				Uri.EscapeUriString(Game.Settings.Debug.UUID),
				Uri.EscapeUriString(Platform.CurrentPlatform.ToString()),
				Uri.EscapeUriString(Environment.OSVersion.ToString()),
				Uri.EscapeUriString(Platform.RuntimeVersion),
				Uri.EscapeUriString(Game.Renderer.GLVersion),
				Uri.EscapeUriString(System.Globalization.CultureInfo.InstalledUICulture.TwoLetterISOLanguageName),
				Uri.EscapeUriString(ModMetadata.AllMods["modchooser"].Version),
				Uri.EscapeUriString(Game.ModData.Manifest.Mod.Id),
				Uri.EscapeUriString(Game.ModData.Manifest.Mod.Version),
				Uri.EscapeUriString(TestWritePermissions().ToString()));
		}

		byte TestWritePermissions()
		{
			try
			{
				using (var fs = File.Create(Path.Combine(".", "mods", ".testwritable"), 1, FileOptions.DeleteOnClose))
					return 1;
			}
			catch
			{
				return 0;
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

		void NewsDownloadComplete(AsyncCompletedEventArgs e, string cacheFile, NewsItem[] oldNews, Action onNewsDownloaded)
		{
			Game.RunAfterTick(() => // run on the main thread
			{
				if (e.Error != null)
				{
					SetNewsStatus("Failed to retrieve news: {0}".F(Download.FormatErrorMessage(e.Error)));
					return;
				}

				fetchedNews = true;
				var newNews = ParseNews(cacheFile);
				if (newNews == null)
					return;

				DisplayNews(newNews);

				if (oldNews == null || newNews.Any(n => !oldNews.Select(c => c.DateTime).Contains(n.DateTime)))
					onNewsDownloaded();
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

		void OpenSkirmishLobbyPanel()
		{
			SwitchMenu(MenuType.None);
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onExit", () => { Game.Disconnect(); SwitchMenu(MenuType.Singleplayer); } },
				{ "onStart", RemoveShellmapUI },
				{ "skirmishMode", true }
			});
		}

		void StartSkirmishGame()
		{
			var map = Game.ModData.MapCache.ChooseInitialMap(Game.Settings.Server.Map, Game.CosmeticRandom);
			Game.Settings.Server.Map = map;
			Game.Settings.Save();

			ConnectionLogic.Connect(IPAddress.Loopback.ToString(),
				Game.CreateLocalServer(map),
				"",
				OpenSkirmishLobbyPanel,
				() => { Game.CloseServer(); SwitchMenu(MenuType.Main); });
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Game.OnRemoteDirectConnect -= OnRemoteDirectConnect;
			base.Dispose(disposing);
		}
	}
}
