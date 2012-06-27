﻿#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA
{
	class PaletteFromFileInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Tileset = null;
		public readonly string Filename = null;
		public readonly int[] ShadowIndex = { };

		public object Create(ActorInitializer init) { return new PaletteFromFile(init.world, this); }
	}

	class PaletteFromFile : IPalette
	{
		readonly World world;
		readonly PaletteFromFileInfo info;
		public PaletteFromFile(World world, PaletteFromFileInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void InitPalette( WorldRenderer wr )
		{
			if( info.Tileset == null || info.Tileset.ToLowerInvariant() == world.Map.Tileset.ToLowerInvariant() )
				wr.AddPalette( info.Name, new Palette( FileSystem.Open( info.Filename ), info.ShadowIndex ) );
		}
	}
}
