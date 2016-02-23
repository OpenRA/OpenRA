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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class UpgradeModCommand : IUtilityCommand
	{
		public string Name { get { return "--upgrade-mod"; } }

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("CURRENTENGINE", "Upgrade mod rules to the latest engine version.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;
			modData.MapCache.LoadMaps();

			var engineDate = Exts.ParseIntegerInvariant(args[1]);

			Console.WriteLine("Processing Rules:");
			foreach (var filename in modData.Manifest.Rules)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeRules.UpgradeActorRules(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.Write(yaml.WriteToString());
			}

			Console.WriteLine("Processing Weapons:");
			foreach (var filename in modData.Manifest.Weapons)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeRules.UpgradeWeaponRules(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.Write(yaml.WriteToString());
			}

			Console.WriteLine("Processing Tilesets:");
			foreach (var filename in modData.Manifest.TileSets)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeRules.UpgradeTileset(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.Write(yaml.WriteToString());
			}

			Console.WriteLine("Processing Cursors:");
			foreach (var filename in modData.Manifest.Cursors)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeRules.UpgradeCursors(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.Write(yaml.WriteToString());
			}

			Console.WriteLine("Processing Chrome Metrics:");
			foreach (var filename in modData.Manifest.ChromeMetrics)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeRules.UpgradeChromeMetrics(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.Write(yaml.WriteToString());
			}

			Console.WriteLine("Processing Chrome Layout:");
			foreach (var filename in modData.Manifest.ChromeLayout)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeRules.UpgradeChromeLayout(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.Write(yaml.WriteToString());
			}

			Console.WriteLine("Processing Maps:");
			var mapPreviews = modData.MapCache
				.Where(m => m.Status == MapStatus.Available);

			foreach (var p in mapPreviews)
			{
				var package = (IReadWritePackage)p.Package;
				Console.WriteLine("\t" + package.Name);
				UpgradeRules.UpgradeMapFormat(modData, package);

				var map = new Map(modData, package);
				UpgradeRules.UpgradeActorRules(engineDate, ref map.RuleDefinitions, null, 0);
				UpgradeRules.UpgradeWeaponRules(engineDate, ref map.WeaponDefinitions, null, 0);
				UpgradeRules.UpgradePlayers(engineDate, ref map.PlayerDefinitions, null, 0);
				UpgradeRules.UpgradeActors(engineDate, ref map.ActorDefinitions, null, 0);
				map.Save(package);
			}
		}
	}
}
