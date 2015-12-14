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
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class CheckSquenceSprites : IUtilityCommand
	{
		public string Name { get { return "--check-sequence-sprites"; } }

		public bool ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Check the sequence definitions for missing sprite files.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;
			Game.ModData.ModFiles.LoadFromManifest(Game.ModData.Manifest);
			Game.ModData.SpriteSequenceLoader.OnMissingSpriteError = s => Console.WriteLine("\t" + s);

			foreach (var t in Game.ModData.Manifest.TileSets)
			{
				var ts = new TileSet(Game.ModData, t);
				Console.WriteLine("Tileset: " + ts.Name);
				var sc = new SpriteCache(modData.SpriteLoaders, new SheetBuilder(SheetType.Indexed));
				var sequenceFiles = modData.Manifest.Sequences;

				var partial = sequenceFiles
					.Select(s => MiniYaml.FromFile(s))
					.Aggregate(MiniYaml.MergePartial);

				var nodes = MiniYaml.ApplyRemovals(partial);
				foreach (var n in nodes)
					Game.ModData.SpriteSequenceLoader.ParseSequences(Game.ModData, ts, sc, n);
			}
		}
	}
}
