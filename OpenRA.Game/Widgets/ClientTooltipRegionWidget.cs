#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;

namespace OpenRA.Widgets
{
	public class ClientTooltipRegionWidget : Widget
	{
		public readonly string Template;
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;
		OrderManager orderManager;
		int clientIndex;

		public ClientTooltipRegionWidget() : base()
		{
			tooltipContainer = Lazy.New(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ClientTooltipRegionWidget(ClientTooltipRegionWidget other)
			: base(other)
		{
			Template = other.Template;
			TooltipContainer = other.TooltipContainer;
			tooltipContainer = Lazy.New(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
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
			tooltipContainer.Value.SetTooltip(Template, new WidgetArgs() {{"orderManager", orderManager}, {"clientIndex", clientIndex}});
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;
			tooltipContainer.Value.RemoveTooltip();
		}
	}
}
