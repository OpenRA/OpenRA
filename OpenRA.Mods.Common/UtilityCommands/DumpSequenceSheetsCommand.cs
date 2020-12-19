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
using OpenRA.FileSystem;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class DumpSequenceSheetsCommand : IUtilityCommand
	{
		static readonly int[] ChannelMasks = { 2, 1, 0, 3 };

		string IUtilityCommand.Name { get { return "--dump-sequence-sheets"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		[Desc("PALETTE", "TILESET-OR-MAP", "Exports sequence texture atlas as a set of png images.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			var palette = new ImmutablePalette(args[1], new int[0]);

			SequenceProvider sequences = null;
			var mapPackage = new Folder(Platform.EngineDir).OpenPackage(args[2], modData.ModFiles);
			if (mapPackage != null)
				sequences = new Map(modData, mapPackage).Rules.Sequences;
			else if (!modData.DefaultSequences.TryGetValue(args[2], out sequences))
				throw new InvalidOperationException("{0} is not a valid tileset or map path".F(args[2]));

			sequences.Preload();

			var count = 0;

			var sb = sequences.SpriteCache.SheetBuilders[SheetType.Indexed];
			foreach (var s in sb.AllSheets)
			{
				var max = s == sb.Current ? (int)sb.CurrentChannel + 1 : 4;
				for (var i = 0; i < max; i++)
					s.AsPng((TextureChannel)ChannelMasks[i], palette).Save("{0}.png".F(count++));
			}

			sb = sequences.SpriteCache.SheetBuilders[SheetType.BGRA];
			foreach (var s in sb.AllSheets)
				s.AsPng().Save("{0}.png".F(count++));

			Console.WriteLine("Saved [0..{0}].png", count - 1);
		}
	}
}
