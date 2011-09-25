#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Widgets
{
	public class BackgroundWidget : Widget
	{
		public readonly string Background = "dialog";
		public readonly bool ClickThrough = false;

		public override void Draw()
		{
			WidgetUtils.DrawPanel(Background, RenderBounds);
		}

		public BackgroundWidget() : base() { }

		public override bool HandleMouseInput(MouseInput mi)
		{
			return !ClickThrough;
		}

		protected BackgroundWidget(BackgroundWidget other)
			: base(other)
		{
			Background = other.Background;
			ClickThrough = other.ClickThrough;
		}

		public override Widget Clone() { return new BackgroundWidget(this); }
	}
}