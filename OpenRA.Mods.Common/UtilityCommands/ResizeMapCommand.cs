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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class ResizeMapCommand : IUtilityCommand
	{
		public string Name { get { return "--resize-map"; } }

		int width;
		int height;

		Map map;

		public bool ValidateArguments(string[] args)
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
		public void Run(ModData modData, string[] args)
		{
			Game.ModData = modData;
			map = new Map(modData, modData.ModFiles.OpenPackage(args[1], new Folder(".")));
			Console.WriteLine("Resizing map {0} from {1} to {2},{3}", map.Title, map.MapSize, width, height);
			map.Resize(width, height);

			var forRemoval = new List<MiniYamlNode>();

			foreach (var kv in map.ActorDefinitions)
			{
				var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
				var location = actor.InitDict.Get<LocationInit>().Value(null);
				if (!map.Contains(location))
				{
					Console.WriteLine("Removing actor {0} located at {1} due being outside of the new map boundaries.".F(actor.Type, location));
					forRemoval.Add(kv);
				}
			}

			foreach (var kv in forRemoval)
				map.ActorDefinitions.Remove(kv);

			map.Save((IReadWritePackage)map.Package);
		}
	}
}
