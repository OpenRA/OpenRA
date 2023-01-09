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
	[Desc("Creates a single color palette without any base palette file.")]
	class PaletteFromRGBAInfo : TraitInfo, ITilesetSpecificPaletteInfo
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Internal palette name")]
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

		[Desc("Index set to be fully transparent/invisible.")]
		public readonly int TransparentIndex = 0;

		string ITilesetSpecificPaletteInfo.Tileset => Tileset;

		public override object Create(ActorInitializer init) { return new PaletteFromRGBA(init.World, this); }
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
			if (info.Tileset != null && !string.Equals(info.Tileset, world.Map.Tileset, StringComparison.InvariantCultureIgnoreCase))
				return;

			var a = info.A / 255f;
			var r = (int)(a * info.R + 0.5f).Clamp(0, 255);
			var g = (int)(a * info.G + 0.5f).Clamp(0, 255);
			var b = (int)(a * info.B + 0.5f).Clamp(0, 255);
			var c = (uint)Color.FromArgb(info.A, r, g, b).ToArgb();
			wr.AddPalette(info.Name, new ImmutablePalette(Enumerable.Range(0, Palette.Size).Select(i => (i == info.TransparentIndex) ? 0 : c)), info.AllowModifiers);
		}
	}
}
