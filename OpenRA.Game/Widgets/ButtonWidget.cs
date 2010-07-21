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

namespace OpenRA.Widgets
{
	public class ButtonWidget : Widget
	{
		public string Text = "";
		public bool Bold = false;
		public bool Depressed = false;
		public int VisualHeight = 1;
		public Func<string> GetText;

		public ButtonWidget()
			: base()
		{
			GetText = () => { return Text; };
		}

		protected ButtonWidget(ButtonWidget widget)
			: base(widget)
		{
			Text = widget.Text;
			Depressed = widget.Depressed;
			VisualHeight = widget.VisualHeight;
			GetText = widget.GetText;
		}

		public override bool LoseFocus(MouseInput mi)
		{
			Depressed = false;
			return base.LoseFocus(mi);
		}

		public override bool HandleInputInner(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;

			// Only fire the onMouseUp order if we successfully lost focus, and were pressed
			if (Focused && mi.Event == MouseInputEvent.Up)
			{
				var wasPressed = Depressed;
				return (LoseFocus(mi) && wasPressed);
			}

			if (mi.Event == MouseInputEvent.Down)
				Depressed = true;
			else if (mi.Event == MouseInputEvent.Move && Focused)
				Depressed = RenderBounds.Contains(mi.Location.X, mi.Location.Y);

			return Depressed;
		}

		public override void DrawInner(World world)
		{
			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var stateOffset = (Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);
			WidgetUtils.DrawPanel(Depressed ? "dialog3" : "dialog2", RenderBounds);

			var text = GetText();
			font.DrawText(text,
				new int2(RenderOrigin.X + Bounds.Width / 2, RenderOrigin.Y + Bounds.Height / 2)
					- new int2(font.Measure(text).X / 2,
				font.Measure(text).Y / 2) + stateOffset, Color.White);
		}

		public override Widget Clone() { return new ButtonWidget(this); }

	}
}