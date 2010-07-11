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
using System.Linq;
using System.Drawing;

namespace OpenRA.FileFormats
{
	public class MapStub
	{
		public IFolder Package;
		
		// Yaml map data
		public string Uid;
		public bool Selectable;

		public string Title;
		public string Description;
		public string Author;
		public int PlayerCount;
		public string Tileset;
		public Dictionary<string, int2> Waypoints = new Dictionary<string, int2>();
		public IEnumerable<int2> SpawnPoints { get { return Waypoints.Select(kv => kv.Value); } }
		
		public int2 TopLeft;
		public int2 BottomRight;
		public int Width { get { return BottomRight.X - TopLeft.X; } }
		public int Height { get { return BottomRight.Y - TopLeft.Y; } }
		public Lazy<Bitmap> Preview;

		static List<string> Fields = new List<string>() {
			"Selectable", "Title", "Description", "Author", "PlayerCount", "Tileset", "TopLeft", "BottomRight"
		};

		public MapStub() { }

		public MapStub(IFolder package)
		{
			Package = package;
			var yaml = MiniYaml.FromStream(Package.GetContent("map.yaml"));
			FieldLoader.LoadFields(this, yaml, Fields);
			
			// Waypoints
			foreach (var wp in yaml["Waypoints"].Nodes)
			{
				string[] loc = wp.Value.Value.Split(',');
				Waypoints.Add(wp.Key, new int2(int.Parse(loc[0]), int.Parse(loc[1])));
			}
			
			Preview = Lazy.New(
				() => { return new Bitmap(Package.GetContent("preview.png")); }
			);

			Uid = Package.GetContent("map.uid").ReadAllText();
		}
		
		public int2 ConvertToPreview(int2 point, Rectangle container)
		{
			float scale = Math.Min(container.Width * 1.0f / Width, container.Height * 1.0f / Height);

			var size = Math.Max(Width, Height);
			var dw = (int)(scale * (size - Width)) / 2;
			var dh = (int)(scale * (size - Height)) / 2;

			return new int2(container.X + dw + (int)(scale*(point.X - TopLeft.X)) , container.Y + dh + (int)(scale*(point.Y - TopLeft.Y)));
		}
		
		public Rectangle PreviewBounds(Rectangle container)
		{
			float scale = Math.Min(container.Width * 1.0f / Width, container.Height * 1.0f / Height);

			var size = Math.Max(Width, Height);
			var dw = (int)(scale * (size - Width)) / 2;
			var dh = (int)(scale * (size - Height)) / 2;

			return new Rectangle(container.X + dw, container.Y + dh, (int)(Width * scale), (int)(Height * scale));
		}
	}
}
