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
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Palette effect used for blinking \"animations\" on actors.")]
	class LightPaletteRotatorInfo : TraitInfo
	{
		[Desc("Palettes this effect should not apply to.")]
		public readonly HashSet<string> ExcludePalettes = new HashSet<string>();

		[Desc("'Speed' at which the effect cycles through palette indices.")]
		public readonly float TimeStep = .5f;

		[Desc("Palette index to map to rotating color indices.")]
		public readonly int ModifyIndex = 103;

		[Desc("Palette indices to rotate through.")]
		public readonly int[] RotationIndices = { 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 238, 237, 236, 235, 234, 233, 232, 231 };

		public override object Create(ActorInitializer init) { return new LightPaletteRotator(this); }
	}

	class LightPaletteRotator : ITick, IPaletteModifier
	{
		readonly LightPaletteRotatorInfo info;
		float t = 0;

		public LightPaletteRotator(LightPaletteRotatorInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			t += info.TimeStep;
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			foreach (var pal in palettes)
			{
				if (info.ExcludePalettes.Contains(pal.Key))
					continue;

				var rotate = (int)t % info.RotationIndices.Length;
				pal.Value.SetColor(info.ModifyIndex, pal.Value.GetColor(info.RotationIndices[rotate]));
			}
		}
	}
}
