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
	public class GetMapHashCommand : IUtilityCommand
	{
		public string Name { get { return "--map-hash"; } }

		[Desc("MAPFILE", "Generate hash of specified oramap file.")]
		public void Run(ModData modData, string[] args)
		{
			Game.modData = modData;
			var result = new Map(args[1]).Uid;
			Console.WriteLine(result);
		}
	}
}
