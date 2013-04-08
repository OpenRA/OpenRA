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
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class ToggleButtonWidget : ButtonWidget
	{
		public readonly string TooltipTemplate = "BUTTON_TOOLTIP";
		public readonly string TooltipText;
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public ToggleButtonWidget()
			: base()
		{
			tooltipContainer = Lazy.New(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ToggleButtonWidget(ToggleButtonWidget other)
			: base(other)
		{
			TooltipTemplate = other.TooltipTemplate;
			TooltipText = other.TooltipText;
			TooltipContainer = other.TooltipContainer;
			tooltipContainer = Lazy.New(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null) return;
			tooltipContainer.Value.SetTooltip(TooltipTemplate,
				new WidgetArgs() {{ "button", this }});
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null) return;
			tooltipContainer.Value.RemoveTooltip();
		}
	}
}
