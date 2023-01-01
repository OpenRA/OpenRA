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
using System.Collections.Generic;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class ResizeMapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--resize-map";

		int width;
		int height;

		Map map;

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			if (args.Length < 4)
				return false;

			if (!int.TryParse(args[2], out width) && width > 0)
			{
				Console.WriteLine("Invalid WIDTH");
				return false;
			}

			if (!int.TryParse(args[3], out height) && height > 0)
			{
				Console.WriteLine("Invalid HEIGHT");
				return false;
			}

			return true;
		}

		[Desc("MAPFILE", "WIDTH", "HEIGHT", "Resize the map at the bottom corners.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			map = new Map(modData, new Folder(Platform.EngineDir).OpenPackage(args[1], modData.ModFiles));
			Console.WriteLine("Resizing map {0} from {1} to {2},{3}", map.Title, map.MapSize, width, height);
			map.Resize(width, height);

			var forRemoval = new List<MiniYamlNode>();

			foreach (var kv in map.ActorDefinitions)
			{
				var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
				var locationInit = actor.GetOrDefault<LocationInit>();
				if (locationInit == null)
					continue;

				if (!map.Contains(locationInit.Value))
				{
					Console.WriteLine($"Removing actor {actor.Type} located at {locationInit.Value} due being outside of the new map boundaries.");
					forRemoval.Add(kv);
				}
			}

			foreach (var kv in forRemoval)
				map.ActorDefinitions.Remove(kv);

			map.Save((IReadWritePackage)map.Package);
		}
	}
}
