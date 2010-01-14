using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OpenRa.FileFormats
{
	using MiniYamlNodes = Dictionary<string, MiniYaml>;

	public class MiniYaml
	{
		public string Value;
		public Dictionary<string, MiniYaml> Nodes = new Dictionary<string,MiniYaml>();

		public MiniYaml( string value ) : this( value, new Dictionary<string, MiniYaml>() ) { }

		public MiniYaml( string value, Dictionary<string, MiniYaml> nodes )
		{
			Value = value;
			Nodes = nodes;
		}

		public static Dictionary<string, MiniYaml> FromFile( string path )
		{
			var lines = File.ReadAllLines( path );

			var levels = new List<Dictionary<string, MiniYaml>>();
			levels.Add( new Dictionary<string, MiniYaml>() );

			foreach( var line in lines )
			{
				var t = line.TrimStart( ' ', '\t' );
				if( t.Length == 0 || t[ 0 ] == '#' )
					continue;
				var level = line.Length - t.Length;

				if( levels.Count <= level )
					throw new InvalidOperationException( "Bad indent in miniyaml" );
				while( levels.Count > level + 1 )
					levels.RemoveAt( levels.Count - 1 );

				var colon = t.IndexOf( ':' );
				var d = new Dictionary<string, MiniYaml>();

				if( colon == -1 )
					levels[ level ].Add( t.Trim(), new MiniYaml( null, d ) );
				else
				{
					var value = t.Substring( colon + 1 ).Trim();
					if( value.Length == 0 )
						value = null;
					levels[ level ].Add( t.Substring( 0, colon ).Trim(), new MiniYaml( value, d ) );
				}
				levels.Add( d );
			}
			return levels[ 0 ];
		}

		public static Dictionary<string, MiniYaml> Merge( Dictionary<string, MiniYaml> a, Dictionary<string, MiniYaml> b )
		{
			if( a.Count == 0 )
				return b;
			if( b.Count == 0 )
				return a;

			var ret = new Dictionary<string, MiniYaml>();

			var keys = a.Keys.Union( b.Keys ).ToList();

			var noInherit = keys.Where( x => x.Length > 0 && x[ 0 ] == '-' ).Select( x => x.Substring( 1 ) ).ToList();

			foreach( var key in keys )
			{
				MiniYaml aa, bb;
				a.TryGetValue( key, out aa );
				b.TryGetValue( key, out bb );

				if( key.Length > 0 && key[ 0 ] == '-' )
					continue;
				else if( noInherit.Contains( key ) )
				{
					if( aa != null )
						ret.Add( key, aa );
				}
				else
					ret.Add( key, Merge( aa, bb ) );
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
