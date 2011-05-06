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
using OpenRA.Widgets;
using OpenRA.Graphics;
using System.Drawing;
namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncMenuButtonWidget : ButtonWidget
	{
		public CncMenuButtonWidget() : base() { }
		protected CncMenuButtonWidget(CncMenuButtonWidget widget)	: base(widget) { }

		public Func<bool> IsDisabled = () => false;
		
		public override int2 ChildOrigin { get { return RenderOrigin; } }
		public override void DrawInner()
		{
			var font = Game.Renderer.BoldFont;
			var state = IsDisabled() ? "button-disabled" : 
						Depressed ? "button-pressed" : 
						RenderBounds.Contains(Viewport.LastMousePos) ? "button-hover" : 
						"button";
			
			WidgetUtils.DrawPanel(state, RenderBounds);
			var text = GetText();

			font.DrawText(text,
				new int2(RenderOrigin.X + UsableWidth / 2, RenderOrigin.Y + Bounds.Height / 2)
					- new int2(font.Measure(text).X / 2,
				font.Measure(text).Y / 2), IsDisabled() ? Color.Gray : Color.White);
		}
	}
}

