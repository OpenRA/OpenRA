﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA
{
	public class Mod
	{
		public static readonly Dictionary<string, Mod> AllMods = ValidateMods(Directory.GetDirectories("mods").Select(x => x.Substring(5)).ToArray());

		public string Id;
		public string Title;
		public string Description;
		public string Version;
		public string Author;

		public static Dictionary<string, Mod> ValidateMods(string[] mods)
		{
			var ret = new Dictionary<string, Mod>();
			foreach (var m in mods)
			{
				var yamlPath = new[] { "mods", m, "mod.yaml" }.Aggregate(Path.Combine);
				if (!File.Exists(yamlPath))
					continue;

				var yaml = new MiniYaml(null, MiniYaml.FromFile(yamlPath));
				if (!yaml.NodesDict.ContainsKey("Metadata"))
					continue;

				var mod = FieldLoader.Load<Mod>(yaml.NodesDict["Metadata"]);
				mod.Id = m;

				ret.Add(m, mod);
			}

			return ret;
		}
	}
}
