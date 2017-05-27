#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class IronCurtainPaletteEffectInfo : ITraitInfo
	{
		public readonly string PaletteName = "invuln";
		public object Create(ActorInitializer init) { return new IronCurtainPaletteEffect(this); }
	}

	public class IronCurtainPaletteEffect : IPaletteModifier, ITick
	{
		int t;
		string paletteName;

		public IronCurtainPaletteEffect(IronCurtainPaletteEffectInfo info)
		{
			paletteName = info.PaletteName;
		}

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
		{
			// cos value is in range of [-1024, 1024].
			int val = 16 + (WAngle.FromDegrees(t).Cos() / 64); // [0, 32]
			var p = b[paletteName];

			// modify all colors except index 0 which is transparent color.
			for (int j = 1; j < Palette.Size; j++)
			{
				var color = Color.FromArgb(64, val, 0, 0);
				p.SetColor(j, color);
			}
		}

		public void Tick(Actor self)
		{
			// color cycling speed
			t = (t + 16) % 1024;
		}
	}
}
