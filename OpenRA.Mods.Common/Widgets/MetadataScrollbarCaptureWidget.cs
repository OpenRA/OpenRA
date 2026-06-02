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

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	/// <summary>Invisible overlay on the scroll panel scrollbar so wide rows do not block thumb dragging.</summary>
	public class MetadataScrollbarCaptureWidget : Widget
	{
		readonly ScrollPanelWidget panel;

		public MetadataScrollbarCaptureWidget(ScrollPanelWidget panel)
		{
			this.panel = panel;
			IgnoreMouseOver = true;
		}

		protected MetadataScrollbarCaptureWidget(MetadataScrollbarCaptureWidget other)
			: base(other)
		{
			panel = other.panel;
			IgnoreMouseOver = true;
		}

		public override MetadataScrollbarCaptureWidget Clone() => new(this);

		public override void Tick()
		{
			if (panel.ScrollBar == ScrollBar.Hidden)
				return;

			var width = panel.ScrollBar == ScrollBar.Right ? panel.ScrollbarWidth : 0;
			var x = panel.ScrollBar == ScrollBar.Right ? panel.Bounds.Width - panel.ScrollbarWidth : 0;
			Bounds = new WidgetBounds(x, 0, width > 0 ? width : panel.ScrollbarWidth, panel.Bounds.Height);
		}

		public override bool HandleMouseInput(MouseInput mi) => panel.HandleMouseInput(mi);
	}
}
