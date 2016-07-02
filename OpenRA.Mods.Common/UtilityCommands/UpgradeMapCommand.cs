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
using System.Text;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class UpgradeMapCommand : IUtilityCommand
	{
		public string Name { get { return "--upgrade-map"; } }

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		delegate void UpgradeAction(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth);

		static void ProcessYaml(ModData modData, Map map, MiniYaml yaml, int engineDate, UpgradeAction processYaml)
		{
			if (yaml == null)
				return;

			if (yaml.Value != null)
			{
				var files = FieldLoader.GetValue<string[]>("value", yaml.Value);
				foreach (var filename in files)
				{
					var fileNodes = MiniYaml.FromStream(map.Open(filename), filename);
					processYaml(modData, engineDate, ref fileNodes, null, 0);

					// HACK: Obtain the writable save path using knowledge of the underlying filesystem workings
					var packagePath = filename;
					var package = map.Package;
					if (filename.Contains("|"))
						modData.DefaultFileSystem.TryGetPackageContaining(filename, out package, out packagePath);

					((IReadWritePackage)package).Update(packagePath, Encoding.ASCII.GetBytes(fileNodes.WriteToString()));
				}
			}

			processYaml(modData, engineDate, ref yaml.Nodes, null, 1);
		}

		public static void UpgradeMap(ModData modData, IReadWritePackage package, int engineDate)
		{
			UpgradeRules.UpgradeMapFormat(modData, package);

			if (engineDate < UpgradeRules.MinimumSupportedVersion)
			{
				Console.WriteLine("Unsupported engine version. Use the release-{0} utility to update to that version, and then try again",
					UpgradeRules.MinimumSupportedVersion);
				return;
			}

			var map = new Map(modData, package);
			ProcessYaml(modData, map, map.WeaponDefinitions, engineDate, UpgradeRules.UpgradeWeaponRules);
			ProcessYaml(modData, map, map.RuleDefinitions, engineDate, UpgradeRules.UpgradeActorRules);
			UpgradeRules.UpgradePlayers(modData, engineDate, ref map.PlayerDefinitions, null, 0);
			UpgradeRules.UpgradeActors(modData, engineDate, ref map.ActorDefinitions, null, 0);
			map.Save(package);
		}

		[Desc("MAP", "CURRENTENGINE", "Upgrade map rules to the latest engine version.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			// HACK: We know that maps can only be oramap or folders, which are ReadWrite
			var package = modData.ModFiles.OpenPackage(args[1], new Folder(".")) as IReadWritePackage;
			if (package == null)
				throw new FileNotFoundException(args[1]);

			var engineDate = Exts.ParseIntegerInvariant(args[2]);
			UpgradeMap(modData, package, engineDate);
		}
	}
}
