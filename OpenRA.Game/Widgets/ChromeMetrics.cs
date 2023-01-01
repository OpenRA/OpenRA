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
using System.Linq;

namespace OpenRA.Widgets
{
	public static class ChromeMetrics
	{
		static Dictionary<string, string> data = new Dictionary<string, string>();

		public static void Initialize(ModData modData)
		{
			data = new Dictionary<string, string>();
			var metrics = MiniYaml.Merge(modData.Manifest.ChromeMetrics.Select(
				y => MiniYaml.FromStream(modData.DefaultFileSystem.Open(y), y)));
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
			if (!data.TryGetValue(key, out var s))
			{
				result = default;
				return false;
			}

			result = FieldLoader.GetValue<T>(key, s);
			return true;
		}
	}
}
