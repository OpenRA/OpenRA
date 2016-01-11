#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA
{
	public class ModMetadata
	{
		public static readonly Dictionary<string, string> CandidateModPaths = GetCandidateMods();
		public static readonly Dictionary<string, ModMetadata> AllMods = ValidateMods();

		public string Id;
		public string Title;
		public string Description;
		public string Version;
		public string Author;
		public string LogoImagePath;
		public string PreviewImagePath;
		public bool Hidden;
		public ContentInstaller Content;

		static Dictionary<string, ModMetadata> ValidateMods()
		{
			var ret = new Dictionary<string, ModMetadata>();
			foreach (var pair in CandidateModPaths)
			{
				try
				{
					var yamlPath = Path.Combine(pair.Value, "mod.yaml");
					if (!File.Exists(yamlPath))
						continue;

					var yaml = new MiniYaml(null, MiniYaml.FromFile(yamlPath));
					var nd = yaml.ToDictionary();
					if (!nd.ContainsKey("Metadata"))
						continue;

					var metadata = FieldLoader.Load<ModMetadata>(nd["Metadata"]);
					metadata.Id = pair.Key;

					if (nd.ContainsKey("ContentInstaller"))
						metadata.Content = FieldLoader.Load<ContentInstaller>(nd["ContentInstaller"]);

					ret.Add(pair.Key, metadata);
				}
				catch (Exception ex)
				{
					Console.WriteLine("An exception occurred when trying to load ModMetadata for `{0}`:".F(pair.Key));
					Console.WriteLine(ex.Message);
				}
			}

			return ret;
		}

		static Dictionary<string, string> GetCandidateMods()
		{
			// Get mods that are in the game folder.
			var basePath = Platform.ResolvePath(Path.Combine(".", "mods"));
			var mods = Directory.GetDirectories(basePath)
				.ToDictionary(x => x.Substring(basePath.Length + 1));

			// Get mods that are in the support folder.
			var supportPath = Platform.ResolvePath(Path.Combine("^", "mods"));
			if (!Directory.Exists(supportPath))
				return mods;

			foreach (var pair in Directory.GetDirectories(supportPath).ToDictionary(x => x.Substring(supportPath.Length + 1)))
				mods.Add(pair.Key, pair.Value);

			return mods;
		}
	}
}
