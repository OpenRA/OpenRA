#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class PaletteFromCurrentTheatreInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly bool Transparent = true;

		public object Create(ActorInitializer init) { return new PaletteFromCurrentTheatre(init.world, this); }
	}

	class PaletteFromCurrentTheatre
	{
		public PaletteFromCurrentTheatre(World world, PaletteFromCurrentTheatreInfo info)
		{
			world.WorldRenderer.AddPalette(info.Name, 
				new Palette(FileSystem.Open(world.TileSet.Palette), info.Transparent));
		}
	}
}
