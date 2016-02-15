#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This is an expanded JASC palette format aiming to have 32bit colors in a palette."
		+ "It also supports loading proper (legacy) JASC palettes."
		+ "For background transparency, please set your intended background color with an alpha value of 0.")]
	class PaletteFromJasc32FileInfo : ITraitInfo
	{
		[FieldLoader.Require, PaletteDefinition]
		[Desc("Internal palette name.")]
		public readonly string Name = null;

		[Desc("If defined, load the palette only for this tileset.")]
		public readonly string Tileset = null;

		[FieldLoader.Require]
		[Desc("Name of the file to load.")]
		public readonly string Filename = null;

		[Desc("Map listed indices to shadow. Ignores previous color.")]
		public readonly int[] ShadowIndex = { };

		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PaletteFromJasc32File(init.World, this); }
	}

	class PaletteFromJasc32File : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly World world;
		readonly PaletteFromJasc32FileInfo info;
		public PaletteFromJasc32File(World world, PaletteFromJasc32FileInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			var colors = new uint[Palette.Size];
			using (var s = Game.ModData.ModFiles.Open(info.Filename))
			{
				var lines = s.ReadAllLines().ToArray();
				if (lines[0] != "JASC-PAL")
					throw new InvalidDataException("File {0} is not a valid JASC palatte!".F(info.Filename));

				for (var i = 0; i < Palette.Size; i++)
				{
					var split = lines[i + 3].Split(' ');
					colors[i] = (split.Count() == 4)
						? (uint)Color.FromArgb(int.Parse(split[3]), int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2])).ToArgb()
						: (uint)Color.FromArgb(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2])).ToArgb();
				}
			}

			wr.AddPalette(info.Name, new ImmutablePalette(colors), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames
		{
			get
			{
				// Only expose the palette if it is available for the shellmap's tileset (which is a requirement for its use).
				if (info.Tileset == null || info.Tileset == world.TileSet.Id)
					yield return info.Name;
			}
		}
	}
}
