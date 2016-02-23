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

		[Desc("MAP", "CURRENTENGINE", "Upgrade map rules to the latest engine version.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var package = modData.ModFiles.OpenWritablePackage(args[1]);
			UpgradeRules.UpgradeMapFormat(modData, package);

			var map = new Map(modData, package);
			var engineDate = Exts.ParseIntegerInvariant(args[2]);
			UpgradeRules.UpgradeWeaponRules(engineDate, ref map.WeaponDefinitions, null, 0);
			UpgradeRules.UpgradeActorRules(engineDate, ref map.RuleDefinitions, null, 0);
			UpgradeRules.UpgradePlayers(engineDate, ref map.PlayerDefinitions, null, 0);
			UpgradeRules.UpgradeActors(engineDate, ref map.ActorDefinitions, null, 0);
			map.Save(package);
		}
	}
}
