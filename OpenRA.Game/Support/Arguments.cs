#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenRA
{
	public class Arguments
	{
		Dictionary<string, string> args = new Dictionary<string, string>();

		public Arguments(IEnumerable<string> src)
		{
			Regex regex = new Regex("([^=]+)=(.*)");
			foreach (string s in src)
			{
				Match m = regex.Match(s);
				if (m == null || !m.Success)
					continue;

				args.Add(m.Groups[1].Value, m.Groups[2].Value);
			}
		}

		public bool Contains(string key) { return args.ContainsKey(key); }

		public string GetValue(string key, string defaultValue) { return Contains(key) ? args[key] : defaultValue; }

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
