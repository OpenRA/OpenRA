#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Adds the hard-coded shroud palette to the game")]
	class TSShroudPaletteInfo : TraitInfo
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Internal palette name")]
		public readonly string Name = "shroud";

		public override object Create(ActorInitializer init) { return new TSShroudPalette(this); }
	}

	class TSShroudPalette : ILoadsPalettes, IProvidesAssetBrowserPalettes
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

		public IEnumerable<string> PaletteNames { get { yield return info.Name; } }
	}
}
