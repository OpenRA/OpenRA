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
using System.Linq;

using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class CampaignMissionPreviewLogic
	{
		readonly CampaignWorldLogic campaignWorld;
		readonly MapPreviewWidget campaignPreviewWidget;
		readonly ButtonWidget campaignPreviewContinueButton, campaignPreviewGraficButton, campaignPreviewBackButton;

		Dictionary<string, ImageWidget> campaignPreviewFactionLogos;

		public CampaignMissionPreviewLogic(CampaignWorldLogic campaignWorld, Widget widget, Action onExit)
		{
			this.campaignWorld = campaignWorld;

			// Campaign preview grafic
			campaignPreviewWidget = widget.Get<MapPreviewWidget>("CAMPAIGN_PREVIEW_GRAFIC");
			campaignPreviewWidget.Preview = campaignWorld.GetFirstMapPreview;

			campaignPreviewGraficButton = widget.Get<ButtonWidget>("CAMPAIGN_PREVIEW_GRAFIC_BUTTON");
			campaignPreviewGraficButton.OnClick = campaignWorld.CallbackShowCampaignBrowserOnClick;

			campaignPreviewFactionLogos = new Dictionary<string, ImageWidget>();
			foreach (var f in CampaignProgress.Factions) {
				var factionLogo = widget.Get<ImageWidget>("CAMPAIGN_PREVIEW_" + f.ToUpper() + "_LOGO");
				factionLogo.Visible = false;
				campaignPreviewFactionLogos.Add(f, factionLogo);
			}

			// Campaign preview button
			campaignPreviewContinueButton = widget.Get<ButtonWidget>("CAMPAIGN_PREVIEW_CONTINUE_BUTTON");
			campaignPreviewContinueButton.OnClick = campaignWorld.CallbackShowCampaignBrowserOnClick;

			// Campaign preview back button
			campaignPreviewBackButton = widget.Get<ButtonWidget>("CAMPAIGN_PREVIEW_BACK_BUTTON");
			campaignPreviewBackButton.OnClick = () =>
			{
				campaignWorld.SwitchFirstMapPreview();
				if (campaignWorld.GetCongratsFlag())
					campaignWorld.ShowCongratulations();
				else
				{
					Game.Disconnect();
					Ui.CloseWindow();
					onExit();
				}
			};
		}

		public void SetFactionLogoVisible(string faction, bool visible)
		{
			ImageWidget factionLogo = null;
			campaignPreviewFactionLogos.TryGetValue(faction, out factionLogo);
			if (factionLogo != null)
			{
				factionLogo.Visible = visible;
				campaignPreviewWidget.Visible = !visible;
			}
		}
	}
}
