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
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class ImageWidget : Widget
	{
		public string ImageCollection = "";
		public string ImageName = "";
		public Func<string> GetImageName;
		public Func<string> GetImageCollection;

		public ImageWidget()
			: base()
		{
			GetImageName = () => { return ImageName; };
			GetImageCollection = () => { return ImageCollection; };
		}

		protected ImageWidget(ImageWidget other)
			: base(other)
		{
			ImageName = other.ImageName;
			GetImageName = other.GetImageName;
			ImageCollection = other.ImageCollection;
			GetImageCollection = other.GetImageCollection;
		}

		public override Widget Clone() { return new ImageWidget(this); }

		public override void DrawInner(World world)
		{
			var name = GetImageName();
			var collection = GetImageCollection();
			WidgetUtils.DrawRGBA(
				ChromeProvider.GetImage(Game.chrome.renderer, collection, name), 
				RenderOrigin);
		}
	}
}
