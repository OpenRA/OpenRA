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

using OpenRA.FileSystem;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.CampaignLogic
{
	public class CampaignFactionLogic
	{
		readonly Action onStart;
		readonly VqaPlayerWidget videoPlayer;
		readonly VqaPlayerWidget videoBGPlayer;
		readonly ImageWidget chooseFactionBanner;
		readonly BackgroundWidget chooseFactionNoise;
		readonly float cachedMusicVolume;
		bool videoStopped = false;
		bool campaignStarted = false;
		string startedCampaign;

		List<ButtonWidget> buttonList = new List<ButtonWidget>();
		List<ImageWidget> imageList = new List<ImageWidget>();
		List<string> factionList = CampaignProgress.Factions;
		public string VideoStart;
		List<string> videoFaction;
		string audioFaction;
		int actualVideo;

		enum PlayThen
		{
			Replay,
			Faction,
			Start
		}

		PlayThen playThen = PlayThen.Replay;

		[ObjectCreator.UseCtor]
		public CampaignFactionLogic(Widget widget, Action onStart, Action onExit)
		{
			this.onStart = onStart;

			VideoStart = null;
			videoFaction = null;
			audioFaction = null;
			actualVideo = 0;

			int i = 0;
			foreach (var f in factionList)
			{
				buttonList.Add(widget.Get<ButtonWidget>(f));
				buttonList[i].OnClick = () => CallbackFactionButtonOnClick(f);
				imageList.Add(widget.Get<ImageWidget>(f + "_LOGO"));
				i++;
			}

			videoBGPlayer = widget.Get<VqaPlayerWidget>("VIDEO_BG");
			chooseFactionBanner = widget.GetOrNull<ImageWidget>("CHOOSE_FACTION_IMAGE");
			chooseFactionNoise = widget.GetOrNull<BackgroundWidget>("BG");

			this.videoPlayer = widget.Get<VqaPlayerWidget>("VIDEO");

			// Mute other distracting sounds
			cachedMusicVolume = Sound.MusicVolume;
			Sound.MusicVolume = 0;

			GetStartVideo();

			if (VideoStart != null && GlobalFileSystem.Exists(VideoStart))
			{
				if (chooseFactionNoise != null)
					chooseFactionNoise.Visible = false;
				foreach (var image in imageList)
				{
					image.Visible = false;
				}

				videoBGPlayer.Load(VideoStart);
				videoPlayer.Load(VideoStart);
				videoPlayer.PlayThen(PlayThenMethod);
			}

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				if (playThen == PlayThen.Replay)
				{
					StopVideo();
					Game.Disconnect();
					Ui.CloseWindow();
					onExit();
				}
				else
				{
					StopVideo();
					if (playThen != PlayThen.Start && !campaignStarted)
						StartCampaign(startedCampaign);
				}
			};
		}

		void PlayThenMethod()
		{
			if (!videoStopped)
			{
				var filename = "";
				switch (playThen)
				{
					case PlayThen.Replay:
						if (startedCampaign != null)
							playThen = PlayThen.Start;
						break;
					case PlayThen.Faction:
						filename = videoFaction[actualVideo];
						actualVideo++;
						if (GlobalFileSystem.Exists(filename))
							videoPlayer.Load(filename);
						if (actualVideo < videoFaction.Count)
							playThen = PlayThen.Faction;
						else
							playThen = PlayThen.Start;
						break;
					case PlayThen.Start:
						StartCampaign(startedCampaign);
						return;
				}

				videoPlayer.PlayThen(PlayThenMethod);
			}
		}

		void CallbackFactionButtonOnClick(string faction)
		{
			foreach (var button in buttonList)
			{
				button.IsDisabled = () => true;
			}

			if (chooseFactionBanner != null)
				chooseFactionBanner.Visible = false;
			if (chooseFactionNoise != null)
				chooseFactionNoise.Visible = false;
			CampaignProgress.SaveProgress(faction, "");
			startedCampaign = faction + " Campaign";

			GetFactionMedia(faction);

			if (audioFaction != null)
				Sound.Play(audioFaction);

			if (VideoStart == null)
			{
				System.Threading.Thread.Sleep(3000);
				StartCampaign(startedCampaign);
			}
			else
			{
				if (videoFaction != null)
					playThen = PlayThen.Faction;
				else
					playThen = PlayThen.Replay;
			}
		}

		void GetStartVideo()
		{
			if (Game.ModData.Manifest.FactionMedia.Any())
			{
				var yaml = Game.ModData.Manifest.FactionMedia.Select(MiniYaml.FromFile).Aggregate(MiniYaml.MergeLiberal);

				foreach (var mediaType in yaml)
				{
					if (mediaType.Key.Equals("Video"))
					{
						foreach (var type in mediaType.Value.Nodes)
						{
							if (type.Key.Equals("Start"))
							{
								if (type.Value.Nodes.Count > 0)
									VideoStart = type.Value.Nodes[0].Key;
							}
						}
					}
				}
			}
		}

		void GetFactionMedia(string faction)
		{
			if (Game.ModData.Manifest.FactionMedia.Any())
			{
				var yaml = Game.ModData.Manifest.FactionMedia.Select(MiniYaml.FromFile).Aggregate(MiniYaml.MergeLiberal);

				foreach (var mediaType in yaml)
				{
					if (mediaType.Key.Equals("Video"))
					{
						foreach (var type in mediaType.Value.Nodes)
						{
							if (type.Key.Equals(faction))
							{
								if (type.Value.Nodes != null)
								{
									videoFaction = new List<string>();
									foreach (var factionVideo in type.Value.Nodes)
										videoFaction.Add(factionVideo.Key);
								}
							}
						}
					}

					if (mediaType.Key.Equals("Audio"))
					{
						foreach (var type in mediaType.Value.Nodes)
						{
							if (type.Key.Equals(faction))
							{
								if (type.Value.Nodes.Count > 0)
									audioFaction = type.Value.Nodes[0].Key;
								break;
							}
						}
					}
				}
			}
		}

		void StopVideo()
		{
			videoBGPlayer.Visible = false;
			videoPlayer.Visible = false;
			Sound.MusicVolume = cachedMusicVolume;
			videoPlayer.Stop();
			videoStopped = true;
		}

		Map DetectFirstMapFromFaction(string faction)
		{
			var yaml = Game.ModData.Manifest.Missions.Select(MiniYaml.FromFile).Aggregate(MiniYaml.MergeLiberal);

			var allMaps = new List<Map>();
			foreach (var kv in yaml)
			{
				var missionMapPaths = kv.Value.Nodes.Select(n => Path.GetFullPath(n.Key));

				if (faction.Equals(kv.Key))
				{
					var maps = Game.ModData.MapCache
						.Where(p => p.Status == MapStatus.Available && missionMapPaths.Contains(Path.GetFullPath(p.Map.Path)))
						.Select(p => p.Map);
					allMaps.AddRange(maps);
				}
			}

			return allMaps.First();
		}

		void StartCampaign(string faction)
		{
			if (VideoStart == null)
			{
				foreach (var image in imageList)
				{
					image.Visible = false;
				}
			}

			campaignStarted = true;
			OrderManager om = null;

			Action lobbyReady = null;
			lobbyReady = () =>
			{
				Game.LobbyInfoChanged -= lobbyReady;
				onStart();
				om.IssueOrder(Order.Command("state {0}".F(Session.ClientState.Ready)));
			};
			Game.LobbyInfoChanged += lobbyReady;

			var firstMapFromFaction = this.DetectFirstMapFromFaction(faction);
			var selectedMapPreview = Game.ModData.MapCache[firstMapFromFaction.Uid];
			var video = selectedMapPreview.Map.Videos.Briefing;
			CampaignWorldLogic.Campaign = startedCampaign;

			CampaignProgress.SetPlayedMission(firstMapFromFaction.Path.Split('\\').Last());

			if (GlobalFileSystem.Exists(video))
			{
				videoPlayer.Load(video);
				videoPlayer.PlayThen(() => { StopVideo(); om = Game.JoinServer(IPAddress.Loopback.ToString(), Game.CreateLocalServer(firstMapFromFaction.Uid), "", false); });
			}
			else
			{
				StopVideo();
				om = Game.JoinServer(IPAddress.Loopback.ToString(), Game.CreateLocalServer(firstMapFromFaction.Uid), "", false);
			}
		}
	}
}