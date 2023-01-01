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
	class DumpSequenceSheetsCommand : IUtilityCommand
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

			SequenceProvider sequences;
			var mapPackage = new Folder(Platform.EngineDir).OpenPackage(args[2], modData.ModFiles);
			if (mapPackage != null)
				sequences = new Map(modData, mapPackage).Rules.Sequences;
			else if (!modData.DefaultSequences.TryGetValue(args[2], out sequences))
				throw new InvalidOperationException($"{args[2]} is not a valid tileset or map path");

			sequences.Preload();

			var count = 0;

			var sb = sequences.SpriteCache.SheetBuilders[SheetType.Indexed];
			foreach (var s in sb.AllSheets)
			{
				var max = s == sb.Current ? (int)sb.CurrentChannel + 1 : 4;
				for (var i = 0; i < max; i++)
					s.AsPng((TextureChannel)ChannelMasks[i], palette).Save($"{count++}.png");
			}

			sb = sequences.SpriteCache.SheetBuilders[SheetType.BGRA];
			foreach (var s in sb.AllSheets)
				s.AsPng().Save($"{count++}.png");

			Console.WriteLine("Saved [0..{0}].png", count - 1);
		}
	}
}
