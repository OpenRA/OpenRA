#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MissionBrowserLogic : ChromeLogic
	{
		enum PlayingVideo { None, Info, Briefing, GameStart }

		[TranslationReference]
		const string NoVideoTitle = "dialog-no-video.title";

		[TranslationReference]
		const string NoVideoPrompt = "dialog-no-video.prompt";

		[TranslationReference]
		const string NoVideoCancel = "dialog-no-video.cancel";

		[TranslationReference]
		const string CantPlayTitle = "dialog-cant-play-video.title";

		[TranslationReference]
		const string CantPlayPrompt = "dialog-cant-play-video.prompt";

		[TranslationReference]
		const string CantPlayCancel = "dialog-cant-play-video.cancel";

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
		readonly VideoPlayerWidget videoPlayer;
		readonly BackgroundWidget fullscreenVideoPlayer;

		readonly ScrollPanelWidget missionList;
		readonly ScrollItemWidget headerTemplate;
		readonly ScrollItemWidget template;

		MapPreview selectedMap;
		PlayingVideo playingVideo;

		string difficulty;
		string gameSpeed;

		[ObjectCreator.UseCtor]
		public MissionBrowserLogic(Widget widget, ModData modData, World world, Action onStart, Action onExit, string initialMap)
		{
			this.modData = modData;
			this.onStart = onStart;
			Game.BeforeGameStart += OnGameStart;

			missionList = widget.Get<ScrollPanelWidget>("MISSION_LIST");

			headerTemplate = widget.Get<ScrollItemWidget>("HEADER");
			template = widget.Get<ScrollItemWidget>("TEMPLATE");

			var title = widget.GetOrNull<LabelWidget>("MISSIONBROWSER_TITLE");
			if (title != null)
				title.GetText = () => playingVideo != PlayingVideo.None ? selectedMap.Title : title.Text;

			widget.Get("MISSION_INFO").IsVisible = () => selectedMap != null;

			var previewWidget = widget.Get<MapPreviewWidget>("MISSION_PREVIEW");
			previewWidget.Preview = () => selectedMap;
			previewWidget.IsVisible = () => playingVideo == PlayingVideo.None;

			videoPlayer = widget.Get<VideoPlayerWidget>("MISSION_VIDEO");
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
			if (modData.Manifest.Missions.Length > 0)
			{
				var yaml = MiniYaml.Merge(modData.Manifest.Missions.Select(
					m => MiniYaml.FromStream(modData.DefaultFileSystem.Open(m), m)));

				foreach (var kv in yaml)
				{
					var missionMapPaths = kv.Value.Nodes.Select(n => n.Key).ToList();

					var previews = modData.MapCache
						.Where(p => p.Class == MapClassification.System && p.Status == MapStatus.Available)
						.Select(p => new
						{
							Preview = p,
							Index = missionMapPaths.IndexOf(Path.GetFileName(p.Package.Name))
						})
						.Where(x => x.Index != -1)
						.OrderBy(x => x.Index)
						.Select(x => x.Preview);

					if (previews.Any())
					{
						CreateMissionGroup(kv.Key, previews, onExit);
						allPreviews.AddRange(previews);
					}
				}
			}

			// Add an additional group for loose missions
			var loosePreviews = modData.MapCache
				.Where(p => p.Status == MapStatus.Available && p.Visibility.HasFlag(MapVisibility.MissionSelector) && !allPreviews.Any(a => a.Uid == p.Uid));

			if (loosePreviews.Any())
			{
				CreateMissionGroup("Missions", loosePreviews, onExit);
				allPreviews.AddRange(loosePreviews);
			}

			if (allPreviews.Count > 0)
			{
				var uid = modData.MapCache.GetUpdatedMap(initialMap);
				var map = uid == null ? null : modData.MapCache[uid];
				if (map != null && map.Visibility.HasFlag(MapVisibility.MissionSelector))
				{
					SelectMap(map);
					missionList.ScrollToSelectedItem();
				}
				else
					SelectMap(allPreviews.First());
			}

			// Preload map preview to reduce jank
			new Thread(() =>
			{
				foreach (var p in allPreviews)
					p.GetMinimap();
			}).Start();

			var startButton = widget.Get<ButtonWidget>("STARTGAME_BUTTON");
			startButton.OnClick = () => StartMissionClicked(onExit);
			startButton.IsDisabled = () => selectedMap == null;

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				StopVideo(videoPlayer);
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};
		}

		void OnGameStart()
		{
			Ui.CloseWindow();

			DiscordService.UpdateStatus(DiscordState.PlayingCampaign);

			onStart();
		}

		bool disposed;
		protected override void Dispose(bool disposing)
		{
			if (disposing && !disposed)
			{
				disposed = true;
				Game.BeforeGameStart -= OnGameStart;
			}

			base.Dispose(disposing);
		}

		void CreateMissionGroup(string title, IEnumerable<MapPreview> previews, Action onExit)
		{
			var header = ScrollItemWidget.Setup(headerTemplate, () => false, () => { });
			header.Get<LabelWidget>("LABEL").GetText = () => title;
			missionList.AddChild(header);

			foreach (var preview in previews)
			{
				var item = ScrollItemWidget.Setup(template,
					() => selectedMap != null && selectedMap.Uid == preview.Uid,
					() => SelectMap(preview),
					() => StartMissionClicked(onExit));

				var label = item.Get<LabelWithTooltipWidget>("TITLE");
				WidgetUtils.TruncateLabelToTooltip(label, preview.Title);

				missionList.AddChild(item);
			}
		}

		void SelectMap(MapPreview preview)
		{
			selectedMap = preview;

			// Cache the rules on a background thread to avoid jank
			var difficultyDisabled = true;
			var difficulties = new Dictionary<string, string>();

			var briefingVideo = "";
			var briefingVideoVisible = false;

			var infoVideo = "";
			var infoVideoVisible = false;

			new Thread(() =>
			{
				var mapDifficulty = preview.WorldActorInfo.TraitInfos<ScriptLobbyDropdownInfo>()
					.FirstOrDefault(sld => sld.ID == "difficulty");

				if (mapDifficulty != null)
				{
					difficulty = mapDifficulty.Default;
					difficulties = mapDifficulty.Values;
					difficultyDisabled = mapDifficulty.Locked;
				}

				var missionData = preview.WorldActorInfo.TraitInfoOrDefault<MissionDataInfo>();
				if (missionData != null)
				{
					briefingVideo = missionData.BriefingVideo;
					briefingVideoVisible = briefingVideo != null;

					infoVideo = missionData.BackgroundVideo;
					infoVideoVisible = infoVideo != null;

					var briefing = WidgetUtils.WrapText(missionData.Briefing?.Replace("\\n", "\n"), description.Bounds.Width, descriptionFont);
					var height = descriptionFont.Measure(briefing).Y;
					Game.RunAfterTick(() =>
					{
						if (preview == selectedMap)
						{
							description.Text = briefing;
							description.Bounds.Height = height;
							descriptionPanel.Layout.AdjustChildren();
						}
					});
				}
			}).Start();

			startBriefingVideoButton.IsVisible = () => briefingVideoVisible && playingVideo != PlayingVideo.Briefing;
			startBriefingVideoButton.OnClick = () => PlayVideo(videoPlayer, briefingVideo, PlayingVideo.Briefing);

			startInfoVideoButton.IsVisible = () => infoVideoVisible && playingVideo != PlayingVideo.Info;
			startInfoVideoButton.OnClick = () => PlayVideo(videoPlayer, infoVideo, PlayingVideo.Info);

			descriptionPanel.ScrollToTop();

			if (difficultyButton != null)
			{
				var difficultyName = new CachedTransform<string, string>(id => id == null || !difficulties.ContainsKey(id) ? "Normal" : difficulties[id]);
				difficultyButton.IsDisabled = () => difficultyDisabled;
				difficultyButton.GetText = () => difficultyName.Update(difficulty);
				difficultyButton.OnMouseDown = _ =>
				{
					var options = difficulties.Select(kv => new DropDownOption
					{
						Title = kv.Value,
						IsSelected = () => difficulty == kv.Key,
						OnClick = () => difficulty = kv.Key
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

				var speedText = new CachedTransform<string, string>(s => modData.Translation.GetString(speeds[s].Name));
				gameSpeedButton.GetText = () => speedText.Update(gameSpeed);
				gameSpeedButton.OnMouseDown = _ =>
				{
					var options = speeds.Select(s => new DropDownOption
					{
						Title = modData.Translation.GetString(s.Value.Name),
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

		void PlayVideo(VideoPlayerWidget player, string video, PlayingVideo pv, Action onComplete = null)
		{
			if (!modData.DefaultFileSystem.Exists(video))
			{
				ConfirmationDialogs.ButtonPrompt(modData,
					title: NoVideoTitle,
					text: NoVideoPrompt,
					cancelText: NoVideoCancel,
					onCancel: () => { });
			}
			else
			{
				StopVideo(player);

				playingVideo = pv;
				player.LoadAndPlay(video);

				if (player.Video == null)
				{
					StopVideo(player);

					ConfirmationDialogs.ButtonPrompt(modData,
						title: CantPlayTitle,
						text: CantPlayPrompt,
						cancelText: CantPlayCancel,
						onCancel: () => { });
				}
				else
				{
					// video playback runs asynchronously
					player.PlayThen(() =>
					{
						StopVideo(player);
						onComplete?.Invoke();
					});

					// Mute other distracting sounds
					MuteSounds();
				}
			}
		}

		void StopVideo(VideoPlayerWidget player)
		{
			if (playingVideo == PlayingVideo.None)
				return;

			UnMuteSounds();
			player.Stop();
			playingVideo = PlayingVideo.None;
		}

		void StartMissionClicked(Action onExit)
		{
			StopVideo(videoPlayer);

			// If selected mission becomes unavailable, exit MissionBrowser to refresh
			var map = modData.MapCache.GetUpdatedMap(selectedMap.Uid);
			if (map == null)
			{
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
				return;
			}

			selectedMap = modData.MapCache[map];
			var orders = new List<Order>();
			if (difficulty != null)
				orders.Add(Order.Command($"option difficulty {difficulty}"));

			orders.Add(Order.Command($"option gamespeed {gameSpeed}"));
			orders.Add(Order.Command($"state {Session.ClientState.Ready}"));

			var missionData = selectedMap.WorldActorInfo.TraitInfoOrDefault<MissionDataInfo>();
			if (missionData != null && missionData.StartVideo != null && modData.DefaultFileSystem.Exists(missionData.StartVideo))
			{
				var fsPlayer = fullscreenVideoPlayer.Get<VideoPlayerWidget>("PLAYER");
				fullscreenVideoPlayer.Visible = true;
				PlayVideo(fsPlayer, missionData.StartVideo, PlayingVideo.GameStart, () =>
				{
					Game.CreateAndStartLocalServer(selectedMap.Uid, orders);
				});
			}
			else
				Game.CreateAndStartLocalServer(selectedMap.Uid, orders);
		}

		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
