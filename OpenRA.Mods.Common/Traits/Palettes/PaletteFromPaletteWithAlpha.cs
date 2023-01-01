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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Create a palette by applying alpha transparency to another palette.")]
	class PaletteFromPaletteWithAlphaInfo : TraitInfo
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[PaletteReference]
		[FieldLoader.Require]
		[Desc("The name of the palette to base off.")]
		public readonly string BasePalette = null;

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		[Desc("Alpha component that is applied to the base palette.")]
		public readonly float Alpha = 1.0f;

		[Desc("Premultiply color by the alpha component.")]
		public readonly bool Premultiply = true;

		public override object Create(ActorInitializer init) { return new PaletteFromPaletteWithAlpha(this); }
	}

	class PaletteFromPaletteWithAlpha : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly PaletteFromPaletteWithAlphaInfo info;

		public PaletteFromPaletteWithAlpha(PaletteFromPaletteWithAlphaInfo info) { this.info = info; }

		public void LoadPalettes(WorldRenderer wr)
		{
			var remap = new AlphaPaletteRemap(info.Alpha, info.Premultiply);
			wr.AddPalette(info.Name, new ImmutablePalette(wr.Palette(info.BasePalette).Palette, remap), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames { get { yield return info.Name; } }
	}

	class AlphaPaletteRemap : IPaletteRemap
	{
		readonly float alpha;
		readonly bool premultiply;

		public AlphaPaletteRemap(float alpha, bool premultiply)
		{
			this.alpha = alpha;
			this.premultiply = premultiply;
		}

		public Color GetRemappedColor(Color original, int index)
		{
			var a = (int)(original.A * alpha).Clamp(0, 255);
			var r = premultiply ? (int)(alpha * original.R + 0.5f).Clamp(0, 255) : original.R;
			var g = premultiply ? (int)(alpha * original.G + 0.5f).Clamp(0, 255) : original.G;
			var b = premultiply ? (int)(alpha * original.B + 0.5f).Clamp(0, 255) : original.B;

			return Color.FromArgb(a, r, g, b);
		}
	}
}
