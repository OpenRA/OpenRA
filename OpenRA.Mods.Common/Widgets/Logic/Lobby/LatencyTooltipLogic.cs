#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public class LatencyTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public LatencyTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, OrderManager orderManager, Session.Client client)
		{
			var latencyPrefix = widget.Get<LabelWidget>("LATENCY_PREFIX");
			var latencyPrefixFont = Game.Renderer.Fonts[latencyPrefix.Font];
			var latency = widget.Get<LabelWidget>("LATENCY");
			var latencyFont = Game.Renderer.Fonts[latency.Font];
			var rightMargin = (int)widget.LayoutWidth;

			latency.Left = (int)latencyPrefix.LayoutX + latencyPrefixFont.Measure(latencyPrefix.Text + " ").X;

			widget.IsVisible = () => client != null;
			tooltipContainer.BeforeRender = () =>
			{
				if (widget.IsVisible())
					widget.Width = (int)latency.LayoutX + latencyFont.Measure(latency.GetText()).X + rightMargin;
			};

			var ping = orderManager.LobbyInfo.PingFromClient(client);
			latency.GetText = () => LobbyUtils.LatencyDescription(ping);
			latency.GetColor = () => LobbyUtils.LatencyColor(ping);
		}
	}
}
