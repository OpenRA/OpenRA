#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.TS
{
	[Desc("Adds the hard-coded shroud palette to the game")]
	class TSShroudPaletteInfo : ITraitInfo
	{
		[Desc("Internal palette name")]
		public readonly string Name = "shroud";

		public object Create(ActorInitializer init) { return new TSShroudPalette(this); }
	}

	class TSShroudPalette : ILoadsPalettes
	{
		readonly TSShroudPaletteInfo info;

		public TSShroudPalette(TSShroudPaletteInfo info) { this.info = info; }

		public void LoadPalettes(WorldRenderer wr)
		{
			Func<int, uint> makeColor = i =>
			{
				if (i < 128)
					return (uint)(int2.Lerp(255, 0, i, 127) << 24);
				return 0;
			};

			wr.AddPalette(info.Name, new ImmutablePalette(Enumerable.Range(0, Palette.Size).Select(i => makeColor(i))));
		}
	}
}
