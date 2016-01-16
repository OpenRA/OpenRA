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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MissionBrowserLogic : ChromeLogic
	{
		enum PlayingVideo { None, Info, Briefing, GameStart }

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

		MapPreview selectedMapPreview;

		PlayingVideo playingVideo;

		string difficulty;
		string gameSpeed;

		[ObjectCreator.UseCtor]
		public MissionBrowserLogic(Widget widget, World world, Action onStart, Action onExit)
		{
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

			var allMaps = new List<Map>();
			missionList.RemoveChildren();

			// Add a group for each campaign
			if (Game.ModData.Manifest.Missions.Any())
			{
				var partial = Game.ModData.Manifest.Missions
					.Select(MiniYaml.FromFile)
					.Aggregate(MiniYaml.MergePartial);

				var yaml = MiniYaml.ApplyRemovals(partial);
				foreach (var kv in yaml)
				{
					var missionMapPaths = kv.Value.Nodes.Select(n => Path.GetFullPath(n.Key)).ToList();

					var maps = Game.ModData.MapCache
						.Where(p => p.Status == MapStatus.Available && missionMapPaths.Contains(Path.GetFullPath(p.Map.Path)))
						.Select(p => p.Map)
						.OrderBy(m => missionMapPaths.IndexOf(Path.GetFullPath(m.Path)));

					CreateMissionGroup(kv.Key, maps);
					allMaps.AddRange(maps);
				}
			}

			// Add an additional group for loose missions
			var looseMissions = Game.ModData.MapCache
				.Where(p => p.Status == MapStatus.Available && p.Map.Visibility.HasFlag(MapVisibility.MissionSelector) && !allMaps.Contains(p.Map))
				.Select(p => p.Map);

			if (looseMissions.Any())
			{
				CreateMissionGroup("Missions", looseMissions);
				allMaps.AddRange(looseMissions);
			}

			if (allMaps.Any())
				SelectMap(allMaps.First());

			var startButton = widget.Get<ButtonWidget>("STARTGAME_BUTTON");
			startButton.OnClick = StartMissionClicked;
			startButton.IsDisabled = () => selectedMapPreview == null || selectedMapPreview.RuleStatus != MapRuleStatus.Cached;

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				StopVideo(videoPlayer);
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};
		}

		void CreateMissionGroup(string title, IEnumerable<Map> maps)
		{
			var header = ScrollItemWidget.Setup(headerTemplate, () => true, () => { });
			header.Get<LabelWidget>("LABEL").GetText = () => title;
			missionList.AddChild(header);

			foreach (var m in maps)
			{
				var map = m;

				var item = ScrollItemWidget.Setup(template,
					() => selectedMapPreview != null && selectedMapPreview.Uid == map.Uid,
					() => SelectMap(map),
					StartMissionClicked);

				item.Get<LabelWidget>("TITLE").GetText = () => map.Title;
				missionList.AddChild(item);
			}
		}

		void SelectMap(Map map)
		{
			selectedMapPreview = Game.ModData.MapCache[map.Uid];

			// Cache the rules on a background thread to avoid jank
			new Thread(selectedMapPreview.CacheRules).Start();

			var briefingVideo = selectedMapPreview.Map.Videos.Briefing;
			var briefingVideoVisible = briefingVideo != null;
			var briefingVideoDisabled = !(briefingVideoVisible && Game.ModData.ModFiles.Exists(briefingVideo));

			var infoVideo = selectedMapPreview.Map.Videos.BackgroundInfo;
			var infoVideoVisible = infoVideo != null;
			var infoVideoDisabled = !(infoVideoVisible && Game.ModData.ModFiles.Exists(infoVideo));

			startBriefingVideoButton.IsVisible = () => briefingVideoVisible && playingVideo != PlayingVideo.Briefing;
			startBriefingVideoButton.IsDisabled = () => briefingVideoDisabled || playingVideo != PlayingVideo.None;
			startBriefingVideoButton.OnClick = () => PlayVideo(videoPlayer, briefingVideo, PlayingVideo.Briefing, () => StopVideo(videoPlayer));

			startInfoVideoButton.IsVisible = () => infoVideoVisible && playingVideo != PlayingVideo.Info;
			startInfoVideoButton.IsDisabled = () => infoVideoDisabled || playingVideo != PlayingVideo.None;
			startInfoVideoButton.OnClick = () => PlayVideo(videoPlayer, infoVideo, PlayingVideo.Info, () => StopVideo(videoPlayer));

			var text = map.Description != null ? map.Description.Replace("\\n", "\n") : "";
			text = WidgetUtils.WrapText(text, description.Bounds.Width, descriptionFont);
			description.Text = text;
			description.Bounds.Height = descriptionFont.Measure(text).Y;
			descriptionPanel.ScrollToTop();
			descriptionPanel.Layout.AdjustChildren();

			if (difficultyButton != null)
			{
				difficultyButton.IsDisabled = () => !map.Options.Difficulties.Any();

				difficulty = map.Options.Difficulties.FirstOrDefault();
				difficultyButton.GetText = () => difficulty ?? "Normal";
				difficultyButton.OnMouseDown = _ =>
				{
					var options = map.Options.Difficulties.Select(d => new DropDownOption
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
				var speeds = Game.ModData.Manifest.Get<GameSpeeds>().Speeds;
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

			if (selectedMapPreview.RuleStatus != MapRuleStatus.Cached)
				return;

			var gameStartVideo = selectedMapPreview.Map.Videos.GameStart;
			if (gameStartVideo != null && Game.ModData.ModFiles.Exists(gameStartVideo))
			{
				var fsPlayer = fullscreenVideoPlayer.Get<VqaPlayerWidget>("PLAYER");
				fullscreenVideoPlayer.Visible = true;
				PlayVideo(fsPlayer, gameStartVideo, PlayingVideo.GameStart, () =>
				{
					StopVideo(fsPlayer);
					Game.StartMission(selectedMapPreview.Uid, gameSpeed, difficulty, onStart);
				});
			}
			else
				Game.StartMission(selectedMapPreview.Uid, gameSpeed, difficulty, onStart);
		}

		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
