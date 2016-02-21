#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	[Desc("Fades the world from/to black at the start/end of the game, and can (optionally) desaturate the world")]
	public class MenuPaletteEffectInfo : ITraitInfo
	{
		[Desc("Time (in ticks) to fade between states")]
		public readonly int FadeLength = 10;

		[Desc("Effect style to fade to during gameplay. Accepts values of None or Desaturated.")]
		public readonly MenuPaletteEffect.EffectType Effect = MenuPaletteEffect.EffectType.None;

		[Desc("Effect style to fade to when opening the in-game menu. Accepts values of None, Black or Desaturated.")]
		public readonly MenuPaletteEffect.EffectType MenuEffect = MenuPaletteEffect.EffectType.None;

		public object Create(ActorInitializer init) { return new MenuPaletteEffect(this); }
	}

	public class MenuPaletteEffect : IPaletteModifier, ITickRender, IWorldLoaded
	{
		public enum EffectType { None, Black, Desaturated }
		public readonly MenuPaletteEffectInfo Info;

		int remainingFrames;
		EffectType from = EffectType.Black;
		EffectType to = EffectType.Black;

		public MenuPaletteEffect(MenuPaletteEffectInfo info) { Info = info; }

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

		static Color ColorForEffect(EffectType t, Color orig)
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

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			if (to == EffectType.None && remainingFrames == 0)
				return;

			foreach (var pal in palettes.Values)
			{
				for (var x = 0; x < Palette.Size; x++)
				{
					var orig = pal.GetColor(x);
					var t = ColorForEffect(to, orig);

					if (remainingFrames == 0)
						pal.SetColor(x, t);
					else
					{
						var f = ColorForEffect(from, orig);
						pal.SetColor(x, Exts.ColorLerp((float)remainingFrames / Info.FadeLength, t, f));
					}
				}
			}
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			Fade(Info.Effect);
		}
	}
}
