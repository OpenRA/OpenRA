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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic.CampaignLogic
{
	class CampaignMissionPreviewLogic
	{
		readonly CampaignMissionBrowserLogic campaignMissionBrowser;
		readonly CampaignWorldLogic campaignWorld;
		readonly MapPreviewWidget missionPreviewWidget;
		readonly ButtonWidget playButton;
		readonly ButtonWidget missionPreviewGraficButton;
		readonly LabelWidget missionTitle;
		readonly ScrollPanelWidget missionDescriptionPanel;
		readonly LabelWidget missionDescription;
		readonly SpriteFont missionDescriptionFont;
		readonly ScrollPanelWidget countryDescriptionPanel;
		readonly LabelWidget countryDescriptionHeader, countryDescriptionValues;
		readonly SpriteFont countryDescriptionFont;

		public CampaignMissionPreviewLogic(CampaignWorldLogic campaignWorld, CampaignMissionBrowserLogic campaignMissionBrowser, Widget widget, Action onExit)
		{
			this.campaignWorld = campaignWorld;
			this.campaignMissionBrowser = campaignMissionBrowser;

			// Map grafic
			widget.Get("CAMPAIGN_BROWSER_GRAFIC_CONTAINER").IsVisible = () => campaignMissionBrowser.GetSelectedMapPreview() != null;

			missionPreviewWidget = widget.Get<MapPreviewWidget>("MISSION_PREVIEW");
			missionPreviewWidget.Preview = () => campaignMissionBrowser.GetSelectedMapPreview();

			missionPreviewGraficButton = widget.Get<ButtonWidget>("MISSION_PREVIEW_BUTTON");
			missionPreviewGraficButton.OnClick = CallbackPlayButtonOnClick;

			// Mission text
			missionTitle = widget.Get<LabelWidget>("MISSION_TITLE");
			missionTitle.GetText = () => campaignMissionBrowser.GetNextMap().Title;

			// Mission description
			missionDescriptionPanel = widget.Get<ScrollPanelWidget>("MISSION_DESCRIPTION_PANEL");
			missionDescription = missionDescriptionPanel.Get<LabelWidget>("MISSION_DESCRIPTION");
			missionDescriptionFont = Game.Renderer.Fonts[missionDescription.Font];

			// Country description
			countryDescriptionPanel = widget.Get<ScrollPanelWidget>("COUNTRY_DESCRIPTION_PANEL");
			countryDescriptionHeader = countryDescriptionPanel.Get<LabelWidget>("COUNTRY_DESCRIPTION_HEADER");
			countryDescriptionValues = countryDescriptionPanel.Get<LabelWidget>("COUNTRY_DESCRIPTION_VALUES");
			countryDescriptionFont = Game.Renderer.Fonts[missionDescription.Font];

			// Play button
			playButton = widget.Get<ButtonWidget>("PLAY_BUTTON");
			playButton.OnClick = CallbackPlayButtonOnClick;
		}

		public void SetMapContent()
		{
			if (campaignMissionBrowser.GetNextMap() != null)
			{
				// Mission Map
				var missionDescriptionText = campaignMissionBrowser.GetNextMap().Description != null ?
					campaignMissionBrowser.GetNextMap().Description.Replace("\\n", "\n") : "Mission description not available";
				missionDescriptionText = WidgetUtils.WrapText(missionDescriptionText, missionDescription.Bounds.Width, missionDescriptionFont);
				missionDescription.Text = missionDescriptionText;
				missionDescription.Bounds.Height = missionDescriptionFont.Measure(missionDescriptionText).Y;
				missionDescriptionPanel.ScrollToTop();
				missionDescriptionPanel.Layout.AdjustChildren();

				var countryDescriptionText = campaignMissionBrowser.GetNextMap().CountryDescription;
				var countryDescriptionTextHeader = "No information available";
				var countryDescriptionTextValues = "";
				if (countryDescriptionText != null && countryDescriptionText.Length > 0)
				{
					countryDescriptionText = countryDescriptionText.Replace("\\n", "\n");
					if (countryDescriptionText.Contains('|'))
					{
						var splits = countryDescriptionText.Split('|');

						if (splits.Length > 0)
						{
							countryDescriptionTextHeader = "";
							for (int i = 0; i < splits.Length; i = i + 2)
							{
								countryDescriptionTextHeader += splits[i];
								countryDescriptionTextValues += splits[i + 1];
							}
						}
					}
				}

				countryDescriptionTextHeader = WidgetUtils.WrapText(countryDescriptionTextHeader, countryDescriptionHeader.Bounds.Width, countryDescriptionFont);
				countryDescriptionHeader.Text = countryDescriptionTextHeader;
				countryDescriptionHeader.Bounds.Height = countryDescriptionFont.Measure(countryDescriptionTextHeader).Y;
				countryDescriptionTextValues = WidgetUtils.WrapText(countryDescriptionTextValues, countryDescriptionValues.Bounds.Width, countryDescriptionFont);
				countryDescriptionValues.Text = countryDescriptionTextValues;
				countryDescriptionValues.Bounds.Height = countryDescriptionFont.Measure(countryDescriptionTextValues).Y;
				countryDescriptionPanel.ScrollToTop();
				countryDescriptionPanel.Layout.AdjustChildren();

				campaignMissionBrowser.SetSelectedMapPreview(Game.ModData.MapCache[campaignMissionBrowser.GetNextMap().Uid]);
			}
		}

		private void CallbackPlayButtonOnClick()
		{
			campaignWorld.SetCampaignBrowserVisibility(false);
			campaignWorld.SetVideoBackgroundVisibility(true);
			campaignWorld.PlayAndStart();
		}
	}
}