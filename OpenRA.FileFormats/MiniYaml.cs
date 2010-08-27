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
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats
{
	using MiniYamlNodes = List<MiniYamlNode>;

	public class MiniYamlNode
	{
		public struct SourceLocation
		{
			public string Filename; public int Line;
		}

		public SourceLocation Location;
		public string Key;
		public MiniYaml Value;

		public MiniYamlNode( string k, MiniYaml v )
		{
			Key = k;
			Value = v;
		}

		public MiniYamlNode( string k, MiniYaml v, SourceLocation loc )
			: this( k, v )
		{
			Location = loc;
		}

		public MiniYamlNode( string k, string v )
			: this( k, v, null )
		{
		}
		public MiniYamlNode( string k, string v, List<MiniYamlNode> n )
			: this( k, new MiniYaml( v, n ) )
		{
		}

		public MiniYamlNode( string k, string v, List<MiniYamlNode> n, SourceLocation loc )
			: this( k, new MiniYaml( v, n ), loc )
		{
		}
	}

	public class MiniYaml
	{
		public string Value;
		public List<MiniYamlNode> Nodes;

		public Dictionary<string, MiniYaml> NodesDict { get { return Nodes.ToDictionary( x => x.Key, x => x.Value ); } }

		public MiniYaml( string value ) : this( value, null ) { }

		public MiniYaml( string value, List<MiniYamlNode> nodes )
		{
			Value = value;
			Nodes = nodes ?? new List<MiniYamlNode>();
		}

		public static MiniYaml FromDictionary<K, V>( Dictionary<K, V> dict )
		{
			return new MiniYaml( null, dict.Select( x => new MiniYamlNode( x.Key.ToString(), new MiniYaml( x.Value.ToString() ) ) ).ToList() );
		}

		public static MiniYaml FromList<T>( List<T> list )
		{
			return new MiniYaml( null, list.Select( x => new MiniYamlNode( x.ToString(), new MiniYaml( null ) ) ).ToList() );
		}
		
		static List<MiniYamlNode> FromLines(string[] lines, string filename)
		{
			var levels = new List<List<MiniYamlNode>>();
			levels.Add(new List<MiniYamlNode>());

			var lineNo = 0;
			foreach (var line in lines)
			{
				++lineNo;
				var t = line.TrimStart(' ', '\t');
				if (t.Length == 0 || t[0] == '#')
					continue;
				var level = line.Length - t.Length;

				if (levels.Count <= level)
					throw new InvalidOperationException("Bad indent in miniyaml");
				while (levels.Count > level + 1)
					levels.RemoveAt(levels.Count - 1);

				var d = new List<MiniYamlNode>();
				var rhs = SplitAtColon( ref t );
				levels[ level ].Add( new MiniYamlNode( t, rhs, d, new MiniYamlNode.SourceLocation { Filename = filename, Line = lineNo } ) );
				
				levels.Add(d);
			}
			return levels[ 0 ];
		}

		static string SplitAtColon( ref string t )
		{
			var colon = t.IndexOf(':');
			if( colon == -1 )
				return null;
			var ret = t.Substring( colon + 1 ).Trim();
			if( ret.Length == 0 )
				ret = null;
			t = t.Substring( 0, colon ).Trim();
			return ret;
		}

		public static List<MiniYamlNode> FromFileInPackage( string path )
		{
			StreamReader reader = new StreamReader( FileSystem.Open(path) );
			List<string> lines = new List<string>();
			
			while( !reader.EndOfStream )
				lines.Add(reader.ReadLine());
			reader.Close();
			
			return FromLines(lines.ToArray(), path);
		}

		public static Dictionary<string, MiniYaml> DictFromFile( string path )
		{
			return FromFile( path ).ToDictionary( x => x.Key, x => x.Value );
		}

		public static Dictionary<string, MiniYaml> DictFromStream( Stream stream )
		{
			return FromStream( stream ).ToDictionary( x => x.Key, x => x.Value );
		}

		public static List<MiniYamlNode> FromFile( string path )
		{			
			return FromLines(File.ReadAllLines( path ), path);
		}

		public static List<MiniYamlNode> FromStream(Stream s)
		{
			using (var reader = new StreamReader(s))
				return FromString(reader.ReadToEnd());
		}

		public static List<MiniYamlNode> FromString(string text)
		{
			return FromLines(text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries), "<no filename available>");
		}

		public static List<MiniYamlNode> Merge( List<MiniYamlNode> a, List<MiniYamlNode> b )
		{
			if( a.Count == 0 )
				return b;
			if( b.Count == 0 )
				return a;

			var ret = new List<MiniYamlNode>();

			var aDict = a.ToDictionary( x => x.Key );
			var bDict = b.ToDictionary( x => x.Key );
			var keys = aDict.Keys.Union( bDict.Keys ).ToList();

			var noInherit = keys.Where( x => x.Length > 0 && x[ 0 ] == '-' ).Select( x => x.Substring( 1 ) ).ToList();

			foreach( var key in keys )
			{
				MiniYamlNode aa, bb;
				aDict.TryGetValue( key, out aa );
				bDict.TryGetValue( key, out bb );

				if( noInherit.Contains( key ) )
				{
					if( aa != null )
						ret.Add( aa );
				}
				else
				{
					var loc = aa == null ? default( MiniYamlNode.SourceLocation ) : aa.Location;
					var merged = ( aa == null || bb == null ) ? aa ?? bb : new MiniYamlNode( key, Merge( aa.Value, bb.Value ), loc );
					ret.Add( merged );
				}
			}

			return ret;
		}

		public static MiniYaml Merge( MiniYaml a, MiniYaml b )
		{
			if( a == null )
				return b;
			if( b == null )
				return a;

			return new MiniYaml( a.Value ?? b.Value, Merge( a.Nodes, b.Nodes ) );
		}

		public IEnumerable<string> ToLines(string name)
		{
			yield return name + ": " + Value;
			if (Nodes != null)
				foreach (var line in Nodes.ToLines(false))
					yield return "\t" + line;
		}
	}

	public static class MiniYamlExts
	{
		public static void WriteToFile(this MiniYamlNodes y, string filename)
		{
			File.WriteAllLines(filename, y.ToLines(true).Select(x => x.TrimEnd()).ToArray());
		}

		public static string WriteToString(this MiniYamlNodes y)
		{
			return string.Join("\n", y.ToLines(true).Select(x => x.TrimEnd()).ToArray());
		}

		public static IEnumerable<string> ToLines(this MiniYamlNodes y, bool lowest)
		{
			foreach (var kv in y)
			{
				foreach (var line in kv.Value.ToLines(kv.Key))
					yield return line;
				if (lowest)
					yield return "";
			}
		}
	}
}
