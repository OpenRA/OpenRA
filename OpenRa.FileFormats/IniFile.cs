using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;

namespace OpenRa.FileFormats
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

				if (line.Length == 0) continue;

				switch (line[0])
				{
					case ';': break;
					case '[': ProcessSection(line); break;
					default: ProcessEntry(line); break;
				}
			}
		}

		Regex sectionPattern = new Regex( @"^\[([^]]*)\]" );
		Regex entryPattern = new Regex( @"([^=;]+)=([^;]*)" );

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
            int comment = line.IndexOf(';');
            if (comment >= 0)
                line = line.Substring(0, comment);

            int eq = line.IndexOf('=');
            if (eq < 0)
                return false;

            if (currentSection == null)
                throw new InvalidOperationException("No current INI section");

            currentSection.Add(line.Substring(0, eq), 
                line.Substring(eq + 1, line.Length - eq - 1));
			return true;
		}

		public IniSection GetSection( string s )
		{
			IniSection section;
			sections.TryGetValue( s, out section );
			return section;
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
