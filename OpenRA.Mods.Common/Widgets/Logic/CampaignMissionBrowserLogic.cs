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
using System.Threading;

using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class CampaignMissionBrowserLogic
	{
		readonly CampaignWorldLogic campaignWorld;
		readonly MapPreviewWidget missionPreviewWidget;
		readonly LabelWidget previewMenuTitle;
		readonly ButtonWidget campaignBrowserBackButton, nextButton, prevButton;
		readonly ButtonWidget playButton;
		readonly ButtonWidget missionPreviewGraficButton;
		readonly LabelWidget missionTitle;
		readonly ScrollPanelWidget missionDescriptionPanel;
		readonly LabelWidget missionDescription;
		readonly SpriteFont missionDescriptionFont;
		readonly ScrollPanelWidget countryDescriptionPanel;
		readonly LabelWidget countryDescriptionHeader, countryDescriptionValues;
		readonly SpriteFont countryDescriptionFont;

		MapPreview selectedMapPreview, firstMapPreview;
		Map nextMap;

		int mapIndex = 0;
		string lastMission = "";

		bool campaignPreviewRequired = false;
		bool congratsFlag = false;
		Dictionary<string, bool> lastMissionSuccessfullyPlayed = new Dictionary<string, bool>();

		List<Map> factionMaps = new List<Map>();	// All maps of a faction
		List<Map> nextMaps = new List<Map>();		// All actual playable maps

		public CampaignMissionBrowserLogic(CampaignWorldLogic campaignWorld, Widget widget, Action onExit)
		{
			this.campaignWorld = campaignWorld;

			foreach (var f in CampaignProgress.Factions)
				lastMissionSuccessfullyPlayed.Add(f, false);

			// Preview label
			previewMenuTitle = widget.Get<LabelWidget>("PREVIEW_MENU_TITLE");

			// Next and previous button
			nextButton = widget.Get<ButtonWidget>("NEXT_BUTTON");
			prevButton = widget.Get<ButtonWidget>("PREVIOUS_BUTTON");

			// Campaign browser back button
			campaignBrowserBackButton = widget.Get<ButtonWidget>("CAMPAIGN_BROWSER_BACK_BUTTON");
			campaignBrowserBackButton.OnClick = () =>
			{
				if (this.campaignPreviewRequired)
					campaignWorld.ShowCampaignPreview();
				else
				{
					Game.Disconnect();
					Ui.CloseWindow();
					onExit();
				}
			};

			// Map grafic
			widget.Get("CAMPAIGN_BROWSER_GRAFIC_CONTAINER").IsVisible = () => selectedMapPreview != null;

			missionPreviewWidget = widget.Get<MapPreviewWidget>("MISSION_PREVIEW");
			missionPreviewWidget.Preview = () => selectedMapPreview;

			missionPreviewGraficButton = widget.Get<ButtonWidget>("MISSION_PREVIEW_BUTTON");
			missionPreviewGraficButton.OnClick = CallbackPlayButtonOnClick;

			// Mission text
			missionTitle = widget.Get<LabelWidget>("MISSION_TITLE");
			missionTitle.GetText = () => this.GetNextMap().Title;

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
			if (this.GetNextMap() != null)
			{
				// Mission Map
				var missionDescriptionText = this.GetNextMap().Description != null ?
					this.GetNextMap().Description.Replace("\\n", "\n") : "Mission description not available";
				missionDescriptionText = WidgetUtils.WrapText(missionDescriptionText, missionDescription.Bounds.Width, missionDescriptionFont);
				missionDescription.Text = missionDescriptionText;
				missionDescription.Bounds.Height = missionDescriptionFont.Measure(missionDescriptionText).Y;
				missionDescriptionPanel.ScrollToTop();
				missionDescriptionPanel.Layout.AdjustChildren();

				var countryDescriptionText = this.GetNextMap().CountryDescription;
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

				selectedMapPreview = Game.ModData.MapCache[this.GetNextMap().Uid];
			}
		}

		void CallbackPlayButtonOnClick()
		{
			campaignWorld.SetCampaignBrowserVisibility(false);
			campaignWorld.SetVideoBackgroundVisibility(true);
			campaignWorld.PlayAndStart();
		}

		public bool GetCampaignPreviewRequired()
		{
			return this.campaignPreviewRequired;
		}

		void ConfigureNextAndPrevButton()
		{
			if (nextMaps.Count > 1)
			{
				nextButton.OnClick = () =>
				{
					mapIndex = (mapIndex + 1) % nextMaps.Count;
					nextMap = nextMaps[mapIndex];
					SetMapContent();
				};

				prevButton.OnClick = () =>
				{
					mapIndex = (mapIndex + nextMaps.Count - 1) % nextMaps.Count;
					nextMap = nextMaps[mapIndex];
					SetMapContent();
				};
			}
			else
			{
				nextButton.Visible = false;
				prevButton.Visible = false;
			}
		}

		public void ProgressCampaign()
		{
			this.LoadFactionMaps();
			this.LoadCurrentMissions();
			this.CheckCampaignCompleted();
			this.ConfigureNextAndPrevButton();

			this.CheckCampaignProgressForPreview();

			var success = false;
			foreach (var f in CampaignProgress.Factions)
			{
				if (CampaignWorldLogic.Campaign.Equals(f + " Campaign") && lastMissionSuccessfullyPlayed[f])
				{
					success = true;
					break;
				}
			}

			if (success)
				campaignWorld.ShowCongratulations();
			else if (campaignPreviewRequired)
			{
				campaignWorld.ShowCampaignPreview();
			}
			else
				campaignWorld.CallbackShowCampaignBrowserOnClick();
		}

		public void CheckCampaignProgressForPreview()
		{
			// Check campaign preview
			if (nextMaps.Count() > 1)	// More missions available - campaign preview required
				campaignPreviewRequired = true;
			else
				campaignPreviewRequired = false;
		}

		public Map GetNextMap()
		{
			return this.nextMap;
		}

		public MapPreview GetFirstMapPreview()
		{
			return firstMapPreview;
		}

		public void SwitchFirstMapPreview()
		{
			if (firstMapPreview.HasCampaignPreview())
			{
				firstMapPreview.SwitchPreview();
				foreach (var f in CampaignProgress.Factions)
				{
					if (CampaignWorldLogic.Campaign.Equals(f + " Campaign"))
						campaignWorld.SetFactionLogoVisible(f, false);
				}
			}
			else
			{
				foreach (var f in CampaignProgress.Factions)
				{
					if (CampaignWorldLogic.Campaign.Equals(f + " Campaign"))
						campaignWorld.SetFactionLogoVisible(f, true);
				}
			}
		}

		void LoadFactionMaps()
		{
			if (Game.ModData.Manifest.Missions.Any())
			{
				var yaml = Game.ModData.Manifest.Missions.Select(MiniYaml.FromFile).Aggregate(MiniYaml.MergeLiberal);

				foreach (var kv in yaml)
				{
					var missionMapPaths = kv.Value.Nodes.Select(n => Path.GetFullPath(n.Key));

					var maps = Game.ModData.MapCache	// Maps of faction
						.Where(p => p.Status == MapStatus.Available && missionMapPaths.Contains(Path.GetFullPath(p.Map.Path)))
						.Select(p => p.Map);

					if (kv.Key.Equals(CampaignWorldLogic.Campaign))
					{
						factionMaps.AddRange(maps);	 // All factionMaps maps
						campaignWorld.GetWorldMenuTitle().Text = kv.Key.ToString();
						previewMenuTitle.Text = kv.Key.ToString();
						break;
					}
				}
			}
		}

		void LoadCurrentMissions()
		{
			foreach (var f in CampaignProgress.Factions)
			{
				if (CampaignWorldLogic.Campaign.Equals(f + " Campaign"))
					lastMission = CampaignProgress.GetMission(f);
			}

			if (lastMission.Length > 0)
			{
				var maps = factionMaps
					.Where(m => m.Path.Contains(lastMission)).ToList();
				if (maps.Count > 0)
				{
					var nextMissions = maps.First().NextMission;
					if (nextMissions != null)
					{
						nextMissions.Split(',')
							.ToList().ForEach(s => LoadMission(s));
					}

					SelectFirstMission();
				}
			}
		}

		void SelectFirstMission()
		{
			if (nextMaps.Count > 0)
			{
				nextMap = nextMaps[0];
				firstMapPreview = Game.ModData.MapCache[nextMap.Uid];
			}
		}

		void LoadMission(string name)
		{
			nextMaps.AddRange(factionMaps
				.Where(m => m.Path.Contains(name.Trim())));
		}

		void CheckCampaignCompleted()
		{
			// Check if campaign completed
			if (lastMission.Length > 0 && nextMap == null)
			{
				congratsFlag = true;
				foreach (var f in CampaignProgress.Factions)
				{
					if (CampaignWorldLogic.Campaign.Equals(f + " Campaign"))
						lastMissionSuccessfullyPlayed[f] = true;
				}

				LoadMission(lastMission);
				SelectFirstMission();
			}
			else
			{
				congratsFlag = false;

				// The case if no map is in the progress file for this faction (but the other)
				if (lastMission.Length == 0)
				{
					lastMission = factionMaps.First().Path.Split('\\').Last();
					LoadMission(lastMission);
					SelectFirstMission();
				}
			}
		}

		public bool GetCongratsFlag()
		{
			return congratsFlag;
		}
	}
}