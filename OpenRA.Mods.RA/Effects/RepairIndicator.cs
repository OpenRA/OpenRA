#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Effects
{
	class RepairIndicator : IEffect
	{
		readonly Actor building;
		readonly string palettePrefix;
		readonly Animation anim;
		readonly RepairableBuilding rb;
		int shownPlayer = 0;

		public RepairIndicator(Actor building, string palettePrefix)
		{
			this.building = building;
			this.palettePrefix = palettePrefix;

			rb = building.TraitOrDefault<RepairableBuilding>();
			anim = new Animation(building.World, "allyrepair");
			anim.Paused = () => !rb.RepairActive;

			CycleRepairer();
		}

		public void Tick(World world)
		{
			if (!building.IsInWorld || building.IsDead() || 
				rb == null || !rb.Repairers.Any()) 
				world.AddFrameEndTask(w => w.Remove(this));

			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (building.Destroyed || wr.world.FogObscures(building))
				return SpriteRenderable.None;

			return anim.Render(building.CenterPosition, 
				wr.Palette(palettePrefix + rb.Repairers[shownPlayer % rb.Repairers.Count].InternalName));
		}

		void CycleRepairer() 
		{
			anim.PlayThen("repair", CycleRepairer);

			if (++shownPlayer == rb.Repairers.Count)
				shownPlayer = 0;
		}
	}
}
