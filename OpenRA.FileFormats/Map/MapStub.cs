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

namespace OpenRA.FileFormats
{
	public class MapStub
	{
		protected IFolder Container;
		public string Path {get; protected set;}
		
		// Yaml map data
		public string Uid { get; protected set; }
		[FieldLoader.Load] public bool Selectable;
        [FieldLoader.Load] public bool UseAsShellmap;
		[FieldLoader.Load] public string RequiresMod;

		[FieldLoader.Load] public string Title;
		[FieldLoader.Load] public string Type = "Conquest";
		[FieldLoader.Load] public string Description;
		[FieldLoader.Load] public string Author;
		[FieldLoader.Load] public int PlayerCount;
		[FieldLoader.Load] public string Tileset;

		[FieldLoader.LoadUsing( "LoadWaypoints" )]
		public Dictionary<string, int2> Waypoints = new Dictionary<string, int2>();
		public IEnumerable<int2> SpawnPoints { get { return Waypoints.Select(kv => kv.Value); } }
		
		[FieldLoader.Load] public int2 TopLeft;
		[FieldLoader.Load] public int2 BottomRight;
		public Rectangle Bounds;
		
		public MapStub() {} // Hack for the editor - not used for anything important
		
		public MapStub(string path)
		{
			Path = path;
			Container = FileSystem.OpenPackage(path, int.MaxValue);
			var yaml = MiniYaml.FromStream(Container.GetContent("map.yaml"));
			FieldLoader.Load( this, new MiniYaml( null, yaml ) );
			
			Bounds = Rectangle.FromLTRB(TopLeft.X, TopLeft.Y, BottomRight.X, BottomRight.Y);
            Uid = ComputeHash();
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

		static object LoadWaypoints( MiniYaml y )
		{
			var ret = new Dictionary<string, int2>();
			foreach( var wp in y.NodesDict[ "Waypoints" ].NodesDict )
			{
				string[] loc = wp.Value.Value.Split( ',' );
				ret.Add( wp.Key, new int2( int.Parse( loc[ 0 ] ), int.Parse( loc[ 1 ] ) ) );
			}
			return ret;
		}
	}
}
