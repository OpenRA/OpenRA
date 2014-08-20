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

namespace OpenRA.Mods.RA.UtilityCommands
{
	class UpgradeV5MapCommand : IUtilityCommand
	{
		public string Name { get { return "--upgrade-map-v5"; } }

		[Desc("MAPFILE", "MOD", "Upgrade a version 5 map to version 6.")]
		public void Run(string[] args)
		{
			var map = args[1];
			var mod = args[2];
			Game.modData = new ModData(mod);
			new Map(map, mod);
		}
	}
}
