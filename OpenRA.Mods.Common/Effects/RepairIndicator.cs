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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Effects
{
	class RepairIndicator : IEffect
	{
		readonly Actor building;
		readonly Player player;
		readonly string palettePrefix;
		readonly Animation anim;
		readonly RepairableBuilding rb;

		public RepairIndicator(Actor building, string palettePrefix, Player player)
		{
			this.building = building;
			this.player = player;
			this.palettePrefix = palettePrefix;

			rb = building.TraitOrDefault<RepairableBuilding>();
			anim = new Animation(building.World, "allyrepair");
			anim.Paused = () => !rb.RepairActive;
			anim.PlayRepeating("repair");
		}

		public void Tick(World world)
		{
			if (!building.IsInWorld || building.IsDead() ||
				rb == null || rb.Repairer == null || rb.Repairer != player)
				world.AddFrameEndTask(w => w.Remove(this));

			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (building.Destroyed || wr.world.FogObscures(building))
				return SpriteRenderable.None;

			return anim.Render(building.CenterPosition,
					wr.Palette(palettePrefix+player.InternalName));
		}
	}
}
