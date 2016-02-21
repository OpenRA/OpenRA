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

namespace OpenRA.Mods.Common.UtilityCommands
{
	class GetMapHashCommand : IUtilityCommand
	{
		public string Name { get { return "--map-hash"; } }

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("MAPFILE", "Generate hash of specified oramap file.")]
		public void Run(ModData modData, string[] args)
		{
			using (var package = modData.ModFiles.OpenPackage(args[1]))
				Console.WriteLine(Map.ComputeUID(package));
		}
	}
}
