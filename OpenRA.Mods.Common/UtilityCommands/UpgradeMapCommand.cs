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
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class UpgradeMapCommand : IUtilityCommand
	{
		public string Name { get { return "--upgrade-map"; } }

		[Desc("MAP", "CURRENTENGINE", "Upgrade map rules to the latest engine version.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.modData = modData;

			var map = new Map(args[1]);
			var engineDate = Exts.ParseIntegerInvariant(args[2]);

			UpgradeRules.UpgradeWeaponRules(engineDate, ref map.WeaponDefinitions, null, 0);
			UpgradeRules.UpgradeActorRules(engineDate, ref map.RuleDefinitions, null, 0);
			UpgradeSequences.UpgradeActorSequences(engineDate, ref map.SequenceDefinitions, null, 0);
			map.Save(args[1]);
		}
	}
}
