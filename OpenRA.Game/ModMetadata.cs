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
using OpenRA.FileSystem;

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
		public bool Hidden;

		public Dictionary<string, string> RequiresMods;
		public ContentInstaller Content;
		public IReadOnlyPackage Package;

		static Dictionary<string, ModMetadata> ValidateMods()
		{
			var ret = new Dictionary<string, ModMetadata>();
			foreach (var pair in GetCandidateMods())
			{
				try
				{
					IReadOnlyPackage package = null;
					if (Directory.Exists(pair.Value))
						package = new Folder(pair.Value);
					else
						throw new InvalidDataException(pair.Value + " is not a valid mod package");

					if (!package.Contains("mod.yaml"))
						continue;

					var yaml = new MiniYaml(null, MiniYaml.FromStream(package.GetStream("mod.yaml")));
					var nd = yaml.ToDictionary();
					if (!nd.ContainsKey("Metadata"))
						continue;

					var metadata = FieldLoader.Load<ModMetadata>(nd["Metadata"]);
					metadata.Id = pair.Key;
					metadata.Package = package;

					if (nd.ContainsKey("RequiresMods"))
						metadata.RequiresMods = nd["RequiresMods"].ToDictionary(my => my.Value);
					else
						metadata.RequiresMods = new Dictionary<string, string>();

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
