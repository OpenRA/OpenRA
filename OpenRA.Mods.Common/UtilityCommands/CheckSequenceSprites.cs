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
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class CheckSquenceSprites : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--check-sequence-sprites"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Check the sequence definitions for missing sprite files.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			var failed = false;
			modData.SpriteSequenceLoader.OnMissingSpriteError = s => { Console.WriteLine("\t" + s); failed = true; };

			foreach (var t in modData.Manifest.TileSets)
			{
				var ts = new TileSet(modData.DefaultFileSystem, t);
				Console.WriteLine("Tileset: " + ts.Name);
				var sc = new SpriteCache(modData.DefaultFileSystem, modData.SpriteLoaders, new SheetBuilder(SheetType.Indexed));
				var nodes = MiniYaml.Merge(modData.Manifest.Sequences.Select(s => MiniYaml.FromStream(modData.DefaultFileSystem.Open(s), s)));
				foreach (var n in nodes)
					modData.SpriteSequenceLoader.ParseSequences(modData, ts, sc, n);
			}

			if (failed)
				Environment.Exit(1);
		}
	}
}
