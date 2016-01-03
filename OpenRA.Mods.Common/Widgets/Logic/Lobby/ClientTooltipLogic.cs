#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Net;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ClientTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ClientTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, OrderManager orderManager, int clientIndex)
		{
			var admin = widget.Get<LabelWidget>("ADMIN");
			var latency = widget.GetOrNull<LabelWidget>("LATENCY");
			var latencyPrefix = widget.GetOrNull<LabelWidget>("LATENCY_PREFIX");
			var ip = widget.Get<LabelWidget>("IP");
			var location = widget.Get<LabelWidget>("LOCATION");

			var locationOffset = location.Bounds.Y;
			var addressOffset = ip.Bounds.Y;
			var latencyOffset = latency == null ? 0 : latency.Bounds.Y;
			var tooltipHeight = widget.Bounds.Height;

			var margin = widget.Bounds.Width;

			tooltipContainer.IsVisible = () => (orderManager.LobbyInfo.ClientWithIndex(clientIndex) != null);
			tooltipContainer.BeforeRender = () =>
			{
				var latencyPrefixSize = latencyPrefix == null ? 0 : latencyPrefix.Bounds.X + latencyPrefix.MeasureText(latencyPrefix.GetText() + " ").X;
				var locationWidth = location.MeasureText(location.GetText()).X;
				var adminWidth = admin.MeasureText(admin.GetText()).X;
				var ipWidth = ip.MeasureText(ip.GetText()).X;
				var latencyWidth = latency == null ? 0 : latencyPrefixSize + latency.MeasureText(latency.GetText()).X;
				var width = Math.Max(locationWidth, Math.Max(adminWidth, Math.Max(ipWidth, latencyWidth)));
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
