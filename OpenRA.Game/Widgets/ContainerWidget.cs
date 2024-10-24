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

namespace OpenRA.Widgets
{
	public class ContainerWidget : Widget
	{
		public readonly bool ClickThrough = true;

		public ContainerWidget() { IgnoreMouseOver = true; }
		public ContainerWidget(ContainerWidget other)
			: base(other)
		{
			ClickThrough = other.ClickThrough;
			IgnoreMouseOver = true;
		}

		public override string GetCursor(int2 pos) { return null; }
		public override Widget Clone() { return new ContainerWidget(this); }

		public override bool HandleMouseInput(MouseInput mi)
		{
			return !ClickThrough && EventBounds.Contains(mi.Location);
		}
	}
}
