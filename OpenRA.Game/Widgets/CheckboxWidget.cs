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

using System.Drawing;
using System;

namespace OpenRA.Widgets
{
	class CheckboxWidget : Widget
	{
		public string Text = "";
		public Func<bool> Checked = () => {return false;};
		
		public override void Draw(World world)
		{
			if (!IsVisible())
			{
				base.Draw(world);
				return;
			}

			WidgetUtils.DrawPanel("dialog3",
				new Rectangle(Bounds.Location,
					new Size(Bounds.Height, Bounds.Height)),
					() => { });

			Game.chrome.renderer.BoldFont.DrawText(Text,
				new float2(Bounds.Left + Bounds.Height * 2, Bounds.Top), Color.White);

			if (Checked())
			{
				Game.chrome.lineRenderer.FillRect(
					new RectangleF( 
						Game.viewport.Location.X + Bounds.Left + 4, 
						Game.viewport.Location.Y + Bounds.Top + 5,
						Bounds.Height - 9,
						Bounds.Height - 9), 
						Color.White);
				Game.chrome.lineRenderer.Flush();
			}

			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
			
			base.Draw(world);
		}
	}
}