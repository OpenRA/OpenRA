#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
			public override string ToString() { return "{0}:{1}".F(Filename, Line); }
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

		public override string ToString()
		{
			return "{{YamlNode: {0} @ {1}}}".F(Key, Location);
		}
	}

	public class MiniYaml
	{
		public string Value;
		public List<MiniYamlNode> Nodes;

		public Dictionary<string, MiniYaml> NodesDict
		{
			get
			{
				var ret = new Dictionary<string, MiniYaml>();
				foreach (var y in Nodes)
				{
					if (ret.ContainsKey(y.Key))
						throw new InvalidDataException("Duplicate key `{0}' in MiniYaml".F(y.Key));
					ret.Add(y.Key, y.Value);
				}
				return ret;
			}
		}

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
			foreach (var ll in lines)
			{
				var line = ll;
				++lineNo;
				if (line.Contains('#'))
					line = line.Substring(0, line.IndexOf('#')).TrimEnd(' ', '\t');
				var t = line.TrimStart(' ', '\t');
				if (t.Length == 0)
					continue;
				var level = line.Length - t.Length;
				var location = new MiniYamlNode.SourceLocation { Filename = filename, Line = lineNo };

				if (levels.Count <= level)
					throw new YamlException("Bad indent in miniyaml at {0}".F (location));
				while (levels.Count > level + 1)
					levels.RemoveAt(levels.Count - 1);

				var d = new List<MiniYamlNode>();
				var rhs = SplitAtColon( ref t );
				levels[ level ].Add( new MiniYamlNode( t, rhs, d, location ) );

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

		public static List<MiniYamlNode> MergeLiberal(List<MiniYamlNode> a, List<MiniYamlNode> b)
		{
			return Merge(a, b, false);
		}

		public static List<MiniYamlNode> MergeStrict(List<MiniYamlNode> a, List<MiniYamlNode> b)
		{
			return Merge(a, b, true);
		}

		static List<MiniYamlNode> Merge( List<MiniYamlNode> a, List<MiniYamlNode> b, bool throwErrors )
		{
			if( a.Count == 0 )
				return b;
			if( b.Count == 0 )
				return a;

			var ret = new List<MiniYamlNode>();

			var aDict = a.ToDictionary( x => x.Key );
			var bDict = b.ToDictionary( x => x.Key );
			var keys = aDict.Keys.Union( bDict.Keys ).ToList();

			var noInherit = keys.Where(x => x.Length > 0 && x[0] == '-')
				.ToDictionary(x => x.Substring(1), x => false);

			foreach( var key in keys )
			{
				MiniYamlNode aa, bb;
				aDict.TryGetValue( key, out aa );
				bDict.TryGetValue( key, out bb );

				if( noInherit.ContainsKey( key ) )
				{
					if (!throwErrors)
						if (aa != null)
							ret.Add(aa);

					noInherit[key] = true;
				}
				else
				{
					var loc = aa == null ? default( MiniYamlNode.SourceLocation ) : aa.Location;
					var merged = ( aa == null || bb == null ) ? aa ?? bb : new MiniYamlNode( key, Merge( aa.Value, bb.Value, throwErrors ), loc );
					ret.Add( merged );
				}
			}

			if (throwErrors)
			if (noInherit.ContainsValue(false))
				throw new YamlException("Bogus yaml removals: {0}".F(
					noInherit.Where(x => !x.Value).JoinWith(", ")));

			return ret;
		}

		public static MiniYaml MergeLiberal(MiniYaml a, MiniYaml b)
		{
			return Merge(a, b, false);
		}

		public static MiniYaml MergeStrict(MiniYaml a, MiniYaml b)
		{
			return Merge(a, b, true);
		}

		static MiniYaml Merge( MiniYaml a, MiniYaml b, bool throwErrors )
		{
			if( a == null )
				return b;
			if( b == null )
				return a;

			return new MiniYaml( a.Value ?? b.Value, Merge( a.Nodes, b.Nodes, throwErrors ) );
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
			return y.ToLines(true).Select(x => x.TrimEnd()).JoinWith("\n");
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

	public class YamlException : Exception
	{
		public YamlException(string s) : base(s) { }
	}
}
