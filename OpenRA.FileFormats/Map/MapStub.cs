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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

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
		public string Preview;
		public string Tileset;
		
		public int2 TopLeft;
		public int2 BottomRight;
		public int Width {get {return BottomRight.X - TopLeft.X;}}
		public int Height {get {return BottomRight.Y - TopLeft.Y;}}
		
		static List<string> Fields = new List<string>() {
			"Uid", "Title", "Description", "Author", "PlayerCount", "Tileset", "Preview", "TopLeft", "BottomRight"
		};
		
		public MapStub() {}
		
		public MapStub(IFolder package)
		{			
			Package = package;
			var yaml = MiniYaml.FromStream(Package.GetContent("map.yaml"));
						
			FieldLoader.LoadFields(this,yaml,Fields);
		}
	}
}
