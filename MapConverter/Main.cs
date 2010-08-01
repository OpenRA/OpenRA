#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using OpenRA.FileFormats;
using OpenRA;
using System.IO;

namespace MapConverter
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if (args.Length != 3)
			{
				Console.WriteLine("usage: MapConverter mod[,mod]* input-map.ini output-map.yaml");
				return;
			}

			var mods = args[0].Split(',');
			var inputFile = args[1];
			var outputPath = args[2];

			Game.InitializeEngineWithMods(mods);
			var map = MapConverter.Import(inputFile);

			Directory.CreateDirectory(outputPath);
			map.Save(outputPath);
		}
	}
}
