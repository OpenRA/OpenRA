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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class RepairIndicator : IEffect
	{
		Actor building;
		Player player;
		string palettePrefix;
		Animation anim = new Animation("allyrepair");
		RepairableBuilding rb;

		public RepairIndicator(Actor building, string palettePrefix, Player player)
		{
			this.building = building;
			this.player = player;
			this.palettePrefix = palettePrefix;
			rb = building.Trait<RepairableBuilding>();
			anim.PlayRepeating("repair");
		}

		public void Tick(World world)
		{
			if (!building.IsInWorld || building.IsDead() ||
				rb.Repairer == null || rb.Repairer != player)
				world.AddFrameEndTask(w => w.Remove(this));

			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!building.Destroyed)
			{
				yield return new SpriteRenderable(anim.Image, building.CenterPosition, 0,
					wr.Palette(palettePrefix+player.InternalName), 1f);
			}
		}
	}
}
