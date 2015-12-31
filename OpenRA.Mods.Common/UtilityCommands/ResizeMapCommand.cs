#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class ResizeMapCommand : IUtilityCommand
	{
		public string Name { get { return "--resize-map"; } }

		int width;
		int height;

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
			var map = new Map(args[1]);
			Console.WriteLine("Resizing map {0} from {1} to {2},{3}", map.Title, map.MapSize, width, height);
			map.Resize(width, height);
			map.Save(map.Path);
		}
	}
}
