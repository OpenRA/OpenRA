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

namespace OpenRA.FileFormats
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
		
		public static MiniYaml FromDictionary<K,V>(Dictionary<K,V>dict)
		{
			return new MiniYaml( null, dict.ToDictionary( x=>x.Key.ToString(), x=>new MiniYaml(x.Value.ToString())));
		}
		
		public static MiniYaml FromList<T>(List<T>list)
		{
			var d = new Dictionary<string, MiniYaml>();
			return new MiniYaml( null, list.ToDictionary( x=>x.ToString(), x=>new MiniYaml(null)));
		}
		
		static Dictionary<string, MiniYaml> FromLines(string[] lines)
		{
			var levels = new List<Dictionary<string, MiniYaml>>();
			levels.Add(new Dictionary<string, MiniYaml>());

			foreach (var line in lines)
			{
				var t = line.TrimStart(' ', '\t');
				if (t.Length == 0 || t[0] == '#')
					continue;
				var level = line.Length - t.Length;

				if (levels.Count <= level)
					throw new InvalidOperationException("Bad indent in miniyaml");
				while (levels.Count > level + 1)
					levels.RemoveAt(levels.Count - 1);

				var colon = t.IndexOf(':');
				var d = new Dictionary<string, MiniYaml>();
				try
				{
					if (colon == -1)
						levels[level].Add(t.Trim(), new MiniYaml(null, d));
					else
					{
						var value = t.Substring(colon + 1).Trim();
						if (value.Length == 0)
							value = null;
						levels[level].Add(t.Substring(0, colon).Trim(), new MiniYaml(value, d));
					}
				}
				catch (ArgumentException) { throw new InvalidDataException("Duplicate Identifier:`{0}`".F(t)); }
				
				levels.Add(d);
			}
			return levels[0];
		}

		public static Dictionary<string, MiniYaml> FromFileInPackage( string path )
		{
			StreamReader reader = new StreamReader( FileSystem.Open(path) );
			List<string> lines = new List<string>();
			
			while( !reader.EndOfStream )
				lines.Add(reader.ReadLine());
			reader.Close();
			
			return FromLines(lines.ToArray());
		}
		
		public static Dictionary<string, MiniYaml> FromFile( string path )
		{			
			return FromLines(File.ReadAllLines( path ));
		}

		public static Dictionary<string, MiniYaml> FromStream(Stream s)
		{
			using (var reader = new StreamReader(s))
				return FromString(reader.ReadToEnd());
		}

		public static Dictionary<string, MiniYaml> FromString(string text)
		{
			return FromLines(text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries));
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

//				if( key.Length > 0 && key[ 0 ] == '-' )
//					continue;
			//	else 
				if( noInherit.Contains( key ) )
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
