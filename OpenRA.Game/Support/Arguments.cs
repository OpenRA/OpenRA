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

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenRA
{
	public class Arguments
	{
		readonly Dictionary<string, string> args = new Dictionary<string, string>();

		public static Arguments Empty => new Arguments();

		public Arguments(params string[] src)
		{
			var regex = new Regex("([^=]+)=(.*)");
			foreach (var s in src)
			{
				var m = regex.Match(s);
				if (!m.Success)
					continue;

				args[m.Groups[1].Value] = m.Groups[2].Value;
			}
		}

		public bool Contains(string key) { return args.ContainsKey(key); }
		public string GetValue(string key, string defaultValue) { return Contains(key) ? args[key] : defaultValue; }
		public void ReplaceValue(string key, string value) { args[key] = value; }
	}
}
