#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class PaletteFromFileInfo : ITraitInfo
	{
		[Desc("internal palette name")]
		public readonly string Name = null;
		[Desc("If defined, load the palette only for this tileset.")]
		public readonly string Tileset = null;
		[Desc("filename to load")]
		public readonly string Filename = null;
		[Desc("Map listed indices to shadow. Ignores previous color.")]
		public readonly int[] ShadowIndex = { };
		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PaletteFromFile(init.world, this); }
	}

	class PaletteFromFile : ILoadsPalettes
	{
		readonly World world;
		readonly PaletteFromFileInfo info;
		public PaletteFromFile(World world, PaletteFromFileInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			if (info.Tileset == null || info.Tileset.ToLowerInvariant() == world.Map.Tileset.ToLowerInvariant())
				wr.AddPalette(info.Name, new ImmutablePalette(GlobalFileSystem.Open(info.Filename), info.ShadowIndex), info.AllowModifiers);
		}

		public string Filename
		{
			get { return info.Filename; }
		}

		public string Name
		{
			get { return info.Name; }
		}
	}
}
