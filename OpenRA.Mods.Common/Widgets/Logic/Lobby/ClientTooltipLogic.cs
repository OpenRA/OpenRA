#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ClientTooltipLogic : ChromeLogic
	{
		SpriteFont latencyFont;
		SpriteFont latencyPrefixFont;

		[ObjectCreator.UseCtor]
		public ClientTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, OrderManager orderManager, int clientIndex)
		{
			var admin = widget.Get<LabelWidget>("ADMIN");
			var adminFont = Game.Renderer.Fonts[admin.Font];

			var latency = widget.GetOrNull<LabelWidget>("LATENCY");
			if (latency != null)
				latencyFont = Game.Renderer.Fonts[latency.Font];

			var latencyPrefix = widget.GetOrNull<LabelWidget>("LATENCY_PREFIX");
			if (latencyPrefix != null)
				latencyPrefixFont = Game.Renderer.Fonts[latencyPrefix.Font];

			var ip = widget.Get<LabelWidget>("IP");
			var addressFont = Game.Renderer.Fonts[ip.Font];

			var location = widget.Get<LabelWidget>("LOCATION");
			var locationFont = Game.Renderer.Fonts[location.Font];

			var locationOffset = location.Bounds.Y;
			var addressOffset = ip.Bounds.Y;
			var latencyOffset = latency == null ? 0 : latency.Bounds.Y;
			var tooltipHeight = widget.Bounds.Height;

			var margin = widget.Bounds.Width;

			tooltipContainer.IsVisible = () => (orderManager.LobbyInfo.ClientWithIndex(clientIndex) != null);
			tooltipContainer.BeforeRender = () =>
			{
				var latencyPrefixSize = latencyPrefix == null ? 0 : latencyPrefix.Bounds.X + latencyPrefixFont.Measure(latencyPrefix.GetText() + " ").X;
				var locationWidth = locationFont.Measure(location.GetText()).X;
				var adminWidth = adminFont.Measure(admin.GetText()).X;
				var addressWidth = addressFont.Measure(ip.GetText()).X;
				var latencyWidth = latencyFont == null ? 0 : latencyPrefixSize + latencyFont.Measure(latency.GetText()).X;
				var width = Math.Max(locationWidth, Math.Max(adminWidth, Math.Max(addressWidth, latencyWidth)));
				widget.Bounds.Width = width + 2 * margin;
				if (latency != null)
					latency.Bounds.Width = widget.Bounds.Width;
				ip.Bounds.Width = widget.Bounds.Width;
				admin.Bounds.Width = widget.Bounds.Width;
				location.Bounds.Width = widget.Bounds.Width;

				ip.Bounds.Y = addressOffset;
				if (latency != null)
					latency.Bounds.Y = latencyOffset;
				location.Bounds.Y = locationOffset;
				widget.Bounds.Height = tooltipHeight;

				if (admin.IsVisible())
				{
					ip.Bounds.Y += admin.Bounds.Height;
					if (latency != null)
						latency.Bounds.Y += admin.Bounds.Height;
					location.Bounds.Y += admin.Bounds.Height;
					widget.Bounds.Height += admin.Bounds.Height;
				}

				if (latencyPrefix != null)
					latencyPrefix.Bounds.Y = latency.Bounds.Y;
				if (latency != null)
					latency.Bounds.X = latencyPrefixSize;
			};

			admin.IsVisible = () => orderManager.LobbyInfo.ClientWithIndex(clientIndex).IsAdmin;
			var client = orderManager.LobbyInfo.ClientWithIndex(clientIndex);
			var ping = orderManager.LobbyInfo.PingFromClient(client);
			if (latency != null)
			{
				latency.GetText = () => LobbyUtils.LatencyDescription(ping);
				latency.GetColor = () => LobbyUtils.LatencyColor(ping);
			}

			var address = LobbyUtils.GetExternalIP(clientIndex, orderManager);
			var cachedDescriptiveIP = LobbyUtils.DescriptiveIpAddress(address);
			ip.GetText = () => cachedDescriptiveIP;
			var cachedCountryLookup = GeoIP.LookupCountry(address);
			location.GetText = () => cachedCountryLookup;
		}
	}
}
