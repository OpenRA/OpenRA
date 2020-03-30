#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Load a GIMP .gpl or JASC .pal palette file. Supports per-color alpha. Index 0 is hardcoded to be fully transparent/invisible.")]
	class PaletteFromGimpOrJascFileInfo : ITraitInfo
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Palette name used internally.")]
		public readonly string Name = null;

		[Desc("Defines for which tileset IDs this palette should be loaded.",
			"If none specified, it applies to all tileset IDs not explicitly excluded.")]
		public readonly HashSet<string> Tilesets = new HashSet<string>();

		[Desc("Don't load palette for these tileset IDs.")]
		public readonly HashSet<string> ExcludeTilesets = new HashSet<string>();

		[FieldLoader.Require]
		[Desc("Name of the file to load.")]
		public readonly string Filename = null;

		[Desc("Premultiply colors with their alpha values.")]
		public readonly bool Premultiply = true;

		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PaletteFromGimpOrJascFile(init.World, this); }
	}

	class PaletteFromGimpOrJascFile : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly World world;
		readonly PaletteFromGimpOrJascFileInfo info;

		public PaletteFromGimpOrJascFile(World world, PaletteFromGimpOrJascFileInfo info)
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

					if (!lines.MoveNext() || (lines.Current != "GIMP Palette" && lines.Current != "JASC-PAL"))
						throw new InvalidDataException("File `{0}` is not a valid GIMP or JASC palette.".F(info.Filename));

					byte r, g, b, a;
					a = 255;
					var i = 0;

					while (lines.MoveNext() && i < Palette.Size)
					{
						// Skip until first color. Ignore # comments, Name/Columns and blank lines as well as JASC header values.
						if (string.IsNullOrEmpty(lines.Current) || !char.IsDigit(lines.Current.Trim()[0]) || lines.Current == "0100" || lines.Current == "256")
							continue;

						var rgba = lines.Current.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
						if (rgba.Length < 3)
							throw new InvalidDataException("Invalid RGB(A) triplet/quartet: ({0})".F(string.Join(" ", rgba)));

						if (!byte.TryParse(rgba[0], out r))
							throw new InvalidDataException("Invalid R value: {0}".F(rgba[0]));

						if (!byte.TryParse(rgba[1], out g))
							throw new InvalidDataException("Invalid G value: {0}".F(rgba[1]));

						if (!byte.TryParse(rgba[2], out b))
							throw new InvalidDataException("Invalid B value: {0}".F(rgba[2]));

						// Check if color has a (valid) alpha value.
						// Note: We can't throw on "rgba.Length > 3 but parse failed", because in GIMP palettes the 'invalid' value is probably a color name string.
						var noAlpha = rgba.Length > 3 ? !byte.TryParse(rgba[3], out a) : true;

						// Index 0 should always be completely transparent/background color
						if (i == 0)
							colors[i] = 0;
						else if (noAlpha)
							colors[i] = (uint)Color.FromArgb(r, g, b).ToArgb();
						else if (info.Premultiply)
							colors[i] = (uint)Color.FromArgb(a, r * a / 255, g * a / 255, b * a / 255).ToArgb();
						else
							colors[i] = (uint)Color.FromArgb(a, r, g, b).ToArgb();

						i++;
					}
				}
			}

			wr.AddPalette(info.Name, new ImmutablePalette(colors), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames
		{
			get
			{
				// Only expose the palette if it is available for the shellmap's tileset (which is a requirement for its use).
				if ((info.Tilesets.Count == 0 || info.Tilesets.Contains(world.Map.Rules.TileSet.Id))
					&& !info.ExcludeTilesets.Contains(world.Map.Rules.TileSet.Id))
					yield return info.Name;
			}
		}
	}
}
