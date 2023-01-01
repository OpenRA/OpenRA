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
	using GUtil = OpenRA.Graphics.Util;

	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Used for bursted one-colored whole screen effects. Add this to the world actor.")]
	public class FlashPaletteEffectInfo : TraitInfo
	{
		public readonly HashSet<string> ExcludePalettes = new HashSet<string> { "cursor", "chrome", "colorpicker", "fog", "shroud" };

		[Desc("Measured in ticks.")]
		public readonly int Length = 20;

		public readonly Color Color = Color.White;

		[Desc("Set this when using multiple independent flash effects.")]
		public readonly string Type = null;

		public override object Create(ActorInitializer init) { return new FlashPaletteEffect(this); }
	}

	public class FlashPaletteEffect : IPaletteModifier, ITick
	{
		public readonly FlashPaletteEffectInfo Info;

		public FlashPaletteEffect(FlashPaletteEffectInfo info)
		{
			Info = info;
		}

		int remainingFrames;

		public void Enable(int ticks)
		{
			if (ticks == -1)
				remainingFrames = Info.Length;
			else
				remainingFrames = ticks;
		}

		void ITick.Tick(Actor self)
		{
			if (remainingFrames > 0)
				remainingFrames--;
		}

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			if (remainingFrames == 0)
				return;

			var frac = (float)remainingFrames / Info.Length;

			foreach (var pal in palettes)
			{
				for (var x = 0; x < Palette.Size; x++)
				{
					var orig = pal.Value.GetColor(x);
					var c = Info.Color;
					var color = Color.FromArgb(orig.A, ((int)c.R).Clamp(0, 255), ((int)c.G).Clamp(0, 255), ((int)c.B).Clamp(0, 255));
					var final = GUtil.PremultipliedColorLerp(frac, orig, GUtil.PremultiplyAlpha(Color.FromArgb(orig.A, color)));
					pal.Value.SetColor(x, final);
				}
			}
		}
	}
}
