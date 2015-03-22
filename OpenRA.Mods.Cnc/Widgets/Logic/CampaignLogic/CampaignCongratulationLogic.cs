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

using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;
using OpenRA.Graphics;

namespace OpenRA.Mods.Cnc.Widgets.Logic.CampaignLogic
{

	class CampaignCongratulationLogic
	{
		readonly CampaignWorldLogic campaignWorld;
		readonly ButtonWidget campaignCongratulationBackButton;
		readonly ButtonWidget campaignCongratulationContinueButton;
		readonly LabelWidget congratulationText;
		readonly ScrollPanelWidget congratulationTextPanel;
		readonly SpriteFont congratulationTextFont;
		readonly ContainerWidget congratulationNodLogo, congratulationGdiLogo, campaignCongratulationWidget;

		public CampaignCongratulationLogic(CampaignWorldLogic campaignWorld, Widget widget, Action onExit)
		{
			this.campaignWorld = campaignWorld;
			this.campaignCongratulationWidget = widget.Get<ContainerWidget>("CAMPAIGN_CONGRATULATION");

			// Congratulation description
			this.congratulationTextPanel = widget.Get<ScrollPanelWidget>("CONGRATULATION_TEXT_PANEL");
			this.congratulationText = this.congratulationTextPanel.Get<LabelWidget>("CONGRATULATION_TEXT");
			this.congratulationTextFont = Game.Renderer.Fonts[congratulationText.Font];

			// Congratulation logos
			this.congratulationGdiLogo = widget.Get<ContainerWidget>("CONGRATULATION_LOGO_GDI");
			this.congratulationNodLogo = widget.Get<ContainerWidget>("CONGRATULATION_LOGO_NOD");

			// Congratulation replay button
			this.campaignCongratulationContinueButton = widget.Get<ButtonWidget>("CAMPAIGN_CONGRATULATION_CONTINUE_BUTTON");
			this.campaignCongratulationContinueButton.OnClick = this.campaignWorld.CallbackCampaignCongratulationContinueButtonOnClick;

			// Congratulation back button
			this.campaignCongratulationBackButton = widget.Get<ButtonWidget>("CAMPAIGN_CONGRATULATION_BACK_BUTTON");
			this.campaignCongratulationBackButton.OnClick = () =>
			{
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};

			this.SetCongratulationContent();
		}

		public void SetCongratulationVisibility(bool visible)
		{
			campaignCongratulationWidget.IsVisible = () => visible;
		}

		private void SetCongratulationContent()
		{
			// TODO: It would be more elegant to read the text from a yaml-file instead of hardcoding it
			var victoryTextGdi = "Good work Commander! Thanks to your efforts the Global Defence Initiative was victorious. Your actions have thrown the brotherhood into disarray and without their leader we should soon be able to completely rid the world of their remnants.";
			var victoryTextNod = "Well done Brother! Your heroic actions have shown the world truth and freedom. Soon we will be free of the GDIs opression. Kane is proud of you!";
			var faction = CampaignWorldLogic.Campaign.Equals("GDI Campaign");
			var victoryText = faction ? victoryTextGdi : victoryTextNod;
			
			victoryText = WidgetUtils.WrapText(victoryText, this.congratulationText.Bounds.Width, this.congratulationTextFont);
			
			this.congratulationText.Text = victoryText;
			this.congratulationText.Bounds.Height = this.congratulationTextFont.Measure(victoryText).Y;
			this.congratulationTextPanel.ScrollToTop();
			this.congratulationTextPanel.Layout.AdjustChildren();

			this.congratulationGdiLogo.IsVisible = () => faction;
			this.congratulationNodLogo.IsVisible = () => !faction;
		}

	}
}
