﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Adds the hard-coded shroud palette to the game")]
	class ShroudPaletteInfo : ITraitInfo
	{
		[Desc("internal palette name")]
		public readonly string Name = "shroud";
		public object Create(ActorInitializer init) { return new ShroudPalette(this); }
	}

	class ShroudPalette : IPalette
	{
		readonly ShroudPaletteInfo info;

		public ShroudPalette(ShroudPaletteInfo info) { this.info = info; }

		public void InitPalette(WorldRenderer wr)
		{
			var c = new[] {
				Color.Transparent, Color.Green,
				Color.Blue, Color.Yellow,
				Color.Black,
				Color.FromArgb(128,0,0,0),
				Color.Transparent,
				Color.Transparent
			};

			wr.AddPalette(info.Name, new Palette(Exts.MakeArray(256, i => (uint)c[i % 8].ToArgb())), false);
		}
	}
}
