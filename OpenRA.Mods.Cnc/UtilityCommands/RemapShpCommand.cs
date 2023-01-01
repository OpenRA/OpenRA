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
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.SpriteLoaders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	class RemapShpCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--remap";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 5;
		}

		[Desc("SRCMOD:PAL DESTMOD:PAL SRCSHP DESTSHP", "Remap SHPs to another palette")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var remap = new Dictionary<int, int>();

			/* the first 4 entries are fixed */
			for (var i = 0; i < 4; i++)
				remap[i] = i;

			var srcMod = args[1].Split(':')[0];
			var srcModData = new ModData(utility.Mods[srcMod], utility.Mods);
			Game.ModData = srcModData;

			var srcPaletteInfo = srcModData.DefaultRules.Actors[SystemActors.Player].TraitInfo<PlayerColorPaletteInfo>();
			var srcRemapIndex = srcPaletteInfo.RemapIndex;

			var destMod = args[2].Split(':')[0];
			var destModData = new ModData(utility.Mods[destMod], utility.Mods);
			Game.ModData = destModData;
			var destPaletteInfo = destModData.DefaultRules.Actors[SystemActors.Player].TraitInfo<PlayerColorPaletteInfo>();
			var destRemapIndex = destPaletteInfo.RemapIndex;
			var shadowIndex = Array.Empty<int>();

			// the remap range is always 16 entries, but their location and order changes
			for (var i = 0; i < 16; i++)
				remap[srcRemapIndex[i]] = destRemapIndex[i];

			// map everything else to the best match based on channel-wise distance
			var srcPalette = new ImmutablePalette(args[1].Split(':')[1], new[] { 0 }, shadowIndex);
			var destPalette = new ImmutablePalette(args[2].Split(':')[1], new[] { 0 }, shadowIndex);

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
