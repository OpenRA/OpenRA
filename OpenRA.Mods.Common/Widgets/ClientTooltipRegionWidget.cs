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

using System;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ClientTooltipRegionWidget : Widget
	{
		public readonly string TooltipContainer;
		readonly Lazy<TooltipContainerWidget> tooltipContainer;

		public string Template;

		OrderManager orderManager;
		WorldRenderer worldRenderer;
		Session.Client client;

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
			worldRenderer = other.worldRenderer;
			client = other.client;
		}

		public override Widget Clone() { return new ClientTooltipRegionWidget(this); }

		public void Bind(OrderManager orderManager, WorldRenderer worldRenderer, Session.Client client)
		{
			this.orderManager = orderManager;
			this.worldRenderer = worldRenderer;
			this.client = client;
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.SetTooltip(Template, new WidgetArgs()
			{
				{ "orderManager", orderManager },
				{ "worldRenderer", worldRenderer },
				{ "client", client }
			});
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}
	}
}
