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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class MapStub
	{
		public readonly IFolder Package;
		
		// Yaml map data
		public readonly string Uid;
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
	
		static List<string> Fields = new List<string>() {
			"Selectable", "Title", "Description", "Author", "PlayerCount", "Tileset", "TopLeft", "BottomRight"
		};

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

			Uid = Package.GetContent("map.uid").ReadAllText();
		}
	}
}
