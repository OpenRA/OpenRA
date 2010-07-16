#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Drawing;

namespace OpenRA.Widgets
{
	class CheckboxWidget : Widget
	{
		public string Text = "";
		public int baseLine = 1;
		public bool Bold = false;
		public Func<bool> Checked = () => {return false;};
		
		public override void DrawInner(World world)
		{
			var font = (Bold) ? Game.chrome.renderer.BoldFont : Game.chrome.renderer.RegularFont;
			var pos = RenderOrigin;
			var rect = RenderBounds;
			var check = new Rectangle(rect.Location,
					new Size(Bounds.Height, Bounds.Height));
			WidgetUtils.DrawPanel("dialog3", check);

			var textSize = font.Measure(Text);
			font.DrawText(Text,
				new float2(rect.Left + rect.Height * 1.5f, pos.Y - baseLine + (Bounds.Height - textSize.Y)/2), Color.White);

			if (Checked())
			{				
				Game.chrome.renderer.RgbaSpriteRenderer.Flush();
				WidgetUtils.FillRectWithColor(check.InflateBy(-4,-5,-4,-5),Color.White);
				Game.chrome.lineRenderer.Flush();
			}
		}

		public override bool HandleInput(MouseInput mi) { return true; }

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