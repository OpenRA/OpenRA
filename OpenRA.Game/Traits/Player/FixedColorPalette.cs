#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	[Desc("Add this to the World actor definition.")]
	public class FixedColorPaletteInfo : ITraitInfo
	{
		[Desc("The name of the palette to base off.")]
		public readonly string Base = "terrain";
		[Desc("The name of the resulting palette")]
		public readonly string Name = "resources";
		[Desc("Remap these indices to pre-defined colors.")]
		public readonly int[] RemapIndex = { };
		[Desc("The fixed color to remap.")]
		public readonly HSLColor Color;
		[Desc("Luminosity range to span.")]
		public readonly float Ramp = 0.05f;
		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new FixedColorPalette(this); }
	}

	public class FixedColorPalette : IPalette
	{
		readonly FixedColorPaletteInfo info;

		public FixedColorPalette(FixedColorPaletteInfo info)
		{
			this.info = info;
		}

		public void InitPalette(WorldRenderer wr)
		{
			var remap = new PlayerColorRemap(info.RemapIndex, info.Color, info.Ramp);
			wr.AddPalette(info.Name, new Palette(wr.Palette(info.Base).Palette, remap), info.AllowModifiers);
		}
	}
}
