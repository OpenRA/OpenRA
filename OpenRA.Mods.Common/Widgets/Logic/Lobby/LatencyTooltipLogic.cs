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
	public class LatencyTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public LatencyTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, Session.Client client)
		{
			var latencyPrefix = widget.Get<LabelWidget>("LATENCY_PREFIX");
			var latencyPrefixFont = Game.Renderer.Fonts[latencyPrefix.Font];
			var latency = widget.Get<LabelWidget>("LATENCY");
			var latencyFont = Game.Renderer.Fonts[latency.Font];
			var rightMargin = widget.Bounds.Width;

			latency.Bounds.X = latencyPrefix.Bounds.X + latencyPrefixFont.Measure(latencyPrefix.Text + " ").X;

			widget.IsVisible = () => client != null;
			tooltipContainer.BeforeRender = () =>
			{
				if (widget.IsVisible())
					widget.Bounds.Width = latency.Bounds.X + latencyFont.Measure(latency.GetText()).X + rightMargin;
			};

			latency.GetText = () => LobbyUtils.LatencyDescription(client);
			latency.GetColor = () => LobbyUtils.LatencyColor(client);
		}
	}
}
