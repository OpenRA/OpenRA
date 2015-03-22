#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncMainMenuLogic : MainMenuLogic
	{
		readonly Widget rootMenu;
		[ObjectCreator.UseCtor]
		public CncMainMenuLogic(Widget widget, World world)
			: base(widget, world)
		{
			var shellmapDecorations = widget.Get("SHELLMAP_DECORATIONS");
			shellmapDecorations.IsVisible = () => menuType != MenuType.None && Game.Settings.Game.ShowShellmap;
			shellmapDecorations.Get<ImageWidget>("RECBLOCK").IsVisible = () => world.WorldTick / 25 % 2 == 0;

			var shellmapDisabledDecorations = widget.Get("SHELLMAP_DISABLED_DECORATIONS");
			shellmapDisabledDecorations.IsVisible = () => !Game.Settings.Game.ShowShellmap;

			// Changed for Campaign
			rootMenu = widget;

			var mainMenu = widget.Get("MAIN_MENU");
			mainMenu.IsVisible = () => menuType == MenuType.Main;

			// Singleplayer menu
			var singleplayerMenu = widget.Get("SINGLEPLAYER_MENU");
			singleplayerMenu.IsVisible = () => menuType == MenuType.Singleplayer;

			CampaignProgress.Init();

			var campaignButton = singleplayerMenu.Get<ButtonWidget>("CAMPAIGN_BUTTON");
			campaignButton.OnClick = () =>
			{
				CampaignProgress.SetSaveProgressFlag();
                menuType = MenuType.None;
                if (CampaignProgress.GetGdiProgress().Length == 0 && CampaignProgress.GetNodProgress().Length == 0)
				{
					Game.OpenWindow("CAMPAIGN_FACTION", new WidgetArgs
					{
						{ "onExit", () => menuType = MenuType.Singleplayer },
						{ "onStart", RemoveShellmapUI }
					});
				}
				else
				{
					Game.OpenWindow("CAMPAIGN_MENU", new WidgetArgs
					{
						{ "onExit", () => menuType = MenuType.Singleplayer },
						{ "onStart", RemoveShellmapUI }
					});
				}
			};

			if (CampaignProgress.GetSaveProgressFlag() &&
                (CampaignProgress.GetGdiProgress().Length > 0 || CampaignProgress.GetNodProgress().Length > 0))
			{
				menuType = MenuType.None;
				var campaignMenu = Game.OpenWindow("CAMPAIGN_MENU", new WidgetArgs
					{
						{ "onExit", () => menuType = MenuType.Singleplayer },
						{ "onStart", RemoveShellmapUI }
					});
				var campaignWorld = Game.OpenWindow("CAMPAIGN_WORLD", new WidgetArgs
				{
					{ "onExit", () => campaignMenu.Visible = true },
					{ "onStart", RemoveShellmapUI }
				});
			}
		}

		void RemoveShellmapUI()
		{
			rootMenu.Parent.RemoveChild(rootMenu);
		}
	}
}
