#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace OpenRA.Mods.Common.FileFormats
{
	public class IniFile
	{
		readonly Dictionary<string, IniSection> sections = new Dictionary<string, IniSection>();

		public IniFile(Stream s)
		{
			using (s)
				Load(s);
		}

		public IniFile(params Stream[] streams)
		{
			foreach (var s in streams)
				Load(s);
		}

		public void Load(Stream s)
		{
			IniSection currentSection = null;
			foreach (var line in s.ReadAllLines())
			{
				if (line.Length == 0) continue;

				switch (line[0])
				{
					case ';': break;
					case '[': currentSection = ProcessSection(line); break;
					default: ProcessEntry(line, currentSection); break;
				}
			}
		}

		readonly Regex sectionPattern = new Regex(@"^\[([^]]*)\]");

		IniSection ProcessSection(string line)
		{
			var m = sectionPattern.Match(line);
			if (!m.Success)
				return null;
			var sectionName = m.Groups[1].Value.ToLowerInvariant();

			if (!sections.TryGetValue(sectionName, out var ret))
				sections.Add(sectionName, ret = new IniSection(sectionName));
			return ret;
		}

		static bool ProcessEntry(string line, IniSection currentSection)
		{
			var comment = line.IndexOf(';');
			if (comment >= 0)
				line = line.Substring(0, comment);

			line = line.Trim();
			if (line.Length == 0)
				return false;

			var key = line;
			var value = "";
			var eq = line.IndexOf('=');
			if (eq >= 0)
			{
				key = line.Substring(0, eq).Trim();
				value = line.Substring(eq + 1, line.Length - eq - 1).Trim();
			}

			if (currentSection == null)
				throw new InvalidOperationException("No current INI section");

			if (!currentSection.Contains(key))
				currentSection.Add(key, value);
			return true;
		}

		public IniSection GetSection(string s)
		{
			return GetSection(s, false);
		}

		public IniSection GetSection(string s, bool allowFail)
		{
			if (sections.TryGetValue(s.ToLowerInvariant(), out var section))
				return section;

			if (allowFail)
				return new IniSection(s);
			throw new InvalidOperationException("Section does not exist in map or rules: " + s);
		}

		public IEnumerable<IniSection> Sections => sections.Values;
	}

	public class IniSection : IEnumerable<KeyValuePair<string, string>>
	{
		public string Name { get; }
		readonly Dictionary<string, string> values = new Dictionary<string, string>();

		public IniSection(string name)
		{
			Name = name;
		}

		public void Add(string key, string value)
		{
			values[key] = value;
		}

		public bool Contains(string key)
		{
			return values.ContainsKey(key);
		}

		public string GetValue(string key, string defaultValue)
		{
			return values.TryGetValue(key, out var s) ? s : defaultValue;
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
