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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MainMenuLogic
	{
		protected enum MenuType { Main, Singleplayer, Extras, None }

		protected MenuType menuType = MenuType.Main;
		Widget rootMenu;

		protected readonly Widget newsBG;
		readonly ScrollPanelWidget newsPanel;
		readonly Widget newsItemTemplate;
		readonly LabelWidget newsStatus;
		readonly ButtonWidget showNewsButton;
		bool newsExpanded = false;

		[ObjectCreator.UseCtor]
		public MainMenuLogic(Widget widget, World world)
		{
			rootMenu = widget;
			rootMenu.Get<LabelWidget>("VERSION_LABEL").Text = Game.modData.Manifest.Mod.Version;

			// Menu buttons
			var mainMenu = widget.Get("MAIN_MENU");
			mainMenu.IsVisible = () => menuType == MenuType.Main;

			mainMenu.Get<ButtonWidget>("SINGLEPLAYER_BUTTON").OnClick = () => menuType = MenuType.Singleplayer;

			mainMenu.Get<ButtonWidget>("MULTIPLAYER_BUTTON").OnClick = () =>
			{
				menuType = MenuType.None;
				Ui.OpenWindow("SERVERBROWSER_PANEL", new WidgetArgs
				{
					{ "onStart", RemoveShellmapUI },
					{ "onExit", () => menuType = MenuType.Main }
				});
			};

			mainMenu.Get<ButtonWidget>("MODS_BUTTON").OnClick = () =>
			{
				Game.Settings.Game.PreviousMod = Game.modData.Manifest.Mod.Id;
				Game.InitializeWithMod("modchooser", null);
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

			singleplayerMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => menuType = MenuType.Main;

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
				MusicPlayerLogic.OpenWindow(world, () => menuType = MenuType.Extras);
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

			newsBG = widget.GetOrNull("NEWS_BG");
			if (newsBG != null)
			{
				var collapsedNewsBG = widget.Get("COLLAPSED_NEWS_BG");

				if (!Game.Settings.Game.FetchNews)
					collapsedNewsBG.Visible = false;
				else
				{
					newsPanel = widget.Get<ScrollPanelWidget>("NEWS_PANEL");
					newsItemTemplate = widget.Get("NEWS_ITEM_TEMPLATE");
					newsStatus = widget.Get<LabelWidget>("NEWS_STATUS");
					showNewsButton = widget.Get<ButtonWidget>("SHOW_NEWS_BUTTON");

					newsPanel.RemoveChildren();

					newsBG.IsVisible = () => newsExpanded && menuType != MenuType.None;
					collapsedNewsBG.IsVisible = () => !newsExpanded && menuType != MenuType.None;

					newsBG.Get<DropDownButtonWidget>("HIDE_NEWS_BUTTON").OnMouseDown = mi => newsExpanded = false;
					collapsedNewsBG.Get<DropDownButtonWidget>("SHOW_NEWS_BUTTON").OnMouseDown = mi =>
					{
						showNewsButton.IsHighlighted = () => false;
						newsExpanded = true;
					};

					SetNewsStatus("Loading news");

					if (Game.modData.Manifest.NewsUrl != null)
					{
						var cacheFile = GetNewsCacheFile();
						var cacheValid = File.Exists(cacheFile) && DateTime.Today.ToUniversalTime() <= Game.Settings.Game.NewsFetchedDate;

						if (cacheValid)
							DisplayNews(ReadNews(File.ReadAllBytes(cacheFile)));
						else
							new Download(Game.modData.Manifest.NewsUrl, e => { }, NewsDownloadComplete);
					}
				}
			}
		}

		string GetNewsCacheFile()
		{
			var cacheDir = Path.Combine(Platform.SupportDir, "cache", Game.modData.Manifest.Mod.Id);
			Directory.CreateDirectory(cacheDir);
			return Path.Combine(cacheDir, "news.yaml");
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

		IEnumerable<NewsItem> ReadNews(byte[] bytes)
		{
			var str = Encoding.UTF8.GetString(bytes);
			return MiniYaml.FromString(str).Select(node => new NewsItem
			{
				Title = node.Value.NodesDict["Title"].Value,
				Author = node.Value.NodesDict["Author"].Value,
				DateTime = FieldLoader.GetValue<DateTime>("DateTime", node.Key),
				Content = node.Value.NodesDict["Content"].Value
			});
		}

		void DisplayNews(IEnumerable<NewsItem> newsItems)
		{
			newsPanel.RemoveChildren();
			SetNewsStatus("");

			foreach (var i in newsItems)
			{
				var item = i;

				var newsItem = newsItemTemplate.Clone();

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

		void NewsDownloadComplete(DownloadDataCompletedEventArgs e, bool cancelled)
		{
			Game.RunAfterTick(() => // run on the main thread
			{
				if (e.Error != null)
				{
					SetNewsStatus("Failed to retrieve news: {0}".F(Download.FormatErrorMessage(e.Error)));
					return;
				}

				IEnumerable<NewsItem> newNews;
				try
				{
					newNews = ReadNews(e.Result);
					DisplayNews(newNews);
				}
				catch (Exception ex)
				{
					SetNewsStatus("Failed to retrieve news: {0}".F(ex.Message));
					return;
				}

				Game.Settings.Game.NewsFetchedDate = DateTime.Today.ToUniversalTime();
				Game.Settings.Save();

				var cacheFile = GetNewsCacheFile();
				if (File.Exists(cacheFile))
				{
					var oldNews = ReadNews(File.ReadAllBytes(cacheFile));
					if (newNews.Any(n => !oldNews.Select(c => c.DateTime).Contains(n.DateTime)))
						showNewsButton.IsHighlighted = () => Game.LocalTick % 50 < 25;
				}
				else
					showNewsButton.IsHighlighted = () => Game.LocalTick % 50 < 25;

				File.WriteAllBytes(cacheFile, e.Result);
			});
		}

		void RemoveShellmapUI()
		{
			rootMenu.Parent.RemoveChild(rootMenu);
		}

		void OpenSkirmishLobbyPanel()
		{
			menuType = MenuType.None;
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onExit", () => { Game.Disconnect(); menuType = MenuType.Main; } },
				{ "onStart", RemoveShellmapUI },
				{ "skirmishMode", true }
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
	}
}
