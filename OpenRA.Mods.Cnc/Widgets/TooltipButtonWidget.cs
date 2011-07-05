#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class TooltipButtonWidget : ButtonWidget
	{
		public readonly string TooltipTemplate = "BUTTON_TOOLTIP";
		public readonly string TooltipText;
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public TooltipButtonWidget()
			: base()
		{
			tooltipContainer = new Lazy<TooltipContainerWidget>(() =>
				Widget.RootWidget.GetWidget<TooltipContainerWidget>(TooltipContainer));
		}

		protected TooltipButtonWidget(TooltipButtonWidget other)
			: base(other)
		{
			TooltipTemplate = other.TooltipTemplate;
			TooltipText = other.TooltipText;
			TooltipContainer = other.TooltipContainer;
			tooltipContainer = new Lazy<TooltipContainerWidget>(() =>
				Widget.RootWidget.GetWidget<TooltipContainerWidget>(TooltipContainer));
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.SetTooltip(
				Widget.LoadWidget(TooltipTemplate, null, new WidgetArgs() {{ "button", this }}));
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null) return;
			tooltipContainer.Value.RemoveTooltip();
		}
	}
}