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

namespace OpenRA.Mods.Common.UtilityCommands
{
	class UpgradeModCommand : IUtilityCommand
	{
		public string Name { get { return "--upgrade-mod"; } }

		[Desc("CURRENTENGINE", "Upgrade mod rules to the latest engine version.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.modData = modData;
			Game.modData.MapCache.LoadMaps();

			var engineDate = Exts.ParseIntegerInvariant(args[1]);

			Console.WriteLine("Processing Rules:");
			foreach (var filename in Game.modData.Manifest.Rules)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeRules.UpgradeActorRules(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.WriteLine(yaml.WriteToString());
			}

			Console.WriteLine("Processing Sequences:");
			foreach (var filename in Game.modData.Manifest.Sequences)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeSequences.UpgradeActorSequences(engineDate, ref yaml, null, 0);

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
