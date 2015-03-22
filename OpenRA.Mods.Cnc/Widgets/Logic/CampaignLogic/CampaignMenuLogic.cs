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

using OpenRA.Graphics;
using OpenRA.Mods.Cnc;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic.CampaignLogic
{
	public class CampaignMenuLogic
	{
		[ObjectCreator.UseCtor]
		public CampaignMenuLogic(Widget widget, Action onStart, Action onExit)
		{
			var continueButtonGDI = widget.Get<ButtonWidget>("CONTINUE_GDI_BUTTON");
			var continueButtonNOD = widget.Get<ButtonWidget>("CONTINUE_NOD_BUTTON");
			var newButton = widget.Get<ButtonWidget>("NEW_BUTTON");
			var backButton = widget.Get<ButtonWidget>("BACK_BUTTON");

			if (CampaignProgress.GetGdiProgress().Length == 0)
				continueButtonGDI.Disabled = true;

            if (CampaignProgress.GetNodProgress().Length == 0)
				continueButtonNOD.Disabled = true;

			backButton.OnClick = () =>
			{
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};

			newButton.OnClick = () =>
			{
				Game.OpenWindow("CAMPAIGN_FACTION", new WidgetArgs
				{
					{ "onExit", () => { } },
					{ "onStart", () => { widget.Parent.RemoveChild(widget); } }
				});
			};

			continueButtonGDI.OnClick = () =>
			{
				CampaignWorldLogic.Campaign = "GDI Campaign";
				Game.OpenWindow("CAMPAIGN_WORLD", new WidgetArgs
					{
						{ "onExit", () => { } },
						{ "onStart", () => widget.Parent.RemoveChild(widget) }
					});
			};

			continueButtonNOD.OnClick = () =>
			{
				CampaignWorldLogic.Campaign = "Nod Campaign";
				Game.OpenWindow("CAMPAIGN_WORLD", new WidgetArgs
					{
						{ "onExit", () => { } },
						{ "onStart", () => widget.Parent.RemoveChild(widget) }
					});
			};
		}
	}
}
