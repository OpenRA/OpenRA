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
	public class PlayerColorPaletteInfo : TraitInfo
	{
		[PaletteReference]
		[Desc("The name of the palette to base off.")]
		public readonly string BasePalette = null;

		[PaletteDefinition(true)]
		[Desc("The prefix for the resulting player palettes")]
		public readonly string BaseName = "player";

		[Desc("Remap these indices to player colors.")]
		public readonly int[] RemapIndex = Array.Empty<int>();

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public override object Create(ActorInitializer init) { return new PlayerColorPalette(this); }
	}

	public class PlayerColorPalette : ILoadsPlayerPalettes
	{
		readonly PlayerColorPaletteInfo info;

		public PlayerColorPalette(PlayerColorPaletteInfo info)
		{
			this.info = info;
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, Color color, bool replaceExisting)
		{
			var (_, h, s, _) = color.ToAhsv();

			var remap = new PlayerColorRemap(info.RemapIndex.Length == 0 ? Enumerable.Range(0, 256).ToArray() : info.RemapIndex, h, s);
			var pal = new ImmutablePalette(wr.Palette(info.BasePalette).Palette, remap);
			wr.AddPalette(info.BaseName + playerName, pal, info.AllowModifiers, replaceExisting);
		}
	}
}
