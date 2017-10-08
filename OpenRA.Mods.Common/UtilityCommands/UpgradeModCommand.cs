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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class UpgradeModCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--upgrade-mod"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		delegate void UpgradeAction(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth);

		void ProcessYaml(string type, IEnumerable<string> files, ModData modData, int engineDate, UpgradeAction processFile)
		{
			Console.WriteLine("Processing {0}:", type);
			foreach (var filename in files)
			{
				Console.WriteLine("\t" + filename);
				string name;
				IReadOnlyPackage package;
				if (!modData.ModFiles.TryGetPackageContaining(filename, out package, out name) || !(package is Folder))
				{
					Console.WriteLine("\t\tFile cannot be opened for writing! Ignoring...");
					continue;
				}

				var yaml = MiniYaml.FromStream(package.GetStream(name), name);
				processFile(modData, engineDate, ref yaml, null, 0);

				// Generate the on-disk path
				var path = Path.Combine(package.Name, name);
				using (var file = new StreamWriter(path))
					file.Write(yaml.WriteToString());
			}
		}

		[Desc("OLDENGINE", "Upgrade mod rules to the latest engine version.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			var engineDate = Exts.ParseIntegerInvariant(args[1]);
			if (engineDate < UpgradeRules.MinimumSupportedVersion)
			{
				Console.WriteLine("Unsupported engine version. Use the release-{0} utility to update to that version, and then try again",
					UpgradeRules.MinimumSupportedVersion);
				return;
			}

			ProcessYaml("Rules", modData.Manifest.Rules, modData, engineDate, UpgradeRules.UpgradeActorRules);
			ProcessYaml("Weapons", modData.Manifest.Weapons, modData, engineDate, UpgradeRules.UpgradeWeaponRules);
			ProcessYaml("Sequences", modData.Manifest.Sequences, modData, engineDate, UpgradeRules.UpgradeSequences);
			ProcessYaml("Tilesets", modData.Manifest.TileSets, modData, engineDate, UpgradeRules.UpgradeTileset);
			ProcessYaml("Cursors", modData.Manifest.Cursors, modData, engineDate, UpgradeRules.UpgradeCursors);
			ProcessYaml("Chrome Metrics", modData.Manifest.ChromeMetrics, modData, engineDate, UpgradeRules.UpgradeChromeMetrics);
			ProcessYaml("Chrome Layout", modData.Manifest.ChromeLayout, modData, engineDate, UpgradeRules.UpgradeChromeLayout);

			// The map cache won't be valid if there was a map format upgrade, so walk the map packages manually
			// Only upgrade system maps - user maps must be updated manually using --upgrade-map
			Console.WriteLine("Processing System Maps:");
			foreach (var map in modData.MapCache.EnumerateMapsWithoutCaching())
			{
				try
				{
					UpgradeMapCommand.UpgradeMap(modData, (IReadWritePackage)map.Package, engineDate);
				}
				catch (Exception e)
				{
					Console.WriteLine("Failed to upgrade map {0}", map);
					Console.WriteLine("Error was: {0}", e.ToString());
				}
			}
		}
	}
}
