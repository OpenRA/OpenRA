using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenRa.Game
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
	}
}
