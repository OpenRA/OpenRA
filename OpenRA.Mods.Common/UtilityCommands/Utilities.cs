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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public static class Utilities
	{
		/// <exception cref="ArgumentNullException">Thrown if manifestPropertySelector is null.</exception>
		public static MiniYamlNode GetTopLevelNodeByKey(ModData modData, string key,
			Func<Manifest, string[]> manifestPropertySelector,
			Func<Map, MiniYaml> mapPropertySelector = null,
			string mapPath = null)
		{
			if (manifestPropertySelector == null)
				throw new ArgumentNullException("manifestPropertySelector", "Must pass a non-null manifestPropertySelector");

			Map map = null;
			if (mapPath != null)
			{
				try
				{
					map = new Map(modData, new Folder(Platform.EngineDir).OpenPackage(mapPath, modData.ModFiles));
				}
				catch (InvalidDataException ex)
				{
					Console.WriteLine("Could not load map '{0}' so this data does not include the map's overrides.", mapPath);
					Console.WriteLine(ex);
					map = null;
				}
			}

			var manifestNodes = manifestPropertySelector.Invoke(modData.Manifest);
			var mapProperty = map == null || mapPropertySelector == null ? null
				: mapPropertySelector.Invoke(map);

			var fs = map ?? modData.DefaultFileSystem;
			var topLevelNodes = MiniYaml.Load(fs, manifestNodes, mapProperty);
			return topLevelNodes.FirstOrDefault(n => n.Key == key);
		}
	}
}
