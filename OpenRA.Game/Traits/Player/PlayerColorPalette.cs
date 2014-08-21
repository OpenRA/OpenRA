#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;

namespace OpenRA.Traits
{
	[Desc("Add this to the Player actor definition.")]
	public class PlayerColorPaletteInfo : ITraitInfo
	{
		[Desc("The name of the palette to base off.")]
		public readonly string BasePalette = null;
		[Desc("The prefix for the resulting player palettes")]
		public readonly string BaseName = "player";
		[Desc("Remap these indices to player colors.")]
		public readonly int[] RemapIndex = { };
		[Desc("Luminosity range to span.")]
		public readonly float Ramp = 0.05f;
		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PlayerColorPalette(init.self.Owner, this); }
	}

	public class PlayerColorPalette : ILoadsPalettes
	{
		readonly Player owner;
		readonly PlayerColorPaletteInfo info;

		public PlayerColorPalette(Player owner, PlayerColorPaletteInfo info)
		{
			this.owner = owner;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			var remap = new PlayerColorRemap(info.RemapIndex, owner.Color, info.Ramp);
			wr.AddPalette(info.BaseName + owner.InternalName, new ImmutablePalette(wr.Palette(info.BasePalette).Palette, remap), info.AllowModifiers);
		}
	}
}
