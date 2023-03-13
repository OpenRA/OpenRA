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
using OpenRA.FileSystem;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	sealed class DumpSequenceSheetsCommand : IUtilityCommand
	{
		static readonly int[] ChannelMasks = { 2, 1, 0, 3 };

		string IUtilityCommand.Name => "--dump-sequence-sheets";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		[Desc("PALETTE", "TILESET-OR-MAP", "Exports sequence texture atlas as a set of png images.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			var palette = new ImmutablePalette(args[1], new[] { 0 }, Array.Empty<int>());

			SequenceSet sequences;
			if (modData.DefaultTerrainInfo.ContainsKey(args[2]))
				sequences = new SequenceSet(modData.ModFiles, modData, args[2], null);
			else
			{
				var mapPackage = new Folder(Platform.EngineDir).OpenPackage(args[2], modData.ModFiles);
				if (mapPackage == null)
					throw new InvalidOperationException($"{args[2]} is not a valid tileset or map path");

				sequences = new Map(modData, mapPackage).Sequences;
			}

			sequences.LoadSprites();

			var count = 0;

			var sb = sequences.SpriteCache.SheetBuilders[SheetType.Indexed];
			foreach (var s in sb.AllSheets)
			{
				var max = s == sb.Current ? (int)sb.CurrentChannel + 1 : 4;
				for (var i = 0; i < max; i++)
					s.AsPng((TextureChannel)ChannelMasks[i], palette).Save($"{count}.{i}.png");

				count++;
			}

			sb = sequences.SpriteCache.SheetBuilders[SheetType.BGRA];
			foreach (var s in sb.AllSheets)
				s.AsPng().Save($"{count++}.png");
		}
	}
}
