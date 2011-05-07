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
		public Func<bool> IsDisabled = () => false;
		public Action OnClick = () => {};
		
		public CncMenuButtonWidget()
			: base()
		{
			OnMouseUp = mi => { if (!IsDisabled()) OnClick(); return true; };
		}
		
		protected CncMenuButtonWidget(CncMenuButtonWidget widget)
			: base(widget)
		{
			OnMouseUp = mi => { if (!IsDisabled()) OnClick(); return true; };
		}
		
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
	
	public class CncCheckboxWidget : CncMenuButtonWidget
	{
		public CncCheckboxWidget()
			: base() { }
		protected CncCheckboxWidget(CncCheckboxWidget widget)
			: base(widget) { }
		
		public Func<bool> IsChecked = () => false;
		public int baseLine = 1;

		public override void DrawInner()
		{
			var state = IsDisabled() ? "button-disabled" : 
				Depressed ? "button-pressed" : 
				RenderBounds.Contains(Viewport.LastMousePos) ? "button-hover" : 
				"button";
			
			var font = Game.Renderer.BoldFont;
			var rect = RenderBounds;
			WidgetUtils.DrawPanel(state, new Rectangle(rect.Location, new Size(Bounds.Height, Bounds.Height)));

			var textSize = font.Measure(Text);
			font.DrawText(Text,
				new float2(rect.Left + rect.Height * 1.5f, RenderOrigin.Y - baseLine + (Bounds.Height - textSize.Y)/2), Color.White);

			if (IsChecked())
				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage("checkbox", "checked"),
					new float2(rect.Left + 2, rect.Top + 2));
		}
	}
}

