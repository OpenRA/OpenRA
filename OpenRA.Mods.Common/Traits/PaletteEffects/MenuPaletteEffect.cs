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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Fades the world from/to black at the start/end of the game, and can (optionally) desaturate the world")]
	public class MenuPaletteEffectInfo : TraitInfo
	{
		[Desc("Time (in ticks) to fade between states")]
		public readonly int FadeLength = 10;

		[Desc("Effect style to fade to during gameplay. Accepts values of None or Desaturated.")]
		public readonly MenuPaletteEffect.EffectType Effect = MenuPaletteEffect.EffectType.None;

		[Desc("Effect style to fade to when opening the in-game menu. Accepts values of None, Black or Desaturated.")]
		public readonly MenuPaletteEffect.EffectType MenuEffect = MenuPaletteEffect.EffectType.None;

		public override object Create(ActorInitializer init) { return new MenuPaletteEffect(this); }
	}

	public class MenuPaletteEffect : IPaletteModifier, IRender, IWorldLoaded, INotifyGameLoaded
	{
		public enum EffectType { None, Black, Desaturated }
		public readonly MenuPaletteEffectInfo Info;

		EffectType from = EffectType.Black;
		EffectType to = EffectType.Black;

		float frac = 0;
		long startTime;
		long endTime;

		public MenuPaletteEffect(MenuPaletteEffectInfo info) { Info = info; }

		public void Fade(EffectType type)
		{
			startTime = Game.RunTime;
			endTime = startTime + Ui.Timestep * Info.FadeLength;
			frac = 1;

			from = to;
			to = type;
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			if (endTime == 0)
				yield break;

			frac = (endTime - Game.RunTime) * 1f / (endTime - startTime);
			if (frac < 0)
				frac = startTime = endTime = 0;

			yield break;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			yield break;
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
			if (to == EffectType.None && endTime == 0)
				return;

			foreach (var pal in palettes.Values)
			{
				for (var x = 0; x < Palette.Size; x++)
				{
					var orig = pal.GetColor(x);
					var t = ColorForEffect(to, orig);

					if (endTime == 0)
						pal.SetColor(x, t);
					else
					{
						var f = ColorForEffect(from, orig);
						pal.SetColor(x, Exts.ColorLerp(frac, t, f));
					}
				}
			}
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			// HACK: Defer fade-in until the GameLoaded notification for game saves
			if (!w.IsLoadingGameSave)
				Fade(Info.Effect);
		}

		void INotifyGameLoaded.GameLoaded(World world)
		{
			// HACK: Let the menu opening trigger the fade for game saves
			// to avoid glitches resulting from trying to trigger both
			// the standard and menu fades at the same time
			if (world.IsReplay)
				Fade(Info.Effect);
		}
	}
}
