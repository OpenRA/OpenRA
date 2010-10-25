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

namespace OpenRA.Widgets
{
	public class CheckboxWidget : Widget
	{
		public string Text = "";
		public int baseLine = 1;
		public bool Bold = false;
		public Func<bool> Checked = () => false;
		
		public override void DrawInner( WorldRenderer wr )
		{
			var font = Bold ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var pos = RenderOrigin;
			var rect = RenderBounds;
			var check = new Rectangle(rect.Location,
					new Size(Bounds.Height, Bounds.Height));
			WidgetUtils.DrawPanel("dialog3", check);

			var textSize = font.Measure(Text);
			font.DrawText(Text,
				new float2(rect.Left + rect.Height * 1.5f, 
					pos.Y - baseLine + (Bounds.Height - textSize.Y)/2), Color.White);

			if (Checked())
				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage("checkbox", "checked"),
					new float2(rect.Left + 2, rect.Top + 2));
		}

		public override bool HandleInputInner(MouseInput mi) { return true; }

		public CheckboxWidget() : base() { }

		protected CheckboxWidget(CheckboxWidget other)
			: base(other)
		{
			Text = other.Text;
			Checked = other.Checked;
		}

		public override Widget Clone() { return new CheckboxWidget(this); }
	}
}