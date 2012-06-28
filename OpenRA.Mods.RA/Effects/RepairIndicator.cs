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
		Animation anim = new Animation("allyrepair");

		public RepairIndicator(Actor building, Player player)
		{
			this.building = building;
			this.player = player;
			anim.PlayRepeating("repair");
		}

		public void Tick(World world)
		{
			if (!building.IsInWorld ||
				building.IsDead() ||
				building.Trait<RepairableBuilding>().Repairer == null ||
				building.Trait<RepairableBuilding>().Repairer != player)
				world.AddFrameEndTask(w => w.Remove(this));

			anim.Tick();
		}

		public IEnumerable<Renderable> Render()
		{
			if (!building.Destroyed)
			{
				var palette = building.Trait<RenderSimple>().Palette(player);

				yield return new Renderable(anim.Image,
					building.CenterLocation.ToFloat2() - .5f * anim.Image.size, palette, (int)building.CenterLocation.Y);
			}
		}
	}
}
