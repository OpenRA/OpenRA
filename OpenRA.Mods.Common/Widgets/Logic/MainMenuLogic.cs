#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MainMenuLogic
	{
		protected enum MenuType { Main, Singleplayer, Extras, MapEditor, None }

		protected MenuType menuType = MenuType.Main;
		readonly Widget rootMenu;
		readonly ScrollPanelWidget newsPanel;
		readonly Widget newsTemplate;
		readonly LabelWidget newsStatus;

		[ObjectCreator.UseCtor]
		public MainMenuLogic(Widget widget, World world)
		{
			rootMenu = widget;
			rootMenu.Get<LabelWidget>("VERSION_LABEL").Text = Game.ModData.Manifest.Mod.Version;

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
					{ "onExit", () => menuType = MenuType.Main },
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
					Game.Settings.Game.PreviousMod = Game.ModData.Manifest.Mod.Id;
					Game.InitializeMod("modchooser", null);
				});
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

			CampaignProgress.Init(world.Players);

			var factionList = new List<string>();
			var missionPlayed = false;

			foreach (var p in world.Players)
			{
				var faction = p.Faction.Name;
				if (!factionList.Contains(faction))
				{
					factionList.Add(faction);
					missionPlayed = CampaignProgress.GetMission(faction).Length != 0;
				}

				if (missionPlayed)
				{
					break;
				}
			}

			var campaignButton = singleplayerMenu.GetOrNull<ButtonWidget>("CAMPAIGN_BUTTON");
			if (campaignButton != null)
			{
				if (CampaignProgress.Factions.Count == 0)
					campaignButton.Disabled = true;

				campaignButton.OnClick = () =>
				{
					CampaignProgress.SetSaveProgressFlag();
					menuType = MenuType.None;
					if (!missionPlayed)
					{
						Game.OpenWindow("CAMPAIGN_FACTION", new WidgetArgs
						{
							{ "onExit", () => {
								menuType = MenuType.Singleplayer;
								CampaignProgress.ResetSaveProgressFlag();
							}
							},
							{ "onStart", RemoveShellmapUI }
						});
					}
					else
					{
						Game.OpenWindow("CAMPAIGN_MENU", new WidgetArgs
						{
							{ "onExit", () => {
								menuType = MenuType.Singleplayer;
								CampaignProgress.ResetSaveProgressFlag();
							}
							},
							{ "onStart", RemoveShellmapUI }
						});
					}
				};

				if (CampaignProgress.GetSaveProgressFlag() && missionPlayed)
				{
					menuType = MenuType.None;
					var campaignMenu = Game.OpenWindow("CAMPAIGN_MENU", new WidgetArgs
						{
							{ "onExit", () => menuType = MenuType.Singleplayer },
							{ "onStart", RemoveShellmapUI }
						});
					Game.OpenWindow("CAMPAIGN_WORLD", new WidgetArgs
					{
						{ "onExit", () => campaignMenu.Visible = true },
						{ "onStart", RemoveShellmapUI }
					});
				}
			}

			var missionsButton = singleplayerMenu.Get<ButtonWidget>("MISSIONS_BUTTON");
			missionsButton.OnClick = () =>
			{
				menuType = MenuType.None;
				Game.OpenWindow("MISSIONBROWSER_PANEL", new WidgetArgs
				{
					{ "onExit", () => menuType = MenuType.Singleplayer },
					{ "onStart", RemoveShellmapUI }
				});
			};

			var hasCampaign = Game.ModData.Manifest.Missions.Any();
			var hasMissions = Game.ModData.MapCache
				.Any(p => p.Status == MapStatus.Available && p.Map.Visibility.HasFlag(MapVisibility.MissionSelector));

			missionsButton.Disabled = !hasCampaign && !hasMissions;

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
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs
				{
					{ "onExit", () => menuType = MenuType.Extras },
					{ "world", world }
				});
			};

			extrasMenu.Get<ButtonWidget>("MAP_EDITOR_BUTTON").OnClick = () => menuType = MenuType.MapEditor;

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

			// Map editor menu
			var mapEditorMenu = widget.Get("MAP_EDITOR_MENU");
			mapEditorMenu.IsVisible = () => menuType == MenuType.MapEditor;

			var onSelect = new Action<string>(uid =>
			{
				RemoveShellmapUI();
				LoadMapIntoEditor(Game.ModData.MapCache[uid].Map);
			});

			var newMapButton = widget.Get<ButtonWidget>("NEW_MAP_BUTTON");
			newMapButton.OnClick = () =>
			{
				menuType = MenuType.None;
				Game.OpenWindow("NEW_MAP_BG", new WidgetArgs()
				{
					{ "onSelect", onSelect },
					{ "onExit", () => menuType = MenuType.MapEditor }
				});
			};

			var loadMapButton = widget.Get<ButtonWidget>("LOAD_MAP_BUTTON");
			loadMapButton.OnClick = () =>
			{
				menuType = MenuType.None;
				Game.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
				{
					{ "initialMap", null },
					{ "initialTab", MapClassification.User },
					{ "onExit", () => menuType = MenuType.MapEditor },
					{ "onSelect", onSelect },
					{ "filter", MapVisibility.Lobby | MapVisibility.Shellmap | MapVisibility.MissionSelector },
				});
			};

			mapEditorMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => menuType = MenuType.Extras;

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
					// Only query for news once per day.
					var cacheValid = currentNews != null && DateTime.Today.ToUniversalTime() <= Game.Settings.Game.NewsFetchedDate;
					if (!cacheValid)
						new Download(Game.Settings.Game.NewsUrl, cacheFile, e => { },
							(e, c) => NewsDownloadComplete(e, cacheFile, currentNews, () => newsButton.AttachPanel(newsPanel)));

					newsButton.OnClick = () => newsButton.AttachPanel(newsPanel);
				}
			}

			Game.OnRemoteDirectConnect += (host, port) =>
			{
				menuType = MenuType.None;
				Ui.OpenWindow("SERVERBROWSER_PANEL", new WidgetArgs
				{
					{ "onStart", RemoveShellmapUI },
					{ "onExit", () => menuType = MenuType.Main },
					{ "directConnectHost", host },
					{ "directConnectPort", port },
				});
			};
		}

		void LoadMapIntoEditor(Map map)
		{
			ConnectionLogic.Connect(System.Net.IPAddress.Loopback.ToString(),
				Game.CreateLocalServer(map.Uid),
				"",
				() => { Game.LoadEditor(map.Uid); },
				() => { Game.CloseServer(); menuType = MenuType.MapEditor; });
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

				var newNews = ParseNews(cacheFile);
				if (newNews == null)
					return;

				DisplayNews(newNews);

				if (oldNews == null || newNews.Any(n => !oldNews.Select(c => c.DateTime).Contains(n.DateTime)))
					onNewsDownloaded();

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

		void OpenSkirmishLobbyPanel()
		{
			menuType = MenuType.None;
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onExit", () => { Game.Disconnect(); menuType = MenuType.Singleplayer; } },
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
