#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Load a JASC PaintShop Pro Palette .pal file.")]
	class PaletteFromJascFileInfo : ITraitInfo
	{
		[FieldLoader.Require, PaletteDefinition]
		[Desc("Palette name used internally.")]
		public readonly string Name = null;

		[Desc("If defined, load the palette only for this tileset.")]
		public readonly string Tileset = null;

		[FieldLoader.Require]
		[Desc("Name of the file to load.")]
		public readonly string Filename = null;

		[Desc("Map listed indices to shadow. Ignores previous color.")]
		public readonly int[] ShadowIndex = { };

		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PaletteFromJascFile(init.World, this); }
	}

	class PaletteFromJascFile : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly World world;
		readonly PaletteFromJascFileInfo info;

		public PaletteFromJascFile(World world, PaletteFromJascFileInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			var colors = new uint[Palette.Size];
			using (var s = world.Map.Open(info.Filename))
			{
				using (var lines = s.ReadAllLines().GetEnumerator())
				{
					if (lines == null)
						return;

					if (!lines.MoveNext() || lines.Current != "JASC-PAL" || !lines.MoveNext() || !lines.MoveNext())
						throw new InvalidDataException("File `{0}` is not a valid JASC palette.".F(info.Filename));

					if (int.Parse(lines.Current) != Palette.Size)
						throw new InvalidDataException("Only {0} color palettes are supported.".F(Palette.Size));

					byte r, g, b;
					var i = 0;

					while (lines.MoveNext() && i < Palette.Size)
					{
						var rgb = lines.Current.Split(' ');
						if (rgb.Length != 3)
							throw new InvalidDataException("Invalid RGB triplet: ({0})".F(string.Join(" ", rgb)));

						if (!byte.TryParse(rgb[0], out r))
							throw new InvalidDataException("Invalid R value: {0}".F(rgb[0]));

						if (!byte.TryParse(rgb[1], out g))
							throw new InvalidDataException("Invalid G value: {0}".F(rgb[1]));

						if (!byte.TryParse(rgb[2], out b))
							throw new InvalidDataException("Invalid B value: {0}".F(rgb[2]));

						colors[i] = (uint)Color.FromArgb(r, g, b).ToArgb();

						if (r == 0 && g == 0 && b == 0)
							colors[i] = 0;

						i++;
					}
				}
			}

			wr.AddPalette(info.Name, new ImmutablePalette(colors, info.ShadowIndex), info.AllowModifiers);
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
