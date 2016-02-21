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
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ClientTooltipRegionWidget : Widget
	{
		public readonly string Template;
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;
		OrderManager orderManager;
		int clientIndex;

		public ClientTooltipRegionWidget()
		{
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ClientTooltipRegionWidget(ClientTooltipRegionWidget other)
			: base(other)
		{
			Template = other.Template;
			TooltipContainer = other.TooltipContainer;
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
			orderManager = other.orderManager;
			clientIndex = other.clientIndex;
		}

		public override Widget Clone() { return new ClientTooltipRegionWidget(this); }

		public void Bind(OrderManager orderManager, int clientIndex)
		{
			this.orderManager = orderManager;
			this.clientIndex = clientIndex;
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;
			tooltipContainer.Value.SetTooltip(Template, new WidgetArgs() { { "orderManager", orderManager }, { "clientIndex", clientIndex } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;
			tooltipContainer.Value.RemoveTooltip();
		}
	}
}
