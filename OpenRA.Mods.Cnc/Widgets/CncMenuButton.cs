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
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Widgets;
using System.Reflection;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncDropDownButtonWidget : DropDownButtonWidget
	{
		public CncDropDownButtonWidget() : base() { }
		protected CncDropDownButtonWidget(CncDropDownButtonWidget other) : base(other) { }
		public override Widget Clone() { return new CncDropDownButtonWidget(this); }
		
		Widget panel;
		Widget fullscreenMask;
		
		public void RemovePanel()
		{
			Widget.RootWidget.RemoveChild(fullscreenMask);
			Widget.RootWidget.RemoveChild(panel);
			Game.BeforeGameStart -= RemovePanel;
			panel = fullscreenMask = null;
		}
		
		public void DisplayPanel(Widget p)
		{
			if (panel != null)
				throw new InvalidOperationException("Attempted to attach a panel to an open dropdown");
			panel = p;
			
			// Mask to prevent any clicks from being sent to other widgets
			fullscreenMask = new ContainerWidget();
			fullscreenMask.Bounds = new Rectangle(0, 0, Game.viewport.Width, Game.viewport.Height);
			Widget.RootWidget.AddChild(fullscreenMask);
			Game.BeforeGameStart += RemovePanel;

			fullscreenMask.OnMouseDown = mi =>
			{
				RemovePanel();
				return true;
			};
			fullscreenMask.OnMouseUp = mi => true;

			var oldBounds = panel.Bounds;
			panel.Bounds = new Rectangle(RenderOrigin.X, RenderOrigin.Y + Bounds.Height, oldBounds.Width, oldBounds.Height);
			Widget.RootWidget.AddChild(panel);
		}
	}
}

