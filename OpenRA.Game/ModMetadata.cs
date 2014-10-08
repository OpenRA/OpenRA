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
using System.IO;
using System.Linq;

namespace OpenRA
{
	public class ModMetadata
	{
		public static readonly Dictionary<string, ModMetadata> AllMods = ValidateMods();

		public string Id;
		public string Title;
		public string Description;
		public string Version;
		public string Author;

		static Dictionary<string, ModMetadata> ValidateMods()
		{
			var basePath = Platform.ResolvePath(".", "mods");
			var mods = Directory.GetDirectories(basePath)
				.Select(x => x.Substring(basePath.Length + 1));

			var ret = new Dictionary<string, ModMetadata>();
			foreach (var m in mods)
			{
				var yamlPath = Platform.ResolvePath(".", "mods", m, "mod.yaml");
				if (!File.Exists(yamlPath))
					continue;

				var yaml = new MiniYaml(null, MiniYaml.FromFile(yamlPath));
				var nd = yaml.ToDictionary();
				if (!nd.ContainsKey("Metadata"))
					continue;

				var mod = FieldLoader.Load<ModMetadata>(nd["Metadata"]);
				mod.Id = m;

				ret.Add(m, mod);
			}

			return ret;
		}
	}
}
