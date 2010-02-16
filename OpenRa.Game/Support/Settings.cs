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

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenRa
{
	class Settings
	{
		Dictionary<string, string> settings = new Dictionary<string, string>();

		public Settings(IEnumerable<string> src)
		{
			Regex regex = new Regex("([^=]+)=(.*)");
			foreach (string s in src)
			{
				Match m = regex.Match(s);
				if (m == null || !m.Success)
					continue;

				settings.Add(m.Groups[1].Value, m.Groups[2].Value);
			}
		}

		public bool Contains(string key) { return settings.ContainsKey(key); }

		public string GetValue(string key, string defaultValue) { return Contains(key) ? settings[key] : defaultValue; }

		public int GetValue(string key, int defaultValue)
		{
			int result;

			if (!int.TryParse(GetValue(key, defaultValue.ToString()), out result))
				result = defaultValue;

			return result;
		}

		public bool GetValue(string key, bool defaultValue)
		{
			bool result;

			if (!bool.TryParse(GetValue(key, defaultValue.ToString()), out result))
				result = defaultValue;

			return result;
		}
	}
}
