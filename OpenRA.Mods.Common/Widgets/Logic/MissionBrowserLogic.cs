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
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MissionBrowserLogic : ChromeLogic
	{
		enum PlayingVideo { None, Info, Briefing, GameStart }

		readonly ModData modData;
		readonly Action onStart;
		readonly ScrollPanelWidget descriptionPanel;
		readonly LabelWidget description;
		readonly SpriteFont descriptionFont;
		readonly DropDownButtonWidget difficultyButton;
		readonly DropDownButtonWidget gameSpeedButton;
		readonly ButtonWidget startBriefingVideoButton;
		readonly ButtonWidget stopBriefingVideoButton;
		readonly ButtonWidget startInfoVideoButton;
		readonly ButtonWidget stopInfoVideoButton;
		readonly VqaPlayerWidget videoPlayer;
		readonly BackgroundWidget fullscreenVideoPlayer;

		readonly ScrollPanelWidget missionList;
		readonly ScrollItemWidget headerTemplate;
		readonly ScrollItemWidget template;
		readonly Cache<MapPreview, Map> mapCache;

		MapPreview selectedMapPreview;
		Map selectedMap;

		PlayingVideo playingVideo;

		string difficulty;
		string gameSpeed;

		[ObjectCreator.UseCtor]
		public MissionBrowserLogic(Widget widget, ModData modData, World world, Action onStart, Action onExit)
		{
			mapCache = new Cache<MapPreview, Map>(p => new Map(modData, p.Package));
			this.modData = modData;
			this.onStart = onStart;

			missionList = widget.Get<ScrollPanelWidget>("MISSION_LIST");

			headerTemplate = widget.Get<ScrollItemWidget>("HEADER");
			template = widget.Get<ScrollItemWidget>("TEMPLATE");

			var title = widget.GetOrNull<LabelWidget>("MISSIONBROWSER_TITLE");
			if (title != null)
				title.GetText = () => playingVideo != PlayingVideo.None ? selectedMapPreview.Title : title.Text;

			widget.Get("MISSION_INFO").IsVisible = () => selectedMapPreview != null;

			var previewWidget = widget.Get<MapPreviewWidget>("MISSION_PREVIEW");
			previewWidget.Preview = () => selectedMapPreview;
			previewWidget.IsVisible = () => playingVideo == PlayingVideo.None;

			videoPlayer = widget.Get<VqaPlayerWidget>("MISSION_VIDEO");
			widget.Get("MISSION_BIN").IsVisible = () => playingVideo != PlayingVideo.None;
			fullscreenVideoPlayer = Ui.LoadWidget<BackgroundWidget>("FULLSCREEN_PLAYER", Ui.Root, new WidgetArgs { { "world", world } });

			descriptionPanel = widget.Get<ScrollPanelWidget>("MISSION_DESCRIPTION_PANEL");

			description = descriptionPanel.Get<LabelWidget>("MISSION_DESCRIPTION");
			descriptionFont = Game.Renderer.Fonts[description.Font];

			difficultyButton = widget.Get<DropDownButtonWidget>("DIFFICULTY_DROPDOWNBUTTON");
			gameSpeedButton = widget.GetOrNull<DropDownButtonWidget>("GAMESPEED_DROPDOWNBUTTON");

			startBriefingVideoButton = widget.Get<ButtonWidget>("START_BRIEFING_VIDEO_BUTTON");
			stopBriefingVideoButton = widget.Get<ButtonWidget>("STOP_BRIEFING_VIDEO_BUTTON");
			stopBriefingVideoButton.IsVisible = () => playingVideo == PlayingVideo.Briefing;
			stopBriefingVideoButton.OnClick = () => StopVideo(videoPlayer);

			startInfoVideoButton = widget.Get<ButtonWidget>("START_INFO_VIDEO_BUTTON");
			stopInfoVideoButton = widget.Get<ButtonWidget>("STOP_INFO_VIDEO_BUTTON");
			stopInfoVideoButton.IsVisible = () => playingVideo == PlayingVideo.Info;
			stopInfoVideoButton.OnClick = () => StopVideo(videoPlayer);

			var allPreviews = new List<MapPreview>();
			missionList.RemoveChildren();

			// Add a group for each campaign
			if (modData.Manifest.Missions.Any())
			{
				var yaml = MiniYaml.Merge(modData.Manifest.Missions.Select(
					m => MiniYaml.FromStream(modData.DefaultFileSystem.Open(m))));

				foreach (var kv in yaml)
				{
					var missionMapPaths = kv.Value.Nodes.Select(n => Path.GetFullPath(n.Key)).ToList();

					var previews = modData.MapCache
						.Where(p => p.Status == MapStatus.Available && missionMapPaths.Contains(p.Package.Name))
						.OrderBy(p => missionMapPaths.IndexOf(p.Package.Name));

					CreateMissionGroup(kv.Key, previews);
					allPreviews.AddRange(previews);
				}
			}

			// Add an additional group for loose missions
			var loosePreviews = modData.MapCache
				.Where(p => p.Status == MapStatus.Available && p.Visibility.HasFlag(MapVisibility.MissionSelector) && !allPreviews.Any(a => a.Uid == p.Uid));

			if (loosePreviews.Any())
			{
				CreateMissionGroup("Missions", loosePreviews);
				allPreviews.AddRange(loosePreviews);
			}

			if (allPreviews.Any())
				SelectMap(allPreviews.First());

			// Preload map and preview data to reduce jank
			new Thread(() =>
			{
				foreach (var p in allPreviews)
					modData.MapCache[mapCache[p].Uid].GetMinimap();
			}).Start();

			var startButton = widget.Get<ButtonWidget>("STARTGAME_BUTTON");
			startButton.OnClick = StartMissionClicked;
			startButton.IsDisabled = () => selectedMap == null || selectedMap.InvalidCustomRules;

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				StopVideo(videoPlayer);
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};
		}

		void CreateMissionGroup(string title, IEnumerable<MapPreview> previews)
		{
			var header = ScrollItemWidget.Setup(headerTemplate, () => true, () => { });
			header.Get<LabelWidget>("LABEL").GetText = () => title;
			missionList.AddChild(header);

			foreach (var p in previews)
			{
				var preview = p;

				var item = ScrollItemWidget.Setup(template,
					() => selectedMapPreview != null && selectedMapPreview.Uid == preview.Uid,
					() => SelectMap(preview),
					StartMissionClicked);

				item.Get<LabelWidget>("TITLE").GetText = () => preview.Title;
				missionList.AddChild(item);
			}
		}

		void SelectMap(MapPreview preview)
		{
			selectedMap = mapCache[preview];
			selectedMapPreview = preview;

			// Cache the rules on a background thread to avoid jank
			var difficultyDisabled = true;
			var difficulties = new string[0];

			new Thread(() =>
			{
				selectedMap.PreloadRules();
				var mapOptions = selectedMap.Rules.Actors["world"].TraitInfo<MapOptionsInfo>();

				difficulty = mapOptions.Difficulty ?? mapOptions.Difficulties.FirstOrDefault();
				difficulties = mapOptions.Difficulties;
				difficultyDisabled = mapOptions.DifficultyLocked || mapOptions.Difficulties.Length <= 1;
			}).Start();

			var briefingVideo = selectedMap.Videos.Briefing;
			var briefingVideoVisible = briefingVideo != null;
			var briefingVideoDisabled = !(briefingVideoVisible && modData.DefaultFileSystem.Exists(briefingVideo));

			var infoVideo = selectedMap.Videos.BackgroundInfo;
			var infoVideoVisible = infoVideo != null;
			var infoVideoDisabled = !(infoVideoVisible && modData.DefaultFileSystem.Exists(infoVideo));

			startBriefingVideoButton.IsVisible = () => briefingVideoVisible && playingVideo != PlayingVideo.Briefing;
			startBriefingVideoButton.IsDisabled = () => briefingVideoDisabled || playingVideo != PlayingVideo.None;
			startBriefingVideoButton.OnClick = () => PlayVideo(videoPlayer, briefingVideo, PlayingVideo.Briefing, () => StopVideo(videoPlayer));

			startInfoVideoButton.IsVisible = () => infoVideoVisible && playingVideo != PlayingVideo.Info;
			startInfoVideoButton.IsDisabled = () => infoVideoDisabled || playingVideo != PlayingVideo.None;
			startInfoVideoButton.OnClick = () => PlayVideo(videoPlayer, infoVideo, PlayingVideo.Info, () => StopVideo(videoPlayer));

			var text = selectedMap.Description != null ? selectedMap.Description.Replace("\\n", "\n") : "";
			text = WidgetUtils.WrapText(text, description.Bounds.Width, descriptionFont);
			description.Text = text;
			description.Bounds.Height = descriptionFont.Measure(text).Y;
			descriptionPanel.ScrollToTop();
			descriptionPanel.Layout.AdjustChildren();

			if (difficultyButton != null)
			{
				difficultyButton.IsDisabled = () => difficultyDisabled;
				difficultyButton.GetText = () => difficulty ?? "Normal";
				difficultyButton.OnMouseDown = _ =>
				{
					var options = difficulties.Select(d => new DropDownOption
					{
						Title = d,
						IsSelected = () => difficulty == d,
						OnClick = () => difficulty = d
					});

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};

					difficultyButton.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
				};
			}

			if (gameSpeedButton != null)
			{
				var speeds = modData.Manifest.Get<GameSpeeds>().Speeds;
				gameSpeed = "default";

				gameSpeedButton.GetText = () => speeds[gameSpeed].Name;
				gameSpeedButton.OnMouseDown = _ =>
				{
					var options = speeds.Select(s => new DropDownOption
					{
						Title = s.Value.Name,
						IsSelected = () => gameSpeed == s.Key,
						OnClick = () => gameSpeed = s.Key
					});

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};

					gameSpeedButton.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
				};
			}
		}

		float cachedSoundVolume;
		float cachedMusicVolume;
		void MuteSounds()
		{
			cachedSoundVolume = Game.Sound.SoundVolume;
			cachedMusicVolume = Game.Sound.MusicVolume;
			Game.Sound.SoundVolume = Game.Sound.MusicVolume = 0;
		}

		void UnMuteSounds()
		{
			if (cachedSoundVolume > 0)
				Game.Sound.SoundVolume = cachedSoundVolume;

			if (cachedMusicVolume > 0)
				Game.Sound.MusicVolume = cachedMusicVolume;
		}

		void PlayVideo(VqaPlayerWidget player, string video, PlayingVideo pv, Action onComplete)
		{
			StopVideo(player);

			playingVideo = pv;
			player.Load(video);

			// video playback runs asynchronously
			player.PlayThen(onComplete);

			// Mute other distracting sounds
			MuteSounds();
		}

		void StopVideo(VqaPlayerWidget player)
		{
			if (playingVideo == PlayingVideo.None)
				return;

			UnMuteSounds();
			player.Stop();
			playingVideo = PlayingVideo.None;
		}

		void StartMissionClicked()
		{
			StopVideo(videoPlayer);

			if (selectedMap.InvalidCustomRules)
				return;

			var gameStartVideo = selectedMap.Videos.GameStart;
			var orders = new[] {
				Order.Command("gamespeed {0}".F(gameSpeed)),
				Order.Command("difficulty {0}".F(difficulty)),
				Order.Command("state {0}".F(Session.ClientState.Ready))
			};

			if (gameStartVideo != null && modData.DefaultFileSystem.Exists(gameStartVideo))
			{
				var fsPlayer = fullscreenVideoPlayer.Get<VqaPlayerWidget>("PLAYER");
				fullscreenVideoPlayer.Visible = true;
				PlayVideo(fsPlayer, gameStartVideo, PlayingVideo.GameStart, () =>
				{
					StopVideo(fsPlayer);
					Game.CreateAndStartLocalServer(selectedMapPreview.Uid, orders, onStart);
				});
			}
			else
				Game.CreateAndStartLocalServer(selectedMapPreview.Uid, orders, onStart);
		}

		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
