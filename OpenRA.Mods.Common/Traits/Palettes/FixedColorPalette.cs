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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Add this to the World actor definition.")]
	public class FixedColorPaletteInfo : TraitInfo
	{
		[PaletteReference]
		[Desc("The name of the palette to base off.")]
		public readonly string Base = TileSet.TerrainPaletteInternalName;

		[PaletteDefinition]
		[Desc("The name of the resulting palette")]
		public readonly string Name = "resources";

		[Desc("Remap these indices to pre-defined colors.")]
		public readonly int[] RemapIndex = Array.Empty<int>();

		[Desc("The fixed color to remap.")]
		public readonly Color Color;

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public override object Create(ActorInitializer init) { return new FixedColorPalette(this); }
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
			var (_, h, s, _) = info.Color.ToAhsv();

			var remap = new PlayerColorRemap(info.RemapIndex.Length == 0 ? Enumerable.Range(0, 256).ToArray() : info.RemapIndex, h, s);
			wr.AddPalette(info.Name, new ImmutablePalette(wr.Palette(info.Base).Palette, remap), info.AllowModifiers);
		}
	}
}
