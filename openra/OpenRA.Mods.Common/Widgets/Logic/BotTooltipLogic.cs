#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class BotTooltipLogic : ChromeLogic
	{
		[FluentReference("name")]
		const string BotManagedBy = "label-bot-managed-by-tooltip";

		[ObjectCreator.UseCtor]
		public BotTooltipLogic(OrderManager orderManager, Widget widget, Session.Client client)
		{
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var nameFont = Game.Renderer.Fonts[nameLabel.Font];
			var controller = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.Index == client.BotControllerClientIndex);
			if (controller != null)
				nameLabel.GetText = () => FluentProvider.GetMessage(BotManagedBy, "name", controller.Name);

			widget.Bounds.Width = nameFont.Measure(nameLabel.GetText()).X + 2 * nameLabel.Bounds.Left;
		}
	}
}
