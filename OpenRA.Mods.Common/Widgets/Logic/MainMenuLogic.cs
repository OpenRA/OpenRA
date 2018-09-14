#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1203:ConstantsMustAppearBeforeFields",
		Justification = "SystemInformation version should be defined next to the dictionary it refers to.")]
	public class MainMenuLogic : ChromeLogic
	{
		protected enum MenuType { Main, Singleplayer, Extras, MapEditor, SystemInfoPrompt, None }

		protected enum MenuPanel { None, Missions, Skirmish, Multiplayer, MapEditor, Replays }

		protected MenuType menuType = MenuType.Main;
		readonly Widget rootMenu;
		readonly ScrollPanelWidget newsPanel;
		readonly Widget newsTemplate;
		readonly LabelWidget newsStatus;

		// Update news once per game launch
		static bool fetchedNews;

		protected static MenuPanel lastGameState = MenuPanel.None;

		bool newsOpen;

		// Increment the version number when adding new stats
		const int SystemInformationVersion = 3;
		Dictionary<string, Pair<string, string>> GetSystemInformation()
		{
			var lang = System.Globalization.CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
			return new Dictionary<string, Pair<string, string>>()
			{
				{ "id", Pair.New("Anonymous ID", Game.Settings.Debug.UUID) },
				{ "platform", Pair.New("OS Type", Platform.CurrentPlatform.ToString()) },
				{ "os", Pair.New("OS Version", Environment.OSVersion.ToString()) },
				{ "x64", Pair.New("OS is 64 bit", Environment.Is64BitOperatingSystem.ToString()) },
				{ "x64process", Pair.New("Process is 64 bit", Environment.Is64BitProcess.ToString()) },
				{ "runtime", Pair.New(".NET Runtime", Platform.RuntimeVersion) },
				{ "gl", Pair.New("OpenGL Version", Game.Renderer.GLVersion) },
				{ "windowsize", Pair.New("Window Size", "{0}x{1}".F(Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height)) },
				{ "windowscale", Pair.New("Window Scale", Game.Renderer.WindowScale.ToString("F2", CultureInfo.InvariantCulture)) },
				{ "lang", Pair.New("System Language", lang) }
			};
		}

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
			rootMenu.Get<LabelWidget>("VERSION_LABEL").Text = modData.Manifest.Metadata.Version;

			// Menu buttons
			var mainMenu = widget.Get("MAIN_MENU");
			mainMenu.IsVisible = () => menuType == MenuType.Main;

			mainMenu.Get<ButtonWidget>("SINGLEPLAYER_BUTTON").OnClick = () => SwitchMenu(MenuType.Singleplayer);

			mainMenu.Get<ButtonWidget>("MULTIPLAYER_BUTTON").OnClick = OpenMultiplayerPanel;

			mainMenu.Get<ButtonWidget>("CONTENT_BUTTON").OnClick = () =>
			{
				// Switching mods changes the world state (by disposing it),
				// so we can't do this inside the input handler.
				Game.RunAfterTick(() =>
				{
					var content = modData.Manifest.Get<ModContent>();
					Game.InitializeMod(content.ContentInstallerMod, new Arguments(new[] { "Content.Mod=" + modData.Manifest.Id }));
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
			missionsButton.OnClick = OpenMissionBrowserPanel;

			var hasCampaign = modData.Manifest.Missions.Any();
			var hasMissions = modData.MapCache
				.Any(p => p.Status == MapStatus.Available && p.Visibility.HasFlag(MapVisibility.MissionSelector));

			missionsButton.Disabled = !hasCampaign && !hasMissions;

			var hasMaps = modData.MapCache.Any(p => p.Visibility.HasFlag(MapVisibility.Lobby));
			var skirmishButton = singleplayerMenu.Get<ButtonWidget>("SKIRMISH_BUTTON");
			skirmishButton.OnClick = StartSkirmishGame;
			skirmishButton.Disabled = !hasMaps;

			singleplayerMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => SwitchMenu(MenuType.Main);

			// Extras menu
			var extrasMenu = widget.Get("EXTRAS_MENU");
			extrasMenu.IsVisible = () => menuType == MenuType.Extras;

			extrasMenu.Get<ButtonWidget>("REPLAYS_BUTTON").OnClick = OpenReplayBrowserPanel;

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

			// Loading into the map editor
			Game.BeforeGameStart += RemoveShellmapUI;

			var onSelect = new Action<string>(uid => LoadMapIntoEditor(modData.MapCache[uid].Uid));

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

			loadMapButton.Disabled = !hasMaps;

			mapEditorMenu.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => SwitchMenu(MenuType.Extras);

			var newsBG = widget.GetOrNull("NEWS_BG");
			if (newsBG != null)
			{
				newsBG.IsVisible = () => Game.Settings.Game.FetchNews && menuType != MenuType.None && menuType != MenuType.SystemInfoPrompt;

				newsPanel = Ui.LoadWidget<ScrollPanelWidget>("NEWS_PANEL", null, new WidgetArgs());
				newsTemplate = newsPanel.Get("NEWS_ITEM_TEMPLATE");
				newsPanel.RemoveChild(newsTemplate);

				newsStatus = newsPanel.Get<LabelWidget>("NEWS_STATUS");
				SetNewsStatus("Loading news");
			}

			Game.OnRemoteDirectConnect += OnRemoteDirectConnect;

			// Check for updates in the background
			var webServices = modData.Manifest.Get<WebServices>();
			if (Game.Settings.Debug.CheckVersion)
				webServices.CheckModVersion();

			var updateLabel = rootMenu.GetOrNull("UPDATE_NOTICE");
			if (updateLabel != null)
				updateLabel.IsVisible = () => !newsOpen && menuType != MenuType.None &&
					menuType != MenuType.SystemInfoPrompt &&
					webServices.ModVersionStatus == ModVersionStatus.Outdated;

			var playerProfile = widget.GetOrNull("PLAYER_PROFILE_CONTAINER");
			if (playerProfile != null)
			{
				Func<bool> minimalProfile = () => Ui.CurrentWindow() != null;
				Game.LoadWidget(world, "LOCAL_PROFILE_PANEL", playerProfile, new WidgetArgs()
				{
					{ "minimalProfile", minimalProfile }
				});
			}

			// System information opt-out prompt
			var sysInfoPrompt = widget.Get("SYSTEM_INFO_PROMPT");
			sysInfoPrompt.IsVisible = () => menuType == MenuType.SystemInfoPrompt;
			if (Game.Settings.Debug.SystemInformationVersionPrompt < SystemInformationVersion)
			{
				menuType = MenuType.SystemInfoPrompt;

				var sysInfoCheckbox = sysInfoPrompt.Get<CheckboxWidget>("SYSINFO_CHECKBOX");
				sysInfoCheckbox.IsChecked = () => Game.Settings.Debug.SendSystemInformation;
				sysInfoCheckbox.OnClick = () => Game.Settings.Debug.SendSystemInformation ^= true;

				var sysInfoData = sysInfoPrompt.Get<ScrollPanelWidget>("SYSINFO_DATA");
				var template = sysInfoData.Get<LabelWidget>("DATA_TEMPLATE");
				sysInfoData.RemoveChildren();

				foreach (var info in GetSystemInformation().Values)
				{
					var label = template.Clone() as LabelWidget;
					var text = info.First + ": " + info.Second;
					label.GetText = () => text;
					sysInfoData.AddChild(label);
				}

				sysInfoPrompt.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
				{
					Game.Settings.Debug.SystemInformationVersionPrompt = SystemInformationVersion;
					Game.Settings.Save();
					SwitchMenu(MenuType.Main);
					LoadAndDisplayNews(webServices.GameNews, newsBG);
				};
			}
			else
				LoadAndDisplayNews(webServices.GameNews, newsBG);

			Game.OnShellmapLoaded += OpenMenuBasedOnLastGame;
		}

		void LoadAndDisplayNews(string newsURL, Widget newsBG)
		{
			if (newsBG != null && Game.Settings.Game.FetchNews)
			{
				var cacheFile = Platform.ResolvePath(Platform.SupportDirPrefix, "news.yaml");
				var currentNews = ParseNews(cacheFile);
				if (currentNews != null)
					DisplayNews(currentNews);

				var newsButton = newsBG.GetOrNull<DropDownButtonWidget>("NEWS_BUTTON");
				if (newsButton != null)
				{
					if (!fetchedNews)
					{
						// Send the mod and engine version to support version-filtered news (update prompts)
						newsURL += "?version={0}&mod={1}&modversion={2}".F(
							Uri.EscapeUriString(Game.EngineVersion),
							Uri.EscapeUriString(Game.ModData.Manifest.Id),
							Uri.EscapeUriString(Game.ModData.Manifest.Metadata.Version));

						// Append system profile data if the player has opted in
						if (Game.Settings.Debug.SendSystemInformation)
							newsURL += "&sysinfoversion={0}&".F(SystemInformationVersion)
								+ GetSystemInformation()
								.Select(kv => kv.Key + "=" + Uri.EscapeUriString(kv.Value.Second))
								.JoinWith("&");

						new Download(newsURL, cacheFile, e => { },
							e => NewsDownloadComplete(e, cacheFile, currentNews,
								() => OpenNewsPanel(newsButton)));
					}

					newsButton.OnClick = () => OpenNewsPanel(newsButton);
				}
			}
		}

		void OpenNewsPanel(DropDownButtonWidget button)
		{
			newsOpen = true;
			button.AttachPanel(newsPanel, () => newsOpen = false);
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

			lastGameState = MenuPanel.MapEditor;
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

		void OpenMissionBrowserPanel()
		{
			SwitchMenu(MenuType.None);
			Game.OpenWindow("MISSIONBROWSER_PANEL", new WidgetArgs
			{
				{ "onExit", () => SwitchMenu(MenuType.Singleplayer) },
				{ "onStart", () => { RemoveShellmapUI(); lastGameState = MenuPanel.Missions; } }
			});
		}

		void OpenSkirmishLobbyPanel()
		{
			SwitchMenu(MenuType.None);
			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onExit", () => { Game.Disconnect(); SwitchMenu(MenuType.Singleplayer); } },
				{ "onStart", () => { RemoveShellmapUI(); lastGameState = MenuPanel.Skirmish; } },
				{ "skirmishMode", true }
			});
		}

		void OpenMultiplayerPanel()
		{
			SwitchMenu(MenuType.None);
			Ui.OpenWindow("MULTIPLAYER_PANEL", new WidgetArgs
			{
				{ "onStart", () => { RemoveShellmapUI(); lastGameState = MenuPanel.Multiplayer; } },
				{ "onExit", () => SwitchMenu(MenuType.Main) },
				{ "directConnectHost", null },
				{ "directConnectPort", 0 },
			});
		}

		void OpenReplayBrowserPanel()
		{
			SwitchMenu(MenuType.None);
			Ui.OpenWindow("REPLAYBROWSER_PANEL", new WidgetArgs
			{
				{ "onExit", () => SwitchMenu(MenuType.Extras) },
				{ "onStart", () => { RemoveShellmapUI(); lastGameState = MenuPanel.Replays; } }
			});
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Game.OnRemoteDirectConnect -= OnRemoteDirectConnect;
				Game.BeforeGameStart -= RemoveShellmapUI;
			}

			Game.OnShellmapLoaded -= OpenMenuBasedOnLastGame;
			base.Dispose(disposing);
		}

		void OpenMenuBasedOnLastGame()
		{
			switch (lastGameState)
			{
				case MenuPanel.Missions:
					OpenMissionBrowserPanel();
					break;

				case MenuPanel.Replays:
					OpenReplayBrowserPanel();
					break;

				case MenuPanel.Skirmish:
					StartSkirmishGame();
					break;

				case MenuPanel.Multiplayer:
					OpenMultiplayerPanel();
					break;

				case MenuPanel.MapEditor:
					SwitchMenu(MenuType.MapEditor);
					break;
			}

			lastGameState = MenuPanel.None;
		}
	}
}
