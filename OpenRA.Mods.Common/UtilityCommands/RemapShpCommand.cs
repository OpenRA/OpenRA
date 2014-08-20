#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Graphics;
using OpenRA.FileFormats;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class RemapShpCommand : IUtilityCommand
	{
		public string Name { get { return "--remap"; } }

		[Desc("SRCMOD:PAL DESTMOD:PAL SRCSHP DESTSHP", "Remap SHPs to another palette")]
		public void Run(ModData modData, string[] args)
		{
			var remap = new Dictionary<int, int>();

			/* the first 4 entries are fixed */
			for (var i = 0; i < 4; i++)
				remap[i] = i;

			var srcMod = args[1].Split(':')[0];

			Game.modData = new ModData(srcMod);
			GlobalFileSystem.LoadFromManifest(Game.modData.Manifest);
			var srcRules = Game.modData.RulesetCache.LoadDefaultRules();
			var srcPaletteInfo = srcRules.Actors["player"].Traits.Get<PlayerColorPaletteInfo>();
			var srcRemapIndex = srcPaletteInfo.RemapIndex;

			var destMod = args[2].Split(':')[0];
			Game.modData = new ModData(destMod);
			GlobalFileSystem.LoadFromManifest(Game.modData.Manifest);
			var destRules = Game.modData.RulesetCache.LoadDefaultRules();
			var destPaletteInfo = destRules.Actors["player"].Traits.Get<PlayerColorPaletteInfo>();
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

			var srcImage = ShpReader.Load(args[3]);

			using (var destStream = File.Create(args[4]))
				ShpReader.Write(destStream, srcImage.Size,
					srcImage.Frames.Select(im => im.Data.Select(px => (byte)remap[px]).ToArray()));
		}

		static int ColorDistance(uint a, uint b)
		{
			var ca = Color.FromArgb((int)a);
			var cb = Color.FromArgb((int)b);

			return Math.Abs((int)ca.R - (int)cb.R) +
				Math.Abs((int)ca.G - (int)cb.G) +
				Math.Abs((int)ca.B - (int)cb.B);
		}
	}
}
