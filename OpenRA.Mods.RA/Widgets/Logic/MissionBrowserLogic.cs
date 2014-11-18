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
		readonly Action onStart;
		readonly ScrollPanelWidget descriptionPanel;
		readonly LabelWidget description;
		readonly SpriteFont descriptionFont;
		readonly DropDownButtonWidget difficultyButton;
		readonly ButtonWidget startVideoButton;
		readonly ButtonWidget stopVideoButton;
		readonly VqaPlayerWidget videoPlayer;

		readonly ScrollPanelWidget missionList;
		readonly ScrollItemWidget headerTemplate;
		readonly ScrollItemWidget template;

		MapPreview selectedMapPreview;

		bool showVideoPlayer;

		string difficulty;

		[ObjectCreator.UseCtor]
		public MissionBrowserLogic(Widget widget, Action onStart, Action onExit)
		{
			this.onStart = onStart;

			missionList = widget.Get<ScrollPanelWidget>("MISSION_LIST");

			headerTemplate = widget.Get<ScrollItemWidget>("HEADER");
			template = widget.Get<ScrollItemWidget>("TEMPLATE");

			var title = widget.GetOrNull<LabelWidget>("MISSIONBROWSER_TITLE");
			if (title != null)
				title.GetText = () => showVideoPlayer ? selectedMapPreview.Title : title.Text;

			widget.Get("MISSION_INFO").IsVisible = () => selectedMapPreview != null;

			var previewWidget = widget.Get<MapPreviewWidget>("MISSION_PREVIEW");
			previewWidget.Preview = () => selectedMapPreview;
			previewWidget.IsVisible = () => !showVideoPlayer;

			videoPlayer = widget.Get<VqaPlayerWidget>("MISSION_VIDEO");
			widget.Get("MISSION_BIN").IsVisible = () => showVideoPlayer;

			descriptionPanel = widget.Get<ScrollPanelWidget>("MISSION_DESCRIPTION_PANEL");

			description = descriptionPanel.Get<LabelWidget>("MISSION_DESCRIPTION");
			descriptionFont = Game.Renderer.Fonts[description.Font];

			difficultyButton = widget.Get<DropDownButtonWidget>("DIFFICULTY_DROPDOWNBUTTON");

			startVideoButton = widget.Get<ButtonWidget>("START_VIDEO_BUTTON");
			stopVideoButton = widget.Get<ButtonWidget>("STOP_VIDEO_BUTTON");
			stopVideoButton.IsVisible = () => showVideoPlayer;
			stopVideoButton.OnClick = StopVideo;

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
			// Loose missions must define Type: Mission and Selectable: false.
			var looseMissions = Game.modData.MapCache
				.Where(p => p.Status == MapStatus.Available && p.Map.Type == "Mission" && !p.Map.Selectable && !allMaps.Contains(p.Map))
				.Select(p => p.Map);

			if (looseMissions.Any())
			{
				CreateMissionGroup("Missions", looseMissions);
				allMaps.AddRange(looseMissions);
			}

			if (allMaps.Any())
				SelectMap(allMaps.First());

			var startButton = widget.Get<ButtonWidget>("STARTGAME_BUTTON");
			startButton.OnClick = StartMission;
			startButton.IsDisabled = () => selectedMapPreview.RuleStatus != MapRuleStatus.Cached;

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				StopVideo();
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};
		}

		void CreateMissionGroup(string title, IEnumerable<Map> maps)
		{
			var header = ScrollItemWidget.Setup(headerTemplate, () => true, () => {});
			header.Get<LabelWidget>("LABEL").GetText = () => title;
			missionList.AddChild(header);

			foreach (var m in maps)
			{
				var map = m;

				var item = ScrollItemWidget.Setup(template,
					() => selectedMapPreview != null && selectedMapPreview.Uid == map.Uid,
					() => SelectMap(map),
					StartMission);

				item.Get<LabelWidget>("TITLE").GetText = () => map.Title;
				missionList.AddChild(item);
			}
		}

		float cachedSoundVolume;
		float cachedMusicVolume;
		void SelectMap(Map map)
		{
			StopVideo();

			selectedMapPreview = Game.modData.MapCache[map.Uid];

			// Cache the rules on a background thread to avoid jank
			new Thread(selectedMapPreview.CacheRules).Start();

			var video = selectedMapPreview.Map.PreviewVideo;
			var videoVisible = video != null;
			var videoDisabled = !(videoVisible && GlobalFileSystem.Exists(video));

			startVideoButton.IsVisible = () => videoVisible && !showVideoPlayer;
			startVideoButton.IsDisabled = () => videoDisabled;
			startVideoButton.OnClick = () =>
			{
				showVideoPlayer = true;
				videoPlayer.Load(video);
				videoPlayer.PlayThen(StopVideo);

				// Mute other distracting sounds
				cachedSoundVolume = Sound.SoundVolume;
				cachedMusicVolume = Sound.MusicVolume;
				Sound.SoundVolume = Sound.MusicVolume = 0;
			};

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

		void StopVideo()
		{
			if (!showVideoPlayer)
				return;

			Sound.SoundVolume = cachedSoundVolume;
			Sound.MusicVolume = cachedMusicVolume;

			videoPlayer.Stop();
			showVideoPlayer = false;
		}

		void StartMission()
		{
			StopVideo();

			if (selectedMapPreview.RuleStatus != MapRuleStatus.Cached)
				return;

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
