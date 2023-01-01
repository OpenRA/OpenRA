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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Create a color picker palette from another palette.")]
	class ColorPickerPaletteInfo : TraitInfo
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Internal palette name.")]
		public readonly string Name = null;

		[PaletteReference]
		[FieldLoader.Require]
		[Desc("The name of the palette to base off.")]
		public readonly string BasePalette = null;

		[Desc("Remap these indices to player colors.")]
		public readonly int[] RemapIndex = Array.Empty<int>();

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public override object Create(ActorInitializer init) { return new ColorPickerPalette(this); }
	}

	class ColorPickerPalette : ILoadsPalettes, IProvidesAssetBrowserColorPickerPalettes, ITickRender
	{
		readonly ColorPickerPaletteInfo info;
		readonly ColorPickerManagerInfo colorManager;
		Color color;

		public ColorPickerPalette(ColorPickerPaletteInfo info)
		{
			// All users need to use the same TraitInfo instance, chosen as the default mod rules
			colorManager = Game.ModData.DefaultRules.Actors[SystemActors.World].TraitInfo<ColorPickerManagerInfo>();
			this.info = info;
		}

		void ILoadsPalettes.LoadPalettes(WorldRenderer wr)
		{
			color = colorManager.Color;
			var (_, h, s, _) = color.ToAhsv();
			var remap = new PlayerColorRemap(info.RemapIndex.Length == 0 ? Enumerable.Range(0, 256).ToArray() : info.RemapIndex, h, s);
			wr.AddPalette(info.Name, new ImmutablePalette(wr.Palette(info.BasePalette).Palette, remap), info.AllowModifiers);
		}

		IEnumerable<string> IProvidesAssetBrowserColorPickerPalettes.ColorPickerPaletteNames { get { yield return info.Name; } }

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (color == colorManager.Color)
				return;

			color = colorManager.Color;
			var (_, h, s, _) = color.ToAhsv();
			var remap = new PlayerColorRemap(info.RemapIndex.Length == 0 ? Enumerable.Range(0, 256).ToArray() : info.RemapIndex, h, s);
			wr.ReplacePalette(info.Name, new ImmutablePalette(wr.Palette(info.BasePalette).Palette, remap));
		}
	}
}
