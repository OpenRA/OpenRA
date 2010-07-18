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
	class PaletteFromFileInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Theater = null;
		public readonly string Filename = null;
		public readonly bool Transparent = true;

		public object Create(ActorInitializer init) { return new PaletteFromFile(init.world, this); }
	}

	class PaletteFromFile
	{
		public PaletteFromFile(World world, PaletteFromFileInfo info)
		{
			if (info.Theater == null || 
				info.Theater.ToLowerInvariant() == world.Map.Theater.ToLowerInvariant())
			{
				world.WorldRenderer.AddPalette(info.Name, 
					new Palette(FileSystem.Open(info.Filename), info.Transparent));
			}
		}
	}
}
