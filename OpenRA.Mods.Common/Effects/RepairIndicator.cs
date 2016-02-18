#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Effects
{
	class RepairIndicator : IEffect
	{
		readonly Actor building;
		readonly Animation anim;
		readonly RepairableBuilding rb;

		int shownPlayer = 0;

		public RepairIndicator(Actor building)
		{
			this.building = building;

			rb = building.Trait<RepairableBuilding>();
			anim = new Animation(building.World, rb.Info.IndicatorImage, () => !rb.RepairActive || rb.IsTraitDisabled);

			CycleRepairer();
		}

		public void Tick(World world)
		{
			if (!building.IsInWorld || building.IsDead || !rb.Repairers.Any())
				world.AddFrameEndTask(w => w.Remove(this));

			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (building.Disposed || rb.Repairers.Count == 0 || wr.World.FogObscures(building))
				return SpriteRenderable.None;

			PaletteReference palette;
			if (!string.IsNullOrEmpty(rb.Info.IndicatorPalette))
				palette = wr.Palette(rb.Info.IndicatorPalette);
			else
				palette = wr.Palette(rb.Info.IndicatorPalettePrefix + rb.Repairers[shownPlayer % rb.Repairers.Count].InternalName);

			return anim.Render(building.CenterPosition, palette);
		}

		void CycleRepairer()
		{
			anim.PlayThen(rb.Info.IndicatorSequence, CycleRepairer);

			if (++shownPlayer == rb.Repairers.Count)
				shownPlayer = 0;
		}
	}
}
