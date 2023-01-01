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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	class D2kFogPaletteInfo : TraitInfo
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[PaletteReference]
		[FieldLoader.Require]
		[Desc("The name of the shroud palette to base off.")]
		public readonly string BasePalette = null;

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public override object Create(ActorInitializer init) { return new D2kFogPalette(this); }
	}

	class D2kFogPalette : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly D2kFogPaletteInfo info;
		public D2kFogPalette(D2kFogPaletteInfo info) { this.info = info; }

		public void LoadPalettes(WorldRenderer wr)
		{
			var basePalette = wr.Palette(info.BasePalette).Palette;

			// Bit twiddling is equivalent to unpacking RGB channels, dividing them by 2, subtracting from 255, then repacking
			var fog = new uint[Palette.Size];
			for (var i = 0; i < Palette.Size; i++)
				fog[i] = ~((basePalette[i] >> 1) & 0x007F7F7F);

			wr.AddPalette(info.Name, new ImmutablePalette(fog), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames { get { yield return info.Name; } }
	}
}
