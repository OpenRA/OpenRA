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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class LabelWithTooltipWidget : LabelWidget
	{
		public readonly string TooltipTemplate;
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public Func<string> GetTooltipText = () => "";

		[ObjectCreator.UseCtor]
		public LabelWithTooltipWidget(World world)
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

			tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", GetTooltipText } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}
	}
}
