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
using System.Threading;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MissionBrowserLogic
	{
		enum PlayingVideo { None, Info, Briefing, GameStart }

		readonly Action onStart;
		readonly ScrollPanelWidget descriptionPanel;
		readonly LabelWidget description;
		readonly SpriteFont descriptionFont;
		readonly DropDownButtonWidget difficultyButton;
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
			if (Game.modData.Manifest.Missions.Any())
			{
				var yaml = Game.modData.Manifest.Missions.Select(MiniYaml.FromFile).Aggregate(MiniYaml.MergeLiberal);

				foreach (var kv in yaml)
				{
					var missionMapPaths = kv.Value.Nodes.Select(n => Path.GetFullPath(n.Key));

					var maps = Game.modData.MapCache
						.Where(p => p.Status == MapStatus.Available && missionMapPaths.Contains(Path.GetFullPath(p.Map.Path)))
						.Select(p => p.Map);

					CreateMissionGroup(kv.Key, maps);
					allMaps.AddRange(maps);
				}
			}

			// Add an additional group for loose missions
			var looseMissions = Game.modData.MapCache
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
			selectedMapPreview = Game.modData.MapCache[map.Uid];

			// Cache the rules on a background thread to avoid jank
			new Thread(selectedMapPreview.CacheRules).Start();

			var briefingVideo = selectedMapPreview.Map.Videos.Briefing;
			var briefingVideoVisible = briefingVideo != null;
			var briefingVideoDisabled = !(briefingVideoVisible && GlobalFileSystem.Exists(briefingVideo));

			var infoVideo = selectedMapPreview.Map.Videos.BackgroundInfo;
			var infoVideoVisible = infoVideo != null;
			var infoVideoDisabled = !(infoVideoVisible && GlobalFileSystem.Exists(infoVideo));

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

			difficultyButton.IsVisible = () => map.Options.Difficulties.Any();
			if (!map.Options.Difficulties.Any())
				return;

			difficulty = map.Options.Difficulties.First();
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

		float cachedSoundVolume;
		float cachedMusicVolume;
		void MuteSounds()
		{
			cachedSoundVolume = Sound.SoundVolume;
			cachedMusicVolume = Sound.MusicVolume;
			Sound.SoundVolume = Sound.MusicVolume = 0;
		}

		void UnMuteSounds()
		{
			if (cachedSoundVolume > 0)
				Sound.SoundVolume = cachedSoundVolume;

			if (cachedMusicVolume > 0)
				Sound.MusicVolume = cachedMusicVolume;
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
			if (gameStartVideo != null && GlobalFileSystem.Exists(gameStartVideo))
			{
				var fsPlayer = fullscreenVideoPlayer.Get<VqaPlayerWidget>("PLAYER");
				fullscreenVideoPlayer.Visible = true;
				PlayVideo(fsPlayer, gameStartVideo, PlayingVideo.GameStart, () =>
				{
					StopVideo(fsPlayer);
					StartMission();
				});
			}
			else
				StartMission();
		}

		void StartMission()
		{
			OrderManager om = null;

			Action lobbyReady = null;
			lobbyReady = () =>
			{
				om.IssueOrder(Order.Command("difficulty {0}".F(difficulty)));
				Game.LobbyInfoChanged -= lobbyReady;
				onStart();
				om.IssueOrder(Order.Command("state {0}".F(Session.ClientState.Ready)));
			};
			Game.LobbyInfoChanged += lobbyReady;

			om = Game.JoinServer(IPAddress.Loopback.ToString(), Game.CreateLocalServer(selectedMapPreview.Uid), "", false);
		}

		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
