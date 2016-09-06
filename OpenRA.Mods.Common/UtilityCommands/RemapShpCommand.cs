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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class RemapShpCommand : IUtilityCommand
	{
		public string Name { get { return "--remap"; } }

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 5;
		}

		[Desc("SRCMOD:PAL DESTMOD:PAL SRCSHP DESTSHP", "Remap SHPs to another palette")]
		public void Run(ModData modData, string[] args)
		{
			var remap = new Dictionary<int, int>();

			/* the first 4 entries are fixed */
			for (var i = 0; i < 4; i++)
				remap[i] = i;

			var srcMod = args[1].Split(':')[0];
			var srcModData = new ModData(srcMod);
			Game.ModData = srcModData;

			var srcPaletteInfo = srcModData.DefaultRules.Actors["player"].TraitInfo<PlayerColorPaletteInfo>();
			var srcRemapIndex = srcPaletteInfo.RemapIndex;

			var destMod = args[2].Split(':')[0];
			var destModData = new ModData(destMod);
			Game.ModData = destModData;
			var destPaletteInfo = destModData.DefaultRules.Actors["player"].TraitInfo<PlayerColorPaletteInfo>();
			var destRemapIndex = destPaletteInfo.RemapIndex;
			var shadowIndex = new int[] { };

			// the remap range is always 16 entries, but their location and order changes
			for (var i = 0; i < 16; i++)
				remap[PlayerColorRemap.GetRemapIndex(srcRemapIndex, i)]
					= PlayerColorRemap.GetRemapIndex(destRemapIndex, i);

			// map everything else to the best match based on channel-wise distance
			var srcPalette = new ImmutablePalette(args[1].Split(':')[1], shadowIndex);
			var destPalette = new ImmutablePalette(args[2].Split(':')[1], shadowIndex);

			for (var i = 0; i < Palette.Size; i++)
				if (!remap.ContainsKey(i))
					remap[i] = Enumerable.Range(0, Palette.Size)
						.Where(a => !remap.ContainsValue(a))
						.MinBy(a => ColorDistance(destPalette[a], srcPalette[i]));

			using (var s = File.OpenRead(args[3]))
			using (var destStream = File.Create(args[4]))
			{
				var srcImage = new ShpTDSprite(s);
				ShpTDSprite.Write(destStream, srcImage.Size,
					srcImage.Frames.Select(im => im.Data.Select(px => (byte)remap[px]).ToArray()));
			}
		}

		static int ColorDistance(uint a, uint b)
		{
			var ca = Color.FromArgb((int)a);
			var cb = Color.FromArgb((int)b);

			return Math.Abs(ca.R - cb.R) +
				Math.Abs(ca.G - cb.G) +
				Math.Abs(ca.B - cb.B);
		}
	}
}
