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

		public static new void ShowDropDown<T>(Widget w, IEnumerable<T> ts, Func<T, int, LabelWidget> ft)
		{
			var dropDown = new ScrollPanelWidget();
			dropDown.Bounds = new Rectangle(w.RenderOrigin.X, w.RenderOrigin.Y + w.Bounds.Height, w.Bounds.Width, 100);
			dropDown.ItemSpacing = 1;
			dropDown.Background = "panel-black";

			List<LabelWidget> items = new List<LabelWidget>();
			List<Widget> dismissAfter = new List<Widget>();
			foreach (var t in ts)
			{
				var ww = ft(t, dropDown.Bounds.Width - dropDown.ScrollbarWidth);
				dismissAfter.Add(ww);
				ww.OnMouseMove = mi => items.Do(lw =>
				{
					lw.Background = null;
					ww.Background = "button-hover";
				});
	
				dropDown.AddChild(ww);
				items.Add(ww);
			}
			
			dropDown.Bounds.Height = Math.Min(150, dropDown.ContentHeight);
			ShowDropPanel(w, dropDown, dismissAfter, () => true);
		}
	}
}

