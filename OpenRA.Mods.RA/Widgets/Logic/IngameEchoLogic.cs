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
using System.Drawing;
using System.Linq;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngameEchoLogic
	{
		readonly Ruleset modRules;

		readonly ChatDisplayWidget chatOverlayDisplay;

		[ObjectCreator.UseCtor]
		public IngameEchoLogic(Widget widget, OrderManager orderManager, World world, Ruleset modRules)
		{
			this.modRules = modRules;

			chatOverlayDisplay = (ChatDisplayWidget)widget;

			Game.AddEchoLine += AddChatLine;
			Game.BeforeGameStart += UnregisterEvents;

			AddChatLine("Mic test");
			AddChatLine("Mic test");
			AddChatLine("Mic pass");
		}

		void UnregisterEvents()
		{
			Game.AddEchoLine -= AddChatLine;
			Game.BeforeGameStart -= UnregisterEvents;
		}

		public void AddChatLine(string text)
		{
			chatOverlayDisplay.AddLine(Color.White, null, text);
			Sound.PlayNotification(modRules, null, "Sounds", "ChatLine", null);
		}
	}
}
