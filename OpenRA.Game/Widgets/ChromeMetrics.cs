#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
			var metrics = yaml.Select(y => MiniYaml.FromFile(y))
				.Aggregate(MiniYaml.MergeLiberal);

			foreach (var m in metrics)
				foreach (var n in m.Value.Nodes)
					data[n.Key] = n.Value.Value;
		}

		public static T Get<T>(string key)
		{
			return FieldLoader.GetValue<T>(key, data[key]);
		}
	}
}
