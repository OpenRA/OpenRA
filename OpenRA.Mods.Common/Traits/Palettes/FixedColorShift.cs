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

using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Mobius.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Apply a fixed color shift to a palette. Use this to add RGBA compatibility to FixedColorPalette.")]
	public class FixedColorShiftInfo : TraitInfo
	{
		[PaletteReference]
		[FieldLoader.Require]
		[Desc("The name of the palette to base off.")]
		public readonly string BasePalette = null;

		[Desc("The fixed color to remap.")]
		public readonly Color Color;

		[Desc("Hues between this and MaxHue will be shifted.")]
		public readonly float MinHue = 0.29f;

		[Desc("Hues between MinHue and this will be shifted.")]
		public readonly float MaxHue = 0.37f;

		[Desc("Hue reference for the color shift.")]
		public readonly float ReferenceHue = 0.33f;

		[Desc("Saturation reference for the color shift.")]
		public readonly float ReferenceSaturation = 0.925f;

		[Desc("Value reference for the color shift.")]
		public readonly float ReferenceValue = 0.95f;

		public override object Create(ActorInitializer init) { return new FixedColorShift(this); }
	}

	public class FixedColorShift : ILoadsPalettes
	{
		readonly FixedColorShiftInfo info;

		public FixedColorShift(FixedColorShiftInfo info)
		{
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			var (r, g, b) = info.Color.ToLinear();
			var (h, s, v) = Color.RgbToHsv(r, g, b);
			wr.SetPaletteColorShift(info.BasePalette,
				h - info.ReferenceHue, s - info.ReferenceSaturation, v / info.ReferenceValue,
				info.MinHue, info.MaxHue);
		}
	}
}
