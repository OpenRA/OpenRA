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
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class CheckImageReferences : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--check-image-references"; } }

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
				var sc = new SpriteCache(modData.DefaultFileSystem, modData.SpriteLoaders);
				var nodes = MiniYaml.Merge(modData.Manifest.Sequences.Select(s => MiniYaml.FromStream(modData.DefaultFileSystem.Open(s), s)));
				foreach (var n in nodes.Where(node => !node.Key.StartsWith(ActorInfo.AbstractActorPrefix, StringComparison.Ordinal)))
					modData.SpriteSequenceLoader.ParseSequences(modData, ts, sc, n);

				foreach (var template in ts.Templates)
				{
					foreach (var image in template.Value.Images)
					{
						if (!modData.DefaultFileSystem.Exists(image))
						{
							modData.SpriteSequenceLoader.OnMissingSpriteError("Tileset file {0} not found.".F(image));
							continue;
						}

						var sprites = sc[image];
						if (template.Value.TilesCount > sprites.Length)
							modData.SpriteSequenceLoader.OnMissingSpriteError("Tileset tile {0} has frames defined without matching artwork.".F(template.Key));

						var frames = template.Value.Frames;
						if (frames == null)
							continue;

						foreach (var frame in frames)
							if (sprites.Length < frame)
								modData.SpriteSequenceLoader.OnMissingSpriteError("Tileset tile {0} has more frames defined than the sprite has.".F(template.Key));
					}
				}
			}

			if (failed)
				Environment.Exit(1);
		}
	}
}
