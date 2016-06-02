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

		delegate void UpgradeAction(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth);

		static void ProcessYaml(Map map, MiniYaml yaml, int engineDate, UpgradeAction processYaml)
		{
			if (yaml == null)
				return;

			if (yaml.Value != null)
			{
				var files = FieldLoader.GetValue<string[]>("value", yaml.Value);
			    foreach (var filename in files)
			    {
			        var fileNodes = MiniYaml.FromStream(map.Package.GetStream(filename));
					processYaml(engineDate, ref fileNodes, null, 0);
			        ((IReadWritePackage)map.Package).Update(filename, Encoding.ASCII.GetBytes(fileNodes.WriteToString()));
			    }
			}

			processYaml(engineDate, ref yaml.Nodes, null, 1);
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
			ProcessYaml(map, map.WeaponDefinitions, engineDate, UpgradeRules.UpgradeWeaponRules);
			ProcessYaml(map, map.RuleDefinitions, engineDate, UpgradeRules.UpgradeActorRules);
			UpgradeRules.UpgradePlayers(engineDate, ref map.PlayerDefinitions, null, 0);
			UpgradeRules.UpgradeActors(engineDate, ref map.ActorDefinitions, null, 0);
			map.Save(package);
		}

		[Desc("MAP", "CURRENTENGINE", "Upgrade map rules to the latest engine version.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var package = modData.ModFiles.OpenWritablePackage(args[1]);
			var engineDate = Exts.ParseIntegerInvariant(args[2]);
			UpgradeMap(modData, package, engineDate);
		}
	}
}
