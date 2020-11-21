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

using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class RefreshMapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--refresh-map"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("MAP", "Opens and resaves a map to reformat map.yaml and regenerate the preview.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			// HACK: We know that maps can only be oramap or folders, which are ReadWrite
			var modData = Game.ModData = utility.ModData;
			using (var package = new Folder(Platform.EngineDir).OpenPackage(args[1], modData.ModFiles))
				new Map(modData, package).Save((IReadWritePackage)package);
		}
	}
}
