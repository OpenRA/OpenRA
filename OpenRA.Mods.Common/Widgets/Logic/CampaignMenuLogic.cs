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
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class CampaignMenuLogic
	{
		[ObjectCreator.UseCtor]
		public CampaignMenuLogic(Widget widget, Action onStart, Action onExit)
		{
			var newButton = widget.Get<ButtonWidget>("NEW_BUTTON");
			var backButton = widget.Get<ButtonWidget>("BACK_BUTTON");

			var count = 0;
			foreach (var f in CampaignProgress.Factions)
			{
				count++;
				var continueButton = widget.Get<ButtonWidget>("CONTINUE_" + f.ToUpper() + "_BUTTON");

				if (CampaignProgress.GetMission(f).Length == 0)
					continueButton.Disabled = true;

				continueButton.OnClick = () =>
				{
					CampaignWorldLogic.Campaign = f + " Campaign";
					Game.OpenWindow("CAMPAIGN_WORLD", new WidgetArgs
					{
						{ "onExit", () => { } },
						{ "onStart", () => widget.Parent.RemoveChild(widget) }
					});
				};
			}

			backButton.OnClick = () =>
			{
				CampaignProgress.ResetSaveProgressFlag();
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
		}
	}
}
