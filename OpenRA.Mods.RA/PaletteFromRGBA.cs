#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA
{
	class PaletteFromRGBAInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Tileset = null;
		public readonly int R = 0;
		public readonly int G = 0;
		public readonly int B = 0;
		public readonly int A = 255;
		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PaletteFromRGBA(init.world, this); }
	}

	class PaletteFromRGBA : IPalette
	{
		readonly World world;
		readonly PaletteFromRGBAInfo info;
		public PaletteFromRGBA(World world, PaletteFromRGBAInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void InitPalette(WorldRenderer wr)
		{
			// Enable palette only for a specific tileset
			if (info.Tileset != null && info.Tileset.ToLowerInvariant() != world.Map.Tileset.ToLowerInvariant())
				return;

			var c = (uint)((info.A << 24) | (info.R << 16) | (info.G << 8) | info.B);
			wr.AddPalette(info.Name, new Palette(Exts.MakeArray(256, i => (i == 0) ? 0 : c)), info.AllowModifiers);
		}
	}
}
