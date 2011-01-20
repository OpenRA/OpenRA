#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using System.Collections.Generic;

namespace OpenRA.Widgets
{
	public class ProgressBarWidget : Widget
	{
		public int Percentage = 0;
		
		public ProgressBarWidget() : base() {}
		protected ProgressBarWidget(ProgressBarWidget widget)
			: base(widget)
		{
			Percentage = widget.Percentage;
		}

		public override void DrawInner()
		{
			WidgetUtils.DrawPanel("dialog3", RenderBounds);
			
			var barRect = new Rectangle(RenderBounds.X + 2, RenderBounds.Y + 2, Percentage * (RenderBounds.Width - 4) / 100, RenderBounds.Height - 4);
			if (barRect.Width > 0)
				WidgetUtils.DrawPanel("dialog2", barRect);
		}
		
		public override Widget Clone() { return new ProgressBarWidget(this); }
	}
}