#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class GetMapHashCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--map-hash"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("MAPFILE", "Generate hash of specified oramap file.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			using (var package = new Folder(Platform.EngineDir).OpenPackage(args[1], utility.ModData.ModFiles))
				Console.WriteLine(Map.ComputeUID(package));
		}
	}
}
