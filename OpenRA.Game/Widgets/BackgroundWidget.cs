using System.Drawing;
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

namespace OpenRA.Widgets
{
	class BackgroundWidget : Widget
	{
		public readonly string Background = "dialog";
		
		public override void DrawInner(World world)
		{
			var pos = DrawPosition();
			var rect = new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height);
			WidgetUtils.DrawPanel(Background, rect);
		}
		
		public BackgroundWidget() : base() { }

		public BackgroundWidget(Widget other)
			: base(other)
		{
			Background = (other as BackgroundWidget).Background;
		}

		public override Widget Clone() { return new BackgroundWidget(this); }
	}
}