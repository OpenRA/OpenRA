#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Creates a greyscale palette without any base palette file.")]
	class PaletteFromGrayscaleInfo : TraitInfo, ITilesetSpecificPaletteInfo
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[Desc("If defined, load the palette only for this tileset.")]
		public readonly string Tileset = null;

		public readonly bool AllowModifiers = true;

		[Desc("Index set to be fully transparent/invisible.")]
		public readonly int TransparentIndex = 0;

		string ITilesetSpecificPaletteInfo.Tileset => Tileset;

		public override object Create(ActorInitializer init) { return new PaletteFromGrayscale(init.World, this); }
	}

	class PaletteFromGrayscale : ILoadsPalettes
	{
		readonly World world;
		readonly PaletteFromGrayscaleInfo info;
		public PaletteFromGrayscale(World world, PaletteFromGrayscaleInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			// Enable palette only for a specific tileset
			if (info.Tileset != null && !string.Equals(info.Tileset, world.Map.Tileset, StringComparison.InvariantCultureIgnoreCase))
				return;

			wr.AddPalette(info.Name, new ImmutablePalette(Enumerable.Range(0, Palette.Size).Select(i => (i == info.TransparentIndex) ? 0 : (uint)Color.FromArgb(255, i, i, i).ToArgb())), info.AllowModifiers);
		}
	}
}
