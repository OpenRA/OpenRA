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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Create player palettes by applying alpha transparency to another player palette.")]
	class PaletteFromPlayerPaletteWithAlphaInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("The prefix for the resulting player palettes")]
		[PaletteDefinition(true)] public readonly string BaseName = null;

		[FieldLoader.Require]
		[Desc("The name of the player palette to base off.")]
		[PaletteReference(true)] public readonly string BasePalette = null;

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		[Desc("Alpha component that is applied to the base palette.")]
		public readonly float Alpha = 1.0f;

		[Desc("Premultiply color by the alpha component.")]
		public readonly bool Premultiply = true;

		public object Create(ActorInitializer init) { return new PaletteFromPlayerPaletteWithAlpha(this); }
	}

	class PaletteFromPlayerPaletteWithAlpha : ILoadsPlayerPalettes
	{
		readonly PaletteFromPlayerPaletteWithAlphaInfo info;

		public PaletteFromPlayerPaletteWithAlpha(PaletteFromPlayerPaletteWithAlphaInfo info) { this.info = info; }

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, HSLColor color, bool replaceExisting)
		{
			var remap = new AlphaPaletteRemap(info.Alpha, info.Premultiply);
			var pal = new ImmutablePalette(wr.Palette(info.BasePalette + playerName).Palette, remap);
			wr.AddPalette(info.BaseName + playerName, pal, info.AllowModifiers, replaceExisting);
		}
	}
}
