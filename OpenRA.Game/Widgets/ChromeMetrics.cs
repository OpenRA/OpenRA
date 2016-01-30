#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Widgets
{
	public static class ChromeMetrics
	{
		static Dictionary<string, string> data = new Dictionary<string, string>();

		public static void Initialize(IEnumerable<string> yaml)
		{
			data = new Dictionary<string, string>();

			var metrics = MiniYaml.Merge(yaml.Select(MiniYaml.FromFile));
			foreach (var m in metrics)
				foreach (var n in m.Value.Nodes)
					data[n.Key] = n.Value.Value;
		}

		public static T Get<T>(string key)
		{
			return FieldLoader.GetValue<T>(key, data[key]);
		}

		public static bool TryGet<T>(string key, out T result)
		{
			string s;
			if (!data.TryGetValue(key, out s))
			{
				result = default(T);
				return false;
			}

			result = FieldLoader.GetValue<T>(key, s);
			return true;
		}
	}
}
