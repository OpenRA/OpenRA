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
		public Func<bool> Checked = () => {return false;};
		
		public override void DrawInner(World world)
		{
			var pos = DrawPosition();
			var rect = new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height);
			WidgetUtils.DrawPanel("dialog3", new Rectangle(rect.Location,
					new Size(Bounds.Height, Bounds.Height)));

			Game.chrome.renderer.BoldFont.DrawText(Text,
				new float2(rect.Left + rect.Height * 2, rect.Top), Color.White);

			if (Checked())
			{
				Game.chrome.renderer.RgbaSpriteRenderer.Flush();

				Game.chrome.lineRenderer.FillRect(
					new RectangleF( 
						Game.viewport.Location.X + rect.Left + 4, 
						Game.viewport.Location.Y + rect.Top + 5,
						rect.Height - 9,
						rect.Height - 9), 
						Color.White);
				Game.chrome.lineRenderer.Flush();
			}
		}

		public CheckboxWidget() : base() { }

		public CheckboxWidget(Widget other)
			: base(other)
		{
			Text = (other as CheckboxWidget).Text;
			Checked = (other as CheckboxWidget).Checked;
		}

		public override Widget Clone() { return new CheckboxWidget(this); }
	}
}