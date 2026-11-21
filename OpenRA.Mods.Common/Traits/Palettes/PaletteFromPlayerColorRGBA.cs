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

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Create single color player palettes by mixing a base color with the player color.")]
	sealed class PaletteFromPlayerColorRGBAInfo : TraitInfo
	{
		[FieldLoader.Require]
		[PaletteDefinition(true)]
		[Desc("The prefix for the resulting player palettes")]
		public readonly string BaseName = null;

		[Desc("Base color (red component).")]
		public readonly int R = 0;

		[Desc("Base color (green component).")]
		public readonly int G = 0;

		[Desc("Base color (blue component).")]
		public readonly int B = 0;

		[Desc("Alpha channel (transparency) of the resulting palette.")]
		public readonly int A = 255;

		[Desc("How strongly the player color is mixed into the base color (0 - 1).")]
		public readonly float PlayerColorStrength = 0.1f;

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		[Desc("Index set to be fully transparent/invisible.")]
		public readonly int TransparentIndex = 0;

		public override object Create(ActorInitializer init) { return new PaletteFromPlayerColorRGBA(this); }
	}

	sealed class PaletteFromPlayerColorRGBA : ILoadsPlayerPalettes
	{
		readonly PaletteFromPlayerColorRGBAInfo info;

		public PaletteFromPlayerColorRGBA(PaletteFromPlayerColorRGBAInfo info) { this.info = info; }

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, Color color, bool replaceExisting)
		{
			var s = info.PlayerColorStrength.Clamp(0f, 1f);
			var a = info.A / 255f;
			var r = (int)(a * OpenRA.Graphics.Util.Lerp(info.R, color.R, s) + 0.5f).Clamp(0, 255);
			var g = (int)(a * OpenRA.Graphics.Util.Lerp(info.G, color.G, s) + 0.5f).Clamp(0, 255);
			var b = (int)(a * OpenRA.Graphics.Util.Lerp(info.B, color.B, s) + 0.5f).Clamp(0, 255);
			var c = Color.FromArgb(info.A, r, g, b).ToArgb();
			var pal = new ImmutablePalette(Enumerable.Range(0, Palette.Size).Select(i => i == info.TransparentIndex ? 0 : c));
			wr.AddPalette(info.BaseName + playerName, pal, info.AllowModifiers, replaceExisting);
		}
	}
}
