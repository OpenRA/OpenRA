#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Widgets;
using OpenRA.Network;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ClientTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public ClientTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, OrderManager orderManager, int clientIndex)
		{
			var admin = widget.Get<LabelWidget>("ADMIN");
			var adminFont = Game.Renderer.Fonts[admin.Font];

			var latency = widget.Get<LabelWidget>("LATENCY");
			var latencyFont = Game.Renderer.Fonts[latency.Font];

			var ip = widget.Get<LabelWidget>("IP");
			var ipFont = Game.Renderer.Fonts[ip.Font];

			var ipOffset = ip.Bounds.Y;
			var latencyOffset = latency.Bounds.Y;
			var tooltipHeight = widget.Bounds.Height;

			var margin = widget.Bounds.Width;

			tooltipContainer.IsVisible = () => (orderManager.LobbyInfo.ClientWithIndex(clientIndex) != null);
			tooltipContainer.BeforeRender = () =>
			{
				var width = Math.Max(adminFont.Measure(admin.GetText()).X, Math.Max(ipFont.Measure(ip.GetText()).X, latencyFont.Measure(latency.GetText()).X));
				widget.Bounds.Width = width + 2*margin;
				latency.Bounds.Width = widget.Bounds.Width;
				ip.Bounds.Width = widget.Bounds.Width;
				admin.Bounds.Width = widget.Bounds.Width;

				ip.Bounds.Y = ipOffset;
				latency.Bounds.Y = latencyOffset;
				widget.Bounds.Height = tooltipHeight;

				if (admin.IsVisible())
				{
					ip.Bounds.Y += admin.Bounds.Height;
					latency.Bounds.Y += admin.Bounds.Height;
					widget.Bounds.Height += admin.Bounds.Height;
				}
			};

			admin.IsVisible = () => orderManager.LobbyInfo.ClientWithIndex(clientIndex).IsAdmin;
			latency.GetText = () => "Latency: {0}".F(LobbyUtils.LatencyDescription(orderManager.LobbyInfo.ClientWithIndex(clientIndex).Latency));
			ip.GetText = () => LobbyUtils.DescriptiveIpAddress(orderManager.LobbyInfo.ClientWithIndex(clientIndex).IpAddress);
		}
	}
}

