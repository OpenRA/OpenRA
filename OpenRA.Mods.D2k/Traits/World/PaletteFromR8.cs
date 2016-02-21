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
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	class PaletteFromR8Info : ITraitInfo
	{
		[FieldLoader.Require, PaletteDefinition]
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Filename to load")]
		public readonly string Filename = null;

		[Desc("Palette byte offset")]
		public readonly long Offset = 0;

		public readonly bool AllowModifiers = true;
		public readonly bool InvertColor = false;

		public object Create(ActorInitializer init) { return new PaletteFromR8(this); }
	}

	class PaletteFromR8 : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly PaletteFromR8Info info;
		public PaletteFromR8(PaletteFromR8Info info) { this.info = info; }

		public void LoadPalettes(WorldRenderer wr)
		{
			var colors = new uint[Palette.Size];
			using (var s = wr.World.Map.Open(info.Filename))
			{
				s.Seek(info.Offset, SeekOrigin.Begin);

				for (var i = 0; i < Palette.Size; i++)
				{
					var packed = s.ReadUInt16();
					colors[i] = (uint)((255 << 24) | ((packed & 0xF800) << 8) | ((packed & 0x7E0) << 5) | ((packed & 0x1f) << 3));

					if (info.InvertColor)
						colors[i] ^= 0x00FFFFFF;
				}
			}

			wr.AddPalette(info.Name, new ImmutablePalette(colors), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames { get { yield return info.Name; } }
	}
}
