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
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	sealed class DumpSequenceSheetsCommand : IUtilityCommand
	{
		static readonly int[] ChannelMasks = [2, 1, 0, 3];

		string IUtilityCommand.Name => "--dump-sequence-sheets";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 1;
		}

		[Desc("[PALETTE]", "[TILESET-OR-MAP]", "Exports texture atlas' as a set of png images. "
			+ "If palette is not specified, only BGRA sheets are exported. "
			+ "If tileset-or-map is not specified, all tilesets are exported.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			var palette = args.Length > 1 ? new ImmutablePalette(args[1], [0], []) : null;
			var sequences = new List<SequenceSet>();

			if (args.Length == 3)
			{
				var tilesetUpper = args[2].ToUpperInvariant();
				if (modData.DefaultTerrainInfo.ContainsKey(tilesetUpper))
					sequences.Add(new SequenceSet(modData.ModFiles, modData, tilesetUpper, null));
				else
				{
					var mapPackage = new Folder(Platform.EngineDir).OpenPackage(args[2], modData.ModFiles);
					if (mapPackage == null)
						throw new InvalidOperationException($"{args[2]} is not a valid tileset or map path");

					sequences.Add(new Map(modData, mapPackage).Sequences);
				}
			}
			else
			{
				foreach (var t in modData.DefaultTerrainInfo.Keys)
					sequences.Add(new SequenceSet(modData.ModFiles, modData, t, null));
			}

			var sheetCount = 1;
			foreach (var sequence in sequences)
			{
				sequence.LoadSprites();

				var sequencesName = "sequences";
				var terrainName = "tileset";
				if (sequences.Count > 1)
				{
					var name = sequence.TileSet.ToLowerInvariant();
					sequencesName += "." + name;
					terrainName += "." + name;
				}

				var sb = sequence.SpriteCache.SheetBuilders[SheetType.Indexed];
				foreach (var s in sb.AllSheets)
					CommitSheet(sb, s, sequencesName, palette, ref sheetCount);

				foreach (var s in sequence.SpriteCache.SheetBuilders[SheetType.BGRA].AllSheets)
					CommitSheet(null, s, sequencesName, palette, ref sheetCount);

				sequence.Dispose();
			}
		}

		static void CommitSheet(SheetBuilder builder, Sheet sheet, string name, ImmutablePalette palette, ref int count)
		{
			if (builder == null)
				sheet.AsPng().Save($"{count++}.{name}.png", Png.Compression.BEST_SPEED);
			else
			{
				if (palette != null)
				{
					var channels = sheet == builder.Current ? (int)builder.CurrentChannel + 1 : 4;
					for (var i = 0; i < channels; i++)
						sheet.AsPng((TextureChannel)ChannelMasks[i], palette).Save($"{count}.{i}.{name}.png", Png.Compression.BEST_SPEED);

					count++;
				}
			}
		}
	}
}
