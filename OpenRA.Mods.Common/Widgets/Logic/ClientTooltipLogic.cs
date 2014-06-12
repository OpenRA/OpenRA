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
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
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

			var latencyPrefix = widget.Get<LabelWidget>("LATENCY_PREFIX");
			var latencyPrefixFont = Game.Renderer.Fonts[latencyPrefix.Font];

			var ip = widget.Get<LabelWidget>("IP");
			var addressFont = Game.Renderer.Fonts[ip.Font];

			var location = widget.Get<LabelWidget>("LOCATION");
			var locationFont = Game.Renderer.Fonts[location.Font];

			var locationOffset = location.Bounds.Y;
			var addressOffset = ip.Bounds.Y;
			var latencyOffset = latency.Bounds.Y;
			var tooltipHeight = widget.Bounds.Height;

			var margin = widget.Bounds.Width;

			tooltipContainer.IsVisible = () => (orderManager.LobbyInfo.ClientWithIndex(clientIndex) != null);
			tooltipContainer.BeforeRender = () =>
			{
				var latencyPrefixSize = latencyPrefix.Bounds.X + latencyPrefixFont.Measure(latencyPrefix.GetText() + " ").X;
				var width = Math.Max(locationFont.Measure(location.GetText()).X, Math.Max(adminFont.Measure(admin.GetText()).X,
					Math.Max(addressFont.Measure(ip.GetText()).X, latencyPrefixSize + latencyFont.Measure(latency.GetText()).X)));
				widget.Bounds.Width = width + 2 * margin;
				latency.Bounds.Width = widget.Bounds.Width;
				ip.Bounds.Width = widget.Bounds.Width;
				admin.Bounds.Width = widget.Bounds.Width;
				location.Bounds.Width = widget.Bounds.Width;

				ip.Bounds.Y = addressOffset;
				latency.Bounds.Y = latencyOffset;
				location.Bounds.Y = locationOffset;
				widget.Bounds.Height = tooltipHeight;

				if (admin.IsVisible())
				{
					ip.Bounds.Y += admin.Bounds.Height;
					latency.Bounds.Y += admin.Bounds.Height;
					location.Bounds.Y += admin.Bounds.Height;
					widget.Bounds.Height += admin.Bounds.Height;
				}

				latencyPrefix.Bounds.Y = latency.Bounds.Y;
				latency.Bounds.X = latencyPrefixSize;
			};

			admin.IsVisible = () => orderManager.LobbyInfo.ClientWithIndex(clientIndex).IsAdmin;
			var client = orderManager.LobbyInfo.ClientWithIndex(clientIndex);
			var ping = orderManager.LobbyInfo.PingFromClient(client);
			latency.GetText = () => LobbyUtils.LatencyDescription(ping);
			latency.GetColor = () => LobbyUtils.LatencyColor(ping);
			var address = orderManager.LobbyInfo.ClientWithIndex(clientIndex).IpAddress;
			if (address == "127.0.0.1" && UPnP.NatDevice != null)
				address = UPnP.NatDevice.GetExternalIP().ToString();
			var cachedDescriptiveIP = LobbyUtils.DescriptiveIpAddress(address);
			ip.GetText = () => cachedDescriptiveIP;
			var cachedCountryLookup = LobbyUtils.LookupCountry(address);
			location.GetText = () => cachedCountryLookup;
		}
	}
}
