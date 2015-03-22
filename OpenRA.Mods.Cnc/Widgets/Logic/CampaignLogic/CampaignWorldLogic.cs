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
using System.Linq;
using System.Net;

using OpenRA.FileSystem;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic.CampaignLogic
{
	public class CampaignWorldLogic
	{
		public static string Campaign = "";
		readonly Action onStart;

		readonly ContainerWidget campaignBrowser, campaignPreview;
		readonly LabelWidget worldMenuTitle;
		readonly VqaPlayerWidget videoPlayer;
		readonly BackgroundWidget videoBackground;
		readonly float cachedMusicVolume;

		bool videoStopped = false;

		CampaignCongratulationLogic campaignCongratulation;
		CampaignMissionBrowserLogic campaignMissionBrowser;
		CampaignMissionPreviewLogic campaignMissionPreview;

		[ObjectCreator.UseCtor]
		public CampaignWorldLogic(Widget widget, Action onStart, Action onExit)
		{
			this.onStart = onStart;

			cachedMusicVolume = Sound.MusicVolume;

			worldMenuTitle = widget.Get<LabelWidget>("CAMPAIGN_MENU_TITLE");
			campaignBrowser = widget.Get<ContainerWidget>("CAMPAIGN_BROWSER");
			campaignPreview = widget.Get<ContainerWidget>("CAMPAIGN_PREVIEW");

			videoBackground = widget.Get<BackgroundWidget>("VIDEO_BG");
			videoBackground.IsVisible = () => false;

			videoPlayer = widget.Get<VqaPlayerWidget>("VIDEO");

			this.campaignCongratulation = new CampaignCongratulationLogic(this, widget, onExit);
			this.campaignMissionBrowser = new CampaignMissionBrowserLogic(this, widget, onExit);
			this.campaignMissionPreview = new CampaignMissionPreviewLogic(this, campaignMissionBrowser, widget, onExit);

			this.campaignMissionBrowser.ProgressCampaign();
			this.SetMapContent();
		}

		public void CallbackCampaignCongratulationContinueButtonOnClick()
		{
			if (this.campaignMissionBrowser.GetCampaignPreviewRequired())
				this.ShowCampaignPreview();
			else
				this.CallbackShowCampaignBrowserOnClick();
		}

		public void SetMapContent()
		{
			this.campaignMissionPreview.SetMapContent();
		}

		public LabelWidget GetWorldMenuTitle()
		{
			return worldMenuTitle;
		}

		public void ShowCampaignPreview()
		{
			campaignMissionBrowser.SetPreviewContent();
			campaignPreview.IsVisible = () => true;
			campaignBrowser.IsVisible = () => false;
			campaignCongratulation.SetCongratulationVisibility(false);
		}

		public void SetCampaignBrowserVisibility(bool visibility)
		{
			this.campaignBrowser.IsVisible = () => visibility;
		}

		public void CallbackShowCampaignBrowserOnClick()
		{
			campaignPreview.IsVisible = () => false;
			campaignBrowser.IsVisible = () => true;
			campaignCongratulation.SetCongratulationVisibility(false);
		}

		public void ShowCongratulations()
		{
			campaignPreview.IsVisible = () => false;
			campaignBrowser.IsVisible = () => false;
			campaignCongratulation.SetCongratulationVisibility(true);
		}

		public void PlayAndStart()
		{
			Sound.MusicVolume = 0;

			if (GlobalFileSystem.Exists(campaignMissionBrowser.GetNextMap().Videos.Briefing))
				{
					videoPlayer.Load(campaignMissionBrowser.GetNextMap().Videos.Briefing);
					videoPlayer.PlayThen(StopVideoAndStart);
				}
				else
					StartMission();
		}

		public void SetVideoBackgroundVisibility(bool visible)
		{
			this.videoBackground.IsVisible = () => visible;
		}

		void StopVideoAndStart()
		{
			if (videoStopped)
			{
				Sound.MusicVolume = cachedMusicVolume;
				videoPlayer.Stop();
			}
			else
			{
				videoStopped = true;
				StopVideoAndStart();
				StartMission();
			}
		}

		void StartMission()
		{
			OrderManager om = null;

			Action lobbyReady = null;
			lobbyReady = () =>
			{
				Game.LobbyInfoChanged -= lobbyReady;
				onStart();
				om.IssueOrder(Order.Command("state {0}".F(Session.ClientState.Ready)));
			};
			Game.LobbyInfoChanged += lobbyReady;

			CampaignProgress.SetPlayedMission(campaignMissionBrowser.GetNextMap().Path.Split('\\').ToList().Last());

			om = Game.JoinServer(IPAddress.Loopback.ToString(), Game.CreateLocalServer(campaignMissionBrowser.GetNextMap().Uid), "", false);
		}
	}
}
