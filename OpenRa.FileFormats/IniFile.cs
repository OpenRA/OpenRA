using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace MapViewer
{
	public class IniFile
	{
		Dictionary<string, IniSection> sections = new Dictionary<string, IniSection>();
		IniSection currentSection;

		public IniFile( Stream s )
		{
			StreamReader reader = new StreamReader( s );
			while( !reader.EndOfStream )
			{
				string line = reader.ReadLine();
				if( !ProcessEntry( line ) )
					ProcessSection( line );
			}
		}

		Regex sectionPattern = new Regex( @"\[([^]]*)\]" );
		Regex entryPattern = new Regex( @"([^=]+)=([^;]*)" );

		bool ProcessSection( string line )
		{
			Match m = sectionPattern.Match( line );
			if( m == null || !m.Success )
				return false;

			string sectionName = m.Groups[ 1 ].Value;
			currentSection = new IniSection( sectionName );
			sections.Add( sectionName, currentSection );

			return true;
		}

		bool ProcessEntry( string line )
		{
			Match m = entryPattern.Match( line );
			if( m == null || !m.Success )
				return false;

			if( currentSection == null )
				throw new InvalidOperationException( "No current INI section" );

			string keyName = m.Groups[ 1 ].Value;
			string keyValue = m.Groups[ 2 ].Value;

			currentSection.Add( keyName, keyValue );

			return true;
		}

		public IniSection GetSection( string s )
		{
			IniSection section;
			sections.TryGetValue( s, out section );
			return section;
		}
	}

	public class IniSection
	{
		string name;
		Dictionary<string, string> values = new Dictionary<string, string>();

		public IniSection( string name )
		{
			this.name = name;
		}

		public void Add( string key, string value )
		{
			values.Add( key, value );
		}

		public string GetValue( string key, string defaultValue )
		{
			string s;
			return values.TryGetValue( key, out s ) ? s : defaultValue;
		}
	}
}
