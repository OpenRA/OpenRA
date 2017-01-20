#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA
{
	public class ExternalMod
	{
		public readonly string Id;
		public readonly string Version;
		public readonly string Title;
		public readonly string LaunchPath;
		public readonly string[] LaunchArgs;
		public Sprite Icon { get; internal set; }

		public static string MakeKey(Manifest mod) { return MakeKey(mod.Id, mod.Metadata.Version); }
		public static string MakeKey(ExternalMod mod) { return MakeKey(mod.Id, mod.Version); }
		public static string MakeKey(string modId, string modVersion) { return modId + "-" + modVersion; }
	}

	public class ExternalMods : IReadOnlyDictionary<string, ExternalMod>
	{
		readonly Dictionary<string, ExternalMod> mods;
		readonly SheetBuilder sheetBuilder;
		readonly string launchPath;

		public ExternalMods(string launchPath)
		{
			// Process.Start requires paths to not be quoted, even if they contain spaces
			if (launchPath.First() == '"' && launchPath.Last() == '"')
				launchPath = launchPath.Substring(1, launchPath.Length - 2);

			this.launchPath = launchPath;
			sheetBuilder = new SheetBuilder(SheetType.BGRA, 256);
			mods = LoadMods();
		}

		Dictionary<string, ExternalMod> LoadMods()
		{
			var ret = new Dictionary<string, ExternalMod>();
			var supportPath = Platform.ResolvePath(Path.Combine("^", "ModMetadata"));
			if (!Directory.Exists(supportPath))
				return ret;

			foreach (var path in Directory.GetFiles(supportPath, "*.yaml"))
			{
				try
				{
					var yaml = MiniYaml.FromStream(File.OpenRead(path), path).First().Value;
					var mod = FieldLoader.Load<ExternalMod>(yaml);
					var iconNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Icon");
					if (iconNode != null && !string.IsNullOrEmpty(iconNode.Value.Value))
					{
						using (var stream = new MemoryStream(Convert.FromBase64String(iconNode.Value.Value)))
						using (var bitmap = new Bitmap(stream))
							mod.Icon = sheetBuilder.Add(bitmap);
					}

					ret.Add(ExternalMod.MakeKey(mod), mod);
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to parse mod metadata file '{0}'", path);
					Log.Write("debug", e.ToString());
				}
			}

			return ret;
		}

		internal void Register(Manifest mod)
		{
			if (mod.Metadata.Hidden)
				return;

			var iconData = "";
			using (var stream = mod.Package.GetStream("icon.png"))
				if (stream != null)
					iconData = Convert.ToBase64String(stream.ReadAllBytes());

			var key = ExternalMod.MakeKey(mod);
			var yaml = new List<MiniYamlNode>()
			{
				new MiniYamlNode("Registration", new MiniYaml("", new List<MiniYamlNode>()
				{
					new MiniYamlNode("Id", mod.Id),
					new MiniYamlNode("Version", mod.Metadata.Version),
					new MiniYamlNode("Title", mod.Metadata.Title),
					new MiniYamlNode("Icon", iconData),
					new MiniYamlNode("LaunchPath", launchPath),
					new MiniYamlNode("LaunchArgs", "Game.Mod=" + mod.Id)
				}))
			};

			var supportPath = Platform.ResolvePath(Path.Combine("^", "ModMetadata"));
			Directory.CreateDirectory(supportPath);

			File.WriteAllLines(Path.Combine(supportPath, key + ".yaml"), yaml.ToLines(false).ToArray());
		}

		public ExternalMod this[string key] { get { return mods[key]; } }
		public int Count { get { return mods.Count; } }
		public ICollection<string> Keys { get { return mods.Keys; } }
		public ICollection<ExternalMod> Values { get { return mods.Values; } }
		public bool ContainsKey(string key) { return mods.ContainsKey(key); }
		public IEnumerator<KeyValuePair<string, ExternalMod>> GetEnumerator() { return mods.GetEnumerator(); }
		public bool TryGetValue(string key, out ExternalMod value) { return mods.TryGetValue(key, out value); }
		IEnumerator IEnumerable.GetEnumerator() { return mods.GetEnumerator(); }
	}
}
