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

namespace OpenRA.Traits
{
	[Desc("Add this to the Player actor definition.")]
	public class PlayerColorPaletteInfo : ITraitInfo
	{
		[Desc("The name of the palette to base off.")]
		[PaletteReference] public readonly string BasePalette = null;

		[Desc("The prefix for the resulting player palettes")]
		[PaletteDefinition(true)] public readonly string BaseName = "player";

		[Desc("Remap these indices to player colors.")]
		public readonly int[] RemapIndex = { };

		[Desc("Luminosity range to span.")]
		public readonly float Ramp = 0.05f;

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PlayerColorPalette(this); }
	}

	public class PlayerColorPalette : ILoadsPlayerPalettes
	{
		readonly PlayerColorPaletteInfo info;

		public PlayerColorPalette(PlayerColorPaletteInfo info)
		{
			this.info = info;
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, HSLColor color, bool replaceExisting)
		{
			var remap = new PlayerColorRemap(info.RemapIndex, color, info.Ramp);
			var pal = new ImmutablePalette(wr.Palette(info.BasePalette).Palette, remap);
			wr.AddPalette(info.BaseName + playerName, pal, info.AllowModifiers, replaceExisting);
		}
	}
}
