#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;

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
		public ModContent ModContent;
		public IReadOnlyPackage Package;

		static Dictionary<string, ModMetadata> ValidateMods()
		{
			var ret = new Dictionary<string, ModMetadata>();
			foreach (var pair in GetCandidateMods())
			{
				IReadOnlyPackage package = null;
				try
				{
					if (Directory.Exists(pair.Second))
						package = new Folder(pair.Second);
					else
					{
						try
						{
							using (var fileStream = File.OpenRead(pair.Second))
								package = new ZipFile(fileStream, pair.Second);
						}
						catch
						{
							throw new InvalidDataException(pair.Second + " is not a valid mod package");
						}
					}

					if (!package.Contains("mod.yaml"))
					{
						package.Dispose();
						continue;
					}

					var yaml = new MiniYaml(null, MiniYaml.FromStream(package.GetStream("mod.yaml"), "mod.yaml"));
					var nd = yaml.ToDictionary();
					if (!nd.ContainsKey("Metadata"))
					{
						package.Dispose();
						continue;
					}

					var metadata = FieldLoader.Load<ModMetadata>(nd["Metadata"]);
					metadata.Id = pair.First;
					metadata.Package = package;

					if (nd.ContainsKey("RequiresMods"))
						metadata.RequiresMods = nd["RequiresMods"].ToDictionary(my => my.Value);
					else
						metadata.RequiresMods = new Dictionary<string, string>();

					if (nd.ContainsKey("ModContent"))
						metadata.ModContent = FieldLoader.Load<ModContent>(nd["ModContent"]);

					// Mods in the support directory and oramod packages (which are listed later
					// in the CandidateMods list) override mods in the main install.
					ret[pair.First] = metadata;
				}
				catch (Exception ex)
				{
					if (package != null)
						package.Dispose();
					Console.WriteLine("An exception occurred when trying to load ModMetadata for `{0}`:".F(pair.First));
					Console.WriteLine(ex.Message);
				}
			}

			return ret;
		}

		static IEnumerable<Pair<string, string>> GetCandidateMods()
		{
			// Get mods that are in the game folder.
			var basePath = Platform.ResolvePath(Path.Combine(".", "mods"));
			var mods = Directory.GetDirectories(basePath)
				.Select(x => Pair.New(x.Substring(basePath.Length + 1), x))
				.ToList();

			foreach (var m in Directory.GetFiles(basePath, "*.oramod"))
				mods.Add(Pair.New(Path.GetFileNameWithoutExtension(m), m));

			// Get mods that are in the support folder.
			var supportPath = Platform.ResolvePath(Path.Combine("^", "mods"));
			if (!Directory.Exists(supportPath))
				return mods;

			foreach (var pair in Directory.GetDirectories(supportPath).ToDictionary(x => x.Substring(supportPath.Length + 1)))
				mods.Add(Pair.New(pair.Key, pair.Value));

			foreach (var m in Directory.GetFiles(supportPath, "*.oramod"))
				mods.Add(Pair.New(Path.GetFileNameWithoutExtension(m), m));

			return mods;
		}
	}
}
