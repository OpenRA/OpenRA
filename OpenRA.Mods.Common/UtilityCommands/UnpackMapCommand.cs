#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	sealed class UnpackMapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--unpack-map";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2 && new string[] { "unpack", "repack" }.Contains(args[1]);
		}

		[Desc("(unpack|repack)", "For all maps, either unpacks oramap files into folders, or repacks folders into oramap files (only if previously unpacked).")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var unpack = args[1] == "unpack";

			// HACK: The engine code assumes that Game.modData is set.
			// HACK: We know that maps can only be oramap or folders, which are ReadWrite
			var modData = Game.ModData = utility.ModData;
			modData.MapCache.LoadMaps();
			foreach (var kv in modData.MapCache.MapLocations)
			{
				foreach (var mapFilename in kv.Key.Contents)
				{
					var mapPackage = kv.Key.OpenPackage(mapFilename, modData.ModFiles);
					var map = new Map(modData, mapPackage);

					if (unpack)
					{
						if (mapPackage is ZipFileLoader.ReadWriteZipFile z)
						{
							map.Save(new Folder(z.Name.Replace(".oramap", "")));
						}
					}
					else
					{
						if (mapPackage is Folder f)
						{
							if (File.Exists(f.Name + ".oramap"))
							{
								map.Save(ZipFileLoader.Create(f.Name + ".oramap"));
								Directory.Delete(f.Name, true);
							}
						}
					}
				}
			}
		}
	}
}
