#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Create a palette by applying a scale and offset to the colors in another palette.")]
	class PaletteFromScaledPaletteInfo : ITraitInfo
	{
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[Desc("The name of the palette to base off.")]
		public readonly string BasePalette = null;

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		[Desc("Amount to scale the base palette colors by.")]
		public readonly float Scale = 1.0f;

		[Desc("Amount to offset the base palette colors by.")]
		public readonly int Offset = 0;

		public object Create(ActorInitializer init) { return new PaletteFromScaledPalette(this); }
	}

	class PaletteFromScaledPalette : ILoadsPalettes
	{
		readonly PaletteFromScaledPaletteInfo info;
		public PaletteFromScaledPalette(PaletteFromScaledPaletteInfo info) { this.info = info; }

		public void LoadPalettes(WorldRenderer wr)
		{
			var remap = new ScaledPaletteRemap(info.Scale, info.Offset);
			wr.AddPalette(info.Name, new ImmutablePalette(wr.Palette(info.BasePalette).Palette, remap), info.AllowModifiers);
		}
	}

	class ScaledPaletteRemap : IPaletteRemap
	{
		readonly float scale;
		readonly int offset;

		public ScaledPaletteRemap(float scale, int offset)
		{
			this.scale = scale;
			this.offset = offset;
		}

		public Color GetRemappedColor(Color original, int index)
		{
			return Color.FromArgb(original.A,
				(int)Exts.Clamp((int)(scale * original.R + offset), 0, 255),
				(int)Exts.Clamp((int)(scale * original.G + offset), 0, 255),
				(int)Exts.Clamp((int)(scale * original.B + offset), 0, 255));
		}
	}
}
