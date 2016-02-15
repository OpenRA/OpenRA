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

			modData.ModFiles.LoadFromManifest(modData.Manifest);
			modData.SpriteSequenceLoader.OnMissingSpriteError = s => Console.WriteLine("\t" + s);

			foreach (var t in modData.Manifest.TileSets)
			{
				var ts = new TileSet(modData, t);
				Console.WriteLine("Tileset: " + ts.Name);
				var sc = new SpriteCache(modData.DefaultFileSystem, modData.SpriteLoaders, new SheetBuilder(SheetType.Indexed));
				var nodes = MiniYaml.Merge(modData.Manifest.Sequences.Select(s => MiniYaml.FromStream(modData.DefaultFileSystem.Open(s))));
				foreach (var n in nodes)
					modData.SpriteSequenceLoader.ParseSequences(modData, ts, sc, n);
			}
		}
	}
}
