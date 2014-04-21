﻿#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.IO;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class FogPaletteFromR8Info : ITraitInfo
	{
		[Desc("Internal palette name")]
		public readonly string Name = null;
		[Desc("Filename to load")]
		public readonly string Filename = null;
		[Desc("Palette byte offset")]
		public readonly long Offset = 0;
		public readonly bool AllowModifiers = true;
		public readonly bool InvertColor = false;

		public object Create(ActorInitializer init) { return new FogPaletteFromR8(this); }
	}

	class FogPaletteFromR8 : IPalette
	{
		readonly FogPaletteFromR8Info info;
		public FogPaletteFromR8(FogPaletteFromR8Info info) { this.info = info; }

		public void InitPalette(WorldRenderer wr)
		{
			var colors = new uint[256];
			using (var s = GlobalFileSystem.Open(info.Filename))
			{
				s.Seek(info.Offset, SeekOrigin.Begin);

				for (var i = 0; i < 256; i++)
				{
					var packed = s.ReadUInt16();

					// Fog is rendered with half opacity
					colors[i] = (uint)((255 << 24) | ((packed & 0xF800) << 7) | ((packed & 0x7E0) << 4) | ((packed & 0x1f) << 2));

					if (info.InvertColor)
						colors[i] ^= 0x00FFFFFF;
				}
			}

			wr.AddPalette(info.Name, new Palette(colors), info.AllowModifiers);
		}
	}
}
