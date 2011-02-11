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
using System.Security.Cryptography;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA
{
	public class MapStub
	{
		protected IFolder Container;
		public string Path {get; protected set;}
		
		// Yaml map data
		public string Uid { get; protected set; }
		[FieldLoader.Load] public int MapFormat;
		[FieldLoader.Load] public bool Selectable;
        [FieldLoader.Load] public bool UseAsShellmap;
		[FieldLoader.Load] public string RequiresMod;

		[FieldLoader.Load] public string Title;
		[FieldLoader.Load] public string Type = "Conquest";
		[FieldLoader.Load] public string Description;
		[FieldLoader.Load] public string Author;
		[FieldLoader.Load] public string Tileset;
		
		public Dictionary<string, ActorReference> Actors = new Dictionary<string, ActorReference>();

		public int PlayerCount { get { return SpawnPoints.Count(); } }
		public IEnumerable<int2> SpawnPoints { get { return Actors.Values.Where(a => a.Type == "mpspawn").Select(a => a.InitDict.Get<LocationInit>().value); } }
		
		[FieldLoader.Load] public Rectangle Bounds;
				
		public MapStub() {} // Hack for the editor - not used for anything important
		
		public MapStub(string path)
		{
			Path = path;
			Container = FileSystem.OpenPackage(path, int.MaxValue);
			var yaml = new MiniYaml( null, MiniYaml.FromStream(Container.GetContent("map.yaml")) );
			FieldLoader.Load(this, yaml);
            Uid = ComputeHash();
			
			// Load actors
			foreach (var kv in yaml.NodesDict["Actors"].NodesDict)
				Actors.Add(kv.Key, new ActorReference(kv.Value.Value, kv.Value.NodesDict));
			
			// Upgrade map to format 5
			if (MapFormat < 5)
			{
				// Define RequiresMod for map installer
				RequiresMod = Game.CurrentMods.Keys.First();
				
				// Add waypoint actors
				foreach( var wp in yaml.NodesDict[ "Waypoints" ].NodesDict )
				{
					string[] loc = wp.Value.Value.Split( ',' );
					var a = new ActorReference("mpspawn");
					a.Add(new LocationInit(new int2( int.Parse( loc[ 0 ] ), int.Parse( loc[ 1 ] ) )));
					Actors.Add(wp.Key, a);
				}
									
				var TopLeft = (int2)FieldLoader.GetValue( "", typeof(int2), yaml.NodesDict["TopLeft"].Value);
				var BottomRight = (int2)FieldLoader.GetValue( "", typeof(int2), yaml.NodesDict["BottomRight"].Value);
				Bounds = Rectangle.FromLTRB(TopLeft.X, TopLeft.Y, BottomRight.X, BottomRight.Y);
			}
		}

        string ComputeHash()
        {
            // UID is calculated by taking an SHA1 of the yaml and binary data
            // Read the relevant data into a buffer
            var data = Container.GetContent("map.yaml").ReadAllBytes()
                .Concat(Container.GetContent("map.bin").ReadAllBytes()).ToArray();

            // Take the SHA1
            using (var csp = SHA1.Create())
                return new string(csp.ComputeHash(data).SelectMany(a => a.ToString("x2")).ToArray());
        }
	}
}
