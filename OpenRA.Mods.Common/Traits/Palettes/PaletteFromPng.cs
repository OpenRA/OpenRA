#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Load a PNG and use its embedded palette.")]
	class PaletteFromPngInfo : TraitInfo, ITilesetSpecificPaletteInfo, IProvidesCursorPaletteInfo
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[Desc("If defined, load the palette only for this tileset.")]
		public readonly string Tileset = null;

		[FieldLoader.Require]
		[Desc("Filename to load")]
		public readonly string Filename = null;

		[Desc("Map listed indices to shadow. Ignores previous color.")]
		public readonly int[] ShadowIndex = Array.Empty<int>();

		public readonly bool AllowModifiers = true;

		[Desc("Whether this palette is available for cursors.")]
		public readonly bool CursorPalette = false;

		public override object Create(ActorInitializer init) { return new PaletteFromPng(init.World, this); }

		string ITilesetSpecificPaletteInfo.Tileset => Tileset;

		string IProvidesCursorPaletteInfo.Palette => CursorPalette ? Name : null;

		ImmutablePalette IProvidesCursorPaletteInfo.ReadPalette(IReadOnlyFileSystem fileSystem)
		{
			var png = new Png(fileSystem.Open(Filename));

			if (png.Palette == null)
				throw new InvalidOperationException($"Unable to load palette `{Name}` from non-paletted png `{Filename}`");

			var colors = new uint[Palette.Size];

			for (var i = 0; i < png.Palette.Length; i++)
				colors[i] = (uint)png.Palette[i].ToArgb();

			return new ImmutablePalette(colors);
		}
	}

	class PaletteFromPng : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly World world;
		readonly PaletteFromPngInfo info;
		public PaletteFromPng(World world, PaletteFromPngInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			if (info.Tileset != null && !string.Equals(info.Tileset, world.Map.Tileset, StringComparison.InvariantCultureIgnoreCase))
				return;

			wr.AddPalette(info.Name, ((IProvidesCursorPaletteInfo)info).ReadPalette(world.Map), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames
		{
			get
			{
				// Only expose the palette if it is available for the shellmap's tileset (which is a requirement for its use).
				if (info.Tileset == null || info.Tileset == world.Map.Rules.TerrainInfo.Id)
					yield return info.Name;
			}
		}
	}
}
