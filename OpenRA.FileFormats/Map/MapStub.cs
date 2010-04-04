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

using System.Collections.Generic;
using System.Drawing;
using System;

namespace OpenRA.FileFormats
{
	public class MapStub
	{
		public IFolder Package;

		// Yaml map data
		public string Uid;
		public string Title;
		public string Description;
		public string Author;
		public int PlayerCount;
		public string Tileset;
		
		public int2 TopLeft;
		public int2 BottomRight;
		public int Width {get {return BottomRight.X - TopLeft.X;}}
		public int Height {get {return BottomRight.Y - TopLeft.Y;}}
		public Lazy<Bitmap> Preview;
		
		static List<string> Fields = new List<string>() {
			"Uid", "Title", "Description", "Author", "PlayerCount", "Tileset", "TopLeft", "BottomRight"
		};
		
		public MapStub() {}
		
		public MapStub(IFolder package)
		{			
			Package = package;
			var yaml = MiniYaml.FromStream(Package.GetContent("map.yaml"));
			FieldLoader.LoadFields(this,yaml,Fields);
			
			Preview = Lazy.New(
				() => {return new Bitmap(Package.GetContent("preview.png"));}
			);
		}
		
		public Rectangle PreviewBounds(Rectangle container)
		{
			float scale = Math.Min(container.Width*1.0f/Width,container.Height*1.0f/Height);
						
			var size = Math.Max(Width, Height);
			var dw = (int)(scale*(size - Width)) / 2;
			var dh = (int)(scale*(size - Height)) / 2;

			return new Rectangle(container.X + dw, container.Y + dh, (int)(Width*scale), (int)(Height*scale));
		}
	}
}
