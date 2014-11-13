#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("Creates a single color palette without any base palette file.")]
	class PaletteFromRGBAInfo : ITraitInfo
	{
		[Desc("internal palette name")]
		public readonly string Name = null;
		[Desc("If defined, load the palette only for this tileset.")]
		public readonly string Tileset = null;
		[Desc("red color component")]
		public readonly int R = 0;
		[Desc("green color component")]
		public readonly int G = 0;
		[Desc("blue color component")]
		public readonly int B = 0;
		[Desc("alpha channel (transparency)")]
		public readonly int A = 255;
		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PaletteFromRGBA(init.world, this); }
	}

	class PaletteFromRGBA : ILoadsPalettes
	{
		readonly World world;
		readonly PaletteFromRGBAInfo info;
		public PaletteFromRGBA(World world, PaletteFromRGBAInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			// Enable palette only for a specific tileset
			if (info.Tileset != null && info.Tileset.ToLowerInvariant() != world.Map.Tileset.ToLowerInvariant())
				return;

			var c = (uint)((info.A << 24) | (info.R << 16) | (info.G << 8) | info.B);
			wr.AddPalette(info.Name, new ImmutablePalette(Enumerable.Range(0, Palette.Size).Select(i => (i == 0) ? 0 : c)), info.AllowModifiers);
		}
	}
}
