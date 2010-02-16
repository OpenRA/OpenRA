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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace OpenRa.FileFormats
{
	public class IniFile
	{
		Dictionary<string, IniSection> sections = new Dictionary<string, IniSection>();

		public IniFile( Stream s )
		{
			Load( s );
		}

		public IniFile( params Stream[] streams )
		{
			foreach( var s in streams )
				Load( s );
		}

		public void Load( Stream s )
		{
			StreamReader reader = new StreamReader( s );
			IniSection currentSection = null;

			while( !reader.EndOfStream )
			{
				string line = reader.ReadLine();

				if( line.Length == 0 ) continue;

				switch( line[ 0 ] )
				{
				case ';': break;
				case '[': currentSection = ProcessSection( line ); break;
				default: ProcessEntry( line, currentSection ); break;
				}
			}
		}

		Regex sectionPattern = new Regex( @"^\[([^]]*)\]" );

		IniSection ProcessSection( string line )
		{
			Match m = sectionPattern.Match( line );
			if( m == null || !m.Success )
				return null;
			string sectionName = m.Groups[ 1 ].Value.ToLowerInvariant();

			IniSection ret;
			if( !sections.TryGetValue( sectionName, out ret ) )
				sections.Add( sectionName, ret = new IniSection( sectionName ) );
			return ret;
		}

		bool ProcessEntry( string line, IniSection currentSection )
		{
			int comment = line.IndexOf( ';' );
			if( comment >= 0 )
				line = line.Substring( 0, comment );

			line = line.Trim();
			if( line.Length == 0 )
				return false;

			var key = line;
			var value = "";
			int eq = line.IndexOf( '=' );
			if( eq >= 0 )
			{
				key = line.Substring( 0, eq );
				value = line.Substring( eq + 1, line.Length - eq - 1 );
			}

			if( currentSection == null )
				throw new InvalidOperationException( "No current INI section" );

			if( !currentSection.Contains( key ) )
				currentSection.Add( key, value );
			return true;
		}

		public IniSection GetSection( string s )
		{
			return GetSection( s, false );
		}

		public IniSection GetSection( string s, bool allowFail )
		{
			IniSection section;
			if( sections.TryGetValue( s.ToLowerInvariant(), out section ) )
				return section;

			if( allowFail )
				return new IniSection( s );
			throw new InvalidOperationException( "Section does not exist in map or rules: " + s );
		}

		public IEnumerable<IniSection> Sections { get { return sections.Values; } }
	}

	public class IniSection : IEnumerable<KeyValuePair<string, string>>
	{
		public string Name { get; private set; }
		Dictionary<string, string> values = new Dictionary<string, string>();

		public IniSection( string name )
		{
			Name = name;
		}

		public void Add( string key, string value )
		{
			values[key] = value;
		}

		public bool Contains( string key )
		{
			return values.ContainsKey( key );
		}

		public string GetValue( string key, string defaultValue )
		{
			string s;
			return values.TryGetValue( key, out s ) ? s : defaultValue;
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
