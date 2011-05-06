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
using System.Collections.Generic;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class ProgressBarWidget : Widget
	{
		public int Percentage = 0;
		public bool Indeterminate = false;
		int indeterminateTick = 0;
		
		public ProgressBarWidget() : base() {}
		protected ProgressBarWidget(ProgressBarWidget widget)
			: base(widget)
		{
			Percentage = widget.Percentage;
		}

		public override void DrawInner()
		{
			var rb = RenderBounds;
			WidgetUtils.DrawPanel("dialog3", rb);
			Rectangle barRect = Indeterminate ? 
				new Rectangle(rb.X + 2  + (int)((rb.Width - 4)*(-Math.Cos(Math.PI*2*indeterminateTick/100) + 1)*3/8), rb.Y + 2, (rb.Width - 4) / 4, rb.Height - 4) : 
				new Rectangle(rb.X + 2, rb.Y + 2, Percentage * (rb.Width - 4) / 100, rb.Height - 4);
			
			if (barRect.Width > 0)
				WidgetUtils.DrawPanel("dialog2", barRect);
		}
		
		public override void Tick()
		{
			if (Indeterminate && indeterminateTick++ >= 100)
				indeterminateTick = 0;
		}
		
		public override Widget Clone() { return new ProgressBarWidget(this); }
	}
}