#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using System.Text;

namespace OpenRA.Mods.RA.UtilityCommands
{
	class UpgradeModCommand : IUtilityCommand
	{
		public string Name { get { return "--upgrade-mod"; } }

		[Desc("MOD", "CURRENTENGINE", "Upgrade mod rules to the latest engine version.")]
		public void Run(string[] args)
		{
			var mod = args[1];
			var engineDate = Exts.ParseIntegerInvariant(args[2]);

			Game.modData = new ModData(mod);
			Game.modData.MapCache.LoadMaps();

			Console.WriteLine("Processing Rules:");
			foreach (var filename in Game.modData.Manifest.Rules)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeRules.UpgradeActorRules(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.WriteLine(yaml.WriteToString());
			}

			Console.WriteLine("Processing Weapons:");
			foreach (var filename in Game.modData.Manifest.Weapons)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeRules.UpgradeWeaponRules(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.WriteLine(yaml.WriteToString());
			}

			Console.WriteLine("Processing Tilesets:");
			foreach (var filename in Game.modData.Manifest.TileSets)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeRules.UpgradeTileset(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.WriteLine(yaml.WriteToString());
			}

			Console.WriteLine("Processing Maps:");
			var maps = Game.modData.MapCache
				.Where(m => m.Status == MapStatus.Available)
				.Select(m => m.Map);

			foreach (var map in maps)
			{
				Console.WriteLine("\t" + map.Path);
				UpgradeRules.UpgradeActorRules(engineDate, ref map.RuleDefinitions, null, 0);
				UpgradeRules.UpgradeWeaponRules(engineDate, ref map.WeaponDefinitions, null, 0);
				map.Save(map.Path);
			}
		}
	}
}
