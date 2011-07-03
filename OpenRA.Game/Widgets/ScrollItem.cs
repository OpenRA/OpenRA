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
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class ScrollItemWidget : ButtonWidget
	{		
		public ScrollItemWidget()
			: base()
		{
			IsVisible = () => false;
			VisualHeight = 0;
		}
		
		protected ScrollItemWidget(ScrollItemWidget other)
			: base(other)
		{
			IsVisible = () => false;
			VisualHeight = 0;
		}
		
		public Func<bool> IsSelected = () => false;

		public override void Draw()
		{
			var state = IsSelected() ? "scrollitem-selected" : 
				RenderBounds.Contains(Viewport.LastMousePos) ? "scrollitem-hover" : 
				null;
			
			if (state != null)
				WidgetUtils.DrawPanel(state, RenderBounds);
		}
		
		public override Widget Clone() { return new ScrollItemWidget(this); }
				
		public static ScrollItemWidget Setup(ScrollItemWidget template, Func<bool> isSelected, Action onClick)
		{
			var w = template.Clone() as ScrollItemWidget;
			w.IsVisible = () => true;
			w.IsSelected = isSelected;
			w.OnClick = onClick;
			return w;
		}
	}
}