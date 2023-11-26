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

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	sealed class MapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--map";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2 &&
				new string[] { "refresh", "unpack", "repack" }.Contains(args[1]) &&
				(args.Length <= 2 || IsValidRegex(args[2]));

			static bool IsValidRegex(string pattern)
			{
				try
				{
					_ = new Regex(pattern);
				}
				catch (ArgumentException)
				{
					return false;
				}

				return true;
			}
		}

		[Desc("(refresh|unpack|repack) [filenameRegex=.*]",
			"For maps matching regex: " +
			"refresh a map to reformat map.yaml and regenerate the preview, " +
			"unpack oramap files into folders, " +
			"repack folders into oramap files (only if previously unpacked).")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var filenameRegex = args.Length >= 3 ? new Regex(args[2]) : null;

			// HACK: The engine code assumes that Game.modData is set.
			// HACK: We know that maps can only be oramap or folders, which are ReadWrite
			var modData = Game.ModData = utility.ModData;
			modData.MapCache.LoadMaps();
			foreach (var kv in modData.MapCache.MapLocations)
			{
				foreach (var mapFilename in kv.Key.Contents)
				{
					if (filenameRegex != null && !filenameRegex.IsMatch(mapFilename))
						continue;

					using (var mapPackage = kv.Key.OpenPackage(mapFilename, modData.ModFiles))
					{
						var map = new Map(modData, mapPackage);

						switch (args[1])
						{
							case "refresh":
								map.Save((IReadWritePackage)mapPackage);
								break;
							case "unpack":
								if (mapPackage is ZipFileLoader.ReadWriteZipFile z)
									map.Save(new Folder(z.Name.Replace(".oramap", "")));
								break;
							case "repack":
								if (mapPackage is Folder f && File.Exists(f.Name + ".oramap"))
								{
									map.Save(ZipFileLoader.Create(f.Name + ".oramap"));
									Directory.Delete(f.Name, true);
								}

								break;
						}
					}
				}
			}
		}
	}
}
