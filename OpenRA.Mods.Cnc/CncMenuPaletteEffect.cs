#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	public class CncMenuPaletteEffectInfo : ITraitInfo
	{
		public readonly int FadeLength = 10;

		public object Create(ActorInitializer init) { return new CncMenuPaletteEffect(this); }
	}

	public class CncMenuPaletteEffect : IPaletteModifier, ITickRender
	{
		public enum EffectType { None, Black, Desaturated }
		public readonly CncMenuPaletteEffectInfo Info;

		int remainingFrames;
		EffectType from = EffectType.Black;
		EffectType to = EffectType.Black;

		public CncMenuPaletteEffect(CncMenuPaletteEffectInfo info) { Info = info; }

		public void Fade(EffectType type)
		{
			remainingFrames = Info.FadeLength;
			from = to;
			to = type;
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (remainingFrames > 0)
				remainingFrames--;
		}

		Color ColorForEffect(EffectType t, Color orig)
		{
			switch (t)
			{
				case EffectType.Black:
					return Color.FromArgb(orig.A, Color.Black);
				case EffectType.Desaturated:
					var lum = (int)(255 * orig.GetBrightness());
					return Color.FromArgb(orig.A, lum, lum, lum);
				default:
				case EffectType.None:
					return orig;
			}
		}

		public void AdjustPalette(Dictionary<string, Palette> palettes)
		{
			if (to == EffectType.None && remainingFrames == 0)
				return;

			foreach (var pal in palettes)
			{
				for (var x = 0; x < 256; x++)
				{
					var orig = pal.Value.GetColor(x);
					var t = ColorForEffect(to, orig);

					if (remainingFrames == 0)
						pal.Value.SetColor(x, t);
					else
					{
						var f = ColorForEffect(from, orig);
						pal.Value.SetColor(x, Exts.ColorLerp((float)remainingFrames / Info.FadeLength, t, f));
					}
				}
			}
		}
	}
}
