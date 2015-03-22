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

using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic.CampaignLogic
{
	class CampaignMissionBrowserLogic
	{
		readonly CampaignWorldLogic campaignWorld;
		readonly LabelWidget previewMenuTitle;
		readonly ButtonWidget campaignBrowserBackButton, nextButton, prevButton;
		readonly MapPreviewWidget campaignPreviewWidget;
		readonly ButtonWidget campaignPreviewContinueButton, campaignPreviewGraficButton;

		MapPreview selectedMapPreview;
		Map nextMap;

		int mapIndex = 0;
		string lastMission = "";

		bool campaignPreviewRequired = false;
		bool congratsFlag = false;
		bool lastMissionSuccessfullyPlayedGDI = false;
		bool lastMissionSuccessfullyPlayedNod = false;

		List<Map> factionMaps = new List<Map>();	// All maps of a faction
		List<Map> nextMaps = new List<Map>();		// All actual playable maps

		public CampaignMissionBrowserLogic(CampaignWorldLogic campaignWorld, Widget widget, Action onExit)
		{

			this.campaignWorld = campaignWorld;

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

			// Campaign preview grafic
			campaignPreviewWidget = widget.Get<MapPreviewWidget>("CAMPAIGN_PREVIEW_GRAFIC");

			campaignPreviewGraficButton = widget.Get<ButtonWidget>("CAMPAIGN_PREVIEW_GRAFIC_BUTTON");
			campaignPreviewGraficButton.OnClick = campaignWorld.CallbackShowCampaignBrowserOnClick;

			// Campaign preview button
			campaignPreviewContinueButton = widget.Get<ButtonWidget>("CAMPAIGN_PREVIEW_CONTINUE_BUTTON");
			campaignPreviewContinueButton.OnClick = campaignWorld.CallbackShowCampaignBrowserOnClick;

			// Campaign preview back button
			campaignBrowserBackButton = widget.Get<ButtonWidget>("CAMPAIGN_PREVIEW_BACK_BUTTON");
			campaignBrowserBackButton.OnClick = () =>
			{
				if (this.congratsFlag)
					campaignWorld.ShowCongratulations();
				else
				{
					Game.Disconnect();
					Ui.CloseWindow();
					onExit();
				}
			};

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
					campaignWorld.SetMapContent();
				};

				prevButton.OnClick = () =>
				{
					mapIndex = (mapIndex + nextMaps.Count - 1) % nextMaps.Count;
					nextMap = nextMaps[mapIndex];
					campaignWorld.SetMapContent();
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

			if ((CampaignWorldLogic.Campaign.Equals("GDI Campaign") && lastMissionSuccessfullyPlayedGDI) || (CampaignWorldLogic.Campaign.Equals("Nod Campaign") && lastMissionSuccessfullyPlayedNod))
				campaignWorld.ShowCongratulations();
			else if (campaignPreviewRequired)
				campaignWorld.ShowCampaignPreview();
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

		public MapPreview GetSelectedMapPreview()
		{
			return this.selectedMapPreview;
		}

		public void SetSelectedMapPreview(MapPreview selectedMapPreview)
		{
			this.selectedMapPreview = selectedMapPreview;
		}

		public void SetPreviewContent()
		{
			if (this.GetNextMap().Container.Exists("preview.png"))
				using (var dataStream = this.GetNextMap().Container.GetContent("preview.png"))
				{
					this.GetNextMap().CustomPreview = new System.Drawing.Bitmap(dataStream);
					var preview = new MapPreview(this.GetNextMap().Uid, Game.ModData.MapCache);
					preview.SetMinimap(new SheetBuilder(SheetType.BGRA).Add(this.GetNextMap().CustomPreview));
					campaignPreviewWidget.Preview = () => preview;
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

		private void LoadCurrentMissions()
		{
			if (CampaignWorldLogic.Campaign.Equals("GDI Campaign"))
				lastMission = CampaignProgress.GetGdiProgress();
			else
				lastMission = CampaignProgress.GetNodProgress();
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

		private void SelectFirstMission()
		{
			if (nextMaps.Count > 0)
				nextMap = nextMaps[0];
		}

		private void LoadMission(string name)
		{
			nextMaps.AddRange(factionMaps
				.Where(m => m.Path.Contains(name.Trim()))
				);
		}

		void CheckCampaignCompleted()
		{
			// Check if campaign completed
			if (lastMission.Length > 0 && nextMap == null)
			{
				congratsFlag = true;
				if (CampaignWorldLogic.Campaign.Equals("GDI Campaign"))
					lastMissionSuccessfullyPlayedGDI = true;
				else
					lastMissionSuccessfullyPlayedNod = true;
				LoadMission(lastMission);
				SelectFirstMission();
			}
			else
			{
				congratsFlag = false;
				if (lastMission.Length == 0) // The case if no map is in the progress file for this faction (but the other)
				{
					lastMission = factionMaps.First().Path.Split('\\').Last();
					LoadMission(lastMission);
					SelectFirstMission();
				}

			}
		}

	}
}
