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
using System.IO;
using System.Linq;
using System.Net;
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
		readonly ButtonWidget startVideoButton;
		readonly ButtonWidget stopVideoButton;
		readonly VqaPlayerWidget videoPlayer;

		MapPreview selectedMapPreview;

		bool showVideoPlayer;

		[ObjectCreator.UseCtor]
		public MissionBrowserLogic(Widget widget, Action onStart, Action onExit)
		{
			this.onStart = onStart;

			var missionList = widget.Get<ScrollPanelWidget>("MISSION_LIST");

			var headerTemplate = widget.Get<ScrollItemWidget>("HEADER");
			var template = widget.Get<ScrollItemWidget>("TEMPLATE");

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

			startVideoButton = widget.Get<ButtonWidget>("START_VIDEO_BUTTON");
			stopVideoButton = widget.Get<ButtonWidget>("STOP_VIDEO_BUTTON");
			stopVideoButton.IsVisible = () => showVideoPlayer;
			stopVideoButton.OnClick = StopVideo;

			var yaml = new MiniYaml(null, Game.modData.Manifest.Missions.Select(MiniYaml.FromFile).Aggregate(MiniYaml.MergeLiberal)).ToDictionary();

			missionList.RemoveChildren();

			var selectedFirst = false;
			foreach (var kv in yaml)
			{
				var header = ScrollItemWidget.Setup(headerTemplate, () => true, () => {});
				header.Get<LabelWidget>("LABEL").GetText = () => kv.Key;
				missionList.AddChild(header);

				var missionMapPaths = kv.Value.Nodes.Select(n => Platform.ResolvePath(n.Key));

				var maps = Game.modData.MapCache
					.Where(p => p.Status == MapStatus.Available && missionMapPaths.Contains(Path.GetFullPath(p.Map.Path)))
					.Select(p => p.Map);

				foreach (var m in maps)
				{
					var map = m;

					var item = ScrollItemWidget.Setup(template,
						() => selectedMapPreview != null && selectedMapPreview.Uid == map.Uid,
						() => SelectMap(map),
						StartMission);

					item.Get<LabelWidget>("TITLE").GetText = () => map.Title;
					missionList.AddChild(item);

					if (!selectedFirst)
					{
						SelectMap(map);
						selectedFirst = true;
					}
				}
			}

			widget.Get<ButtonWidget>("STARTGAME_BUTTON").OnClick = StartMission;

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				StopVideo();
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};
		}

		float cachedSoundVolume;
		float cachedMusicVolume;
		void SelectMap(Map map)
		{
			StopVideo();

			selectedMapPreview = Game.modData.MapCache[map.Uid];
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

			OrderManager om = null;

			Action lobbyReady = null;
			lobbyReady = () =>
			{
				Game.LobbyInfoChanged -= lobbyReady;
				onStart();
				om.IssueOrder(Order.Command("state {0}".F(Session.ClientState.Ready)));
			};
			Game.LobbyInfoChanged += lobbyReady;

			om = Game.JoinServer(IPAddress.Loopback.ToString(), Game.CreateLocalServer(selectedMapPreview.Uid), "", false);
		}
	}
}
