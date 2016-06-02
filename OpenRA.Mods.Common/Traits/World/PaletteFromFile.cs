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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class PaletteFromFileInfo : ITraitInfo
	{
		[FieldLoader.Require, PaletteDefinition]
		[Desc("internal palette name")]
		public readonly string Name = null;

		[Desc("If defined, load the palette only for this tileset.")]
		public readonly string Tileset = null;

		[FieldLoader.Require]
		[Desc("filename to load")]
		public readonly string Filename = null;

		[Desc("Map listed indices to shadow. Ignores previous color.")]
		public readonly int[] ShadowIndex = { };

		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PaletteFromFile(init.World, this); }
	}

	class PaletteFromFile : ILoadsPalettes, IProvidesAssetBrowserPalettes
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
				wr.AddPalette(info.Name, new ImmutablePalette(world.Map.Open(info.Filename), info.ShadowIndex), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames
		{
			get
			{
				// Only expose the palette if it is available for the shellmap's tileset (which is a requirement for its use).
				if (info.Tileset == null || info.Tileset == world.Map.Rules.TileSet.Id)
					yield return info.Name;
			}
		}
	}
}
