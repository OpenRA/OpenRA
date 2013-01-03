#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace OpenRA.FileFormats
{
	/* describes what is to be loaded in order to run a set of mods */

	public class Manifest
	{
		public readonly string[] Mods = { };
		public readonly string[] Folders = { };
		public readonly string[] Packages = { };
		public readonly string[] Rules = { };
		public readonly string[] ServerTraits = { };
		public readonly string[] Sequences = { };
		public readonly string[] Cursors = { };
		public readonly string[] Chrome = { };
		public readonly string[] Assemblies = { };
		public readonly string[] ChromeLayout = { };
		public readonly string[] Weapons = { };
		public readonly string[] Voices = { };
		public readonly string[] Notifications = { };
		public readonly string[] Music = { };
		public readonly string[] Movies = { };
		public readonly string[] TileSets = { };
		public readonly string[] ChromeMetrics = { };
		public readonly MiniYaml LoadScreen;
		public readonly Dictionary<string, Pair<string,int>> Fonts;
		public readonly int TileSize = 24;

		public Manifest(string lang, string[] mods)
		{
			Mods = mods;
			var yaml = new MiniYaml(null, mods
				.Select(m => MiniYaml.FromFile(
				new[] { "mods", m, "mod.yaml" }.Aggregate(Path.Combine)))
				.Aggregate(MiniYaml.MergeLiberal)).NodesDict;

			// Todo: Use fieldloader
			Folders = YamlList(yaml, "Folders");
			Packages = YamlList(yaml, "Packages");
			Rules = YamlList(yaml, "Rules");
			ServerTraits = YamlList(yaml, "ServerTraits");
			Sequences = YamlList(yaml, "Sequences");
			Cursors = YamlList(yaml, "Cursors");
			Chrome = YamlList(yaml, "Chrome");
			Assemblies = YamlList(yaml, "Assemblies");
			ChromeLayout = YamlList(yaml, "ChromeLayout");
			Weapons = YamlList(yaml, "Weapons");
			Voices = YamlList(yaml, "Voices");
			Notifications = YamlList(yaml, "Notifications");
			Music = YamlList(yaml, "Music");
			Movies = YamlList(yaml, "Movies");
			TileSets = YamlList(yaml, "TileSets");
			ChromeMetrics = YamlList(yaml, "ChromeMetrics");

			LoadScreen = yaml["LoadScreen"];
			Fonts = yaml["Fonts"].NodesDict.ToDictionary(x => x.Key,
				x => Pair.New(x.Value.NodesDict["Font"].Value,
					int.Parse(x.Value.NodesDict["Size"].Value)));

			if (yaml.ContainsKey("TileSize"))
				TileSize = int.Parse(yaml["TileSize"].Value);

			if (yaml.ContainsKey("Languages"))
			{
				//Fallback to English if strings are missing
				yaml = new MiniYaml(null, mods
				.Select(m => MiniYaml.FromFile(
				new[] { "mods", m, "l10n", "en", "language.yaml" }.Aggregate(Path.Combine)))
				.Aggregate(MiniYaml.MergeLiberal)).NodesDict;
				Rules = YamlList(yaml, "Rules").Concat(Rules).ToArray();
				// TODO: chrome layout does not merge yet properly with nodes of the same name

				Console.WriteLine("Mod supports translation, loading language: {0}", lang);
				yaml = new MiniYaml(null, mods
				.Select(m => MiniYaml.FromFile(
				new[] { "mods", m, "l10n", lang, "language.yaml" }.Aggregate(Path.Combine)))
				.Aggregate(MiniYaml.MergeLiberal)).NodesDict;
				Folders = YamlList(yaml, "Folders").Concat(Folders).ToArray();
				Packages = YamlList(yaml, "Packages").Concat(Packages).ToArray();
				Rules = YamlList(yaml, "Rules").Concat(Rules).ToArray();
				ChromeLayout = YamlList(yaml, "ChromeLayout").Concat(ChromeLayout).ToArray();
			}

		}

		static string[] YamlList(Dictionary<string, MiniYaml> yaml, string key)
		{
			if (!yaml.ContainsKey(key))
				return new string[] {};

			return yaml[key].NodesDict.Keys.ToArray();
		}
	}
}
