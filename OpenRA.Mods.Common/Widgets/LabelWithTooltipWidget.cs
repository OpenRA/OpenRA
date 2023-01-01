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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class LabelWithTooltipWidget : LabelWidget
	{
		public readonly string TooltipTemplate;
		public readonly string TooltipContainer;
		protected Lazy<TooltipContainerWidget> tooltipContainer;

		public Func<string> GetTooltipText = () => "";

		[ObjectCreator.UseCtor]
		public LabelWithTooltipWidget()
			: base()
		{
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected LabelWithTooltipWidget(LabelWithTooltipWidget other)
			: base(other)
		{
			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;

			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			GetTooltipText = other.GetTooltipText;
		}

		public override Widget Clone() { return new LabelWithTooltipWidget(this); }

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			if (GetTooltipText != null)
				tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", GetTooltipText } });
		}

		public override void MouseExited()
		{
			// Only try to remove the tooltip if we know it has been created
			// This avoids a crash if the widget (and the container it refers to) are being removed
			if (TooltipContainer != null && tooltipContainer.IsValueCreated)
				tooltipContainer.Value.RemoveTooltip();
		}
	}
}
