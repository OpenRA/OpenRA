using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;

namespace RulesConverter
{
	using MiniYamlNodes = Dictionary<string, MiniYaml>;
	using System.IO;

	static class MiniYamlExts
	{
		public static void WriteToFile( this MiniYamlNodes y, string filename )
		{
			File.WriteAllLines( filename, y.ToLines( true ).ToArray() );
		}

		public static IEnumerable<string> ToLines( this MiniYamlNodes y, bool lowest )
		{
			foreach( var kv in y )
			{
				foreach( var line in kv.Value.ToLines( kv.Key ) )
					yield return line;
				if( lowest )
					yield return "";
			}
		}

		public static IEnumerable<string> ToLines( this MiniYaml y, string name )
		{
			yield return name + ": " + y.Value;
			foreach( var line in y.Nodes.ToLines( false ) )
				yield return "\t" + line;
		}

		public static void OptimizeInherits( this MiniYamlNodes y, MiniYamlNodes baseYaml )
		{
			foreach( var key in y.Keys.ToList() )
			{
				var node = y[ key ];
				MiniYaml inherits;
				node.Nodes.TryGetValue( "Inherits", out inherits );
				if( inherits == null || string.IsNullOrEmpty( inherits.Value ) )
					continue;

				MiniYaml parent;
				baseYaml.TryGetValue( inherits.Value, out parent );
				if( parent == null )
					continue;

				y[ key ] = Diff( node, parent );
				if( y[ key ] == null )
					y.Remove( key );
			}
		}

		public static MiniYamlNodes Diff( MiniYamlNodes a, MiniYamlNodes b )
		{
			if( a.Count == 0 && b.Count == 0 )
				return null;
			if( b.Count == 0 )
				return a;
			if( a.Count == 0 )
				throw new NotImplementedException( "parent has key not in child" );

			var ret = new MiniYamlNodes();

			var keys = a.Keys.Union( b.Keys ).ToList();

			foreach( var key in keys )
			{
				MiniYaml aa, bb;
				a.TryGetValue( key, out aa );
				b.TryGetValue( key, out bb );
				var diff = Diff( aa, bb );
				if( diff != null )
					ret.Add( key, diff );
			}

			if( ret.Count == 0 ) return null;
			return ret;
		}

		public static MiniYaml Diff( MiniYaml a, MiniYaml b )
		{
			if( a == null && b == null )
				throw new InvalidOperationException( "can't happen" );
			else if( a == null )
				throw new NotImplementedException( "parent has key not in child" );
			else if( b == null )
				return a;

			var diff = Diff( a.Nodes, b.Nodes );
			if( diff == null )
				return null;
			return new MiniYaml( a.Value, diff );
		}
	}
}
