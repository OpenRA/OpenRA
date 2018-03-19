#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using System.Text;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class UpgradeMapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--upgrade-map"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		delegate void UpgradeAction(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth);

		static Stream Open(string filename, IReadOnlyPackage package, IReadOnlyFileSystem fallback)
		{
			// Explicit package paths never refer to a map
			if (!filename.Contains("|") && package.Contains(filename))
				return package.GetStream(filename);

			return fallback.Open(filename);
		}

		static void ProcessYaml(ModData modData, IReadOnlyPackage package, MiniYaml yaml, int engineDate, UpgradeAction processYaml)
		{
			if (yaml == null)
				return;

			if (yaml.Value != null)
			{
				var files = FieldLoader.GetValue<string[]>("value", yaml.Value);
				foreach (var filename in files)
				{
					var fileNodes = MiniYaml.FromStream(Open(filename, package, modData.DefaultFileSystem), filename);
					processYaml(modData, engineDate, ref fileNodes, null, 0);

					// HACK: Obtain the writable save path using knowledge of the underlying filesystem workings
					var packagePath = filename;
					var p = package;
					if (filename.Contains("|"))
						modData.DefaultFileSystem.TryGetPackageContaining(filename, out p, out packagePath);

					((IReadWritePackage)p).Update(packagePath, Encoding.ASCII.GetBytes(fileNodes.WriteToString()));
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

			var mapStream = package.GetStream("map.yaml");
			if (mapStream == null)
				return;

			var yaml = new MiniYaml(null, MiniYaml.FromStream(mapStream, package.Name));

			var rules = yaml.Nodes.FirstOrDefault(n => n.Key == "Rules");
			if (rules != null)
				ProcessYaml(modData, package, rules.Value, engineDate, UpgradeRules.UpgradeActorRules);

			var weapons = yaml.Nodes.FirstOrDefault(n => n.Key == "Weapons");
			if (weapons != null)
				ProcessYaml(modData, package, weapons.Value, engineDate, UpgradeRules.UpgradeWeaponRules);

			var sequences = yaml.Nodes.FirstOrDefault(n => n.Key == "Sequences");
			if (sequences != null)
				ProcessYaml(modData, package, sequences.Value, engineDate, UpgradeRules.UpgradeSequences);

			var players = yaml.Nodes.FirstOrDefault(n => n.Key == "Players");
			if (players != null)
				UpgradeRules.UpgradePlayers(modData, engineDate, ref players.Value.Nodes, null, 0);

			var actors = yaml.Nodes.FirstOrDefault(n => n.Key == "Actors");
			if (actors != null)
				UpgradeRules.UpgradeActors(modData, engineDate, ref actors.Value.Nodes, null, 0);

			package.Update("map.yaml", Encoding.UTF8.GetBytes(yaml.Nodes.WriteToString()));
		}

		[Desc("MAP", "OLDENGINE", "Upgrade map rules to the latest engine version.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			// HACK: We know that maps can only be oramap or folders, which are ReadWrite
			var package = new Folder(".").OpenPackage(args[1], modData.ModFiles) as IReadWritePackage;
			if (package == null)
				throw new FileNotFoundException(args[1]);

			var engineDate = Exts.ParseIntegerInvariant(args[2]);
			UpgradeMap(modData, package, engineDate);
		}
	}
}
