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
		public readonly int Amplitude = 32;
		public readonly int Offset = 32;

		[Desc("Cycle speed in game angles per tick. 360 degrees = 1024 game degrees.")]
		public readonly int CycleSpeed = 16;
		public object Create(ActorInitializer init) { return new IronCurtainPaletteEffect(this); }
	}

	public class IronCurtainPaletteEffect : IPaletteModifier, ITick
	{
		int t;
		IronCurtainPaletteEffectInfo info;

		public IronCurtainPaletteEffect(IronCurtainPaletteEffectInfo info)
		{
			this.info = info;
			System.Diagnostics.Debug.Assert(info.Offset >= info.Amplitude, "Offset should be GE than amplitude.");
		}

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
		{
			// cos value is in range of [-1024, 1024].
			int val = info.Offset + info.Amplitude * WAngle.FromDegrees(t).Cos() / 1024;
			var p = b[info.PaletteName];

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
			t = (t + info.CycleSpeed) % 1024;
		}
	}
}
