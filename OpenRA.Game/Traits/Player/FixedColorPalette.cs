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
	[Desc("Add this to the World actor definition.")]
	public class FixedColorPaletteInfo : ITraitInfo
	{
		[Desc("The name of the palette to base off.")]
		[PaletteReference] public readonly string Base = TileSet.TerrainPaletteInternalName;

		[Desc("The name of the resulting palette")]
		[PaletteDefinition] public readonly string Name = "resources";

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

	public class FixedColorPalette : ILoadsPalettes
	{
		readonly FixedColorPaletteInfo info;

		public FixedColorPalette(FixedColorPaletteInfo info)
		{
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			var remap = new PlayerColorRemap(info.RemapIndex, info.Color, info.Ramp);
			wr.AddPalette(info.Name, new ImmutablePalette(wr.Palette(info.Base).Palette, remap), info.AllowModifiers);
		}
	}
}
