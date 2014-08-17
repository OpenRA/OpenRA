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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Effects
{
	public class IonCannon : IEffect
	{
		readonly Target target;
		readonly Animation anim;
		readonly Player firedBy;
		readonly string palette;
		readonly string weapon;

		int weaponDelay;
		bool impacted = false;

		public IonCannon(Player firedBy, string weapon, World world, CPos location, string effect, string palette, int delay)
		{
			this.firedBy = firedBy;
			this.weapon = weapon;
			this.palette = palette;
			weaponDelay = delay;
			target = Target.FromCell(world, location);
			anim = new Animation(world, effect);
			anim.PlayThen("idle", () => Finish(world));
		}

		public void Tick(World world)
		{
			anim.Tick();
			if (!impacted && weaponDelay-- <= 0)
			{
				var weapon = world.Map.Rules.Weapons[this.weapon.ToLowerInvariant()];
				weapon.Impact(target.CenterPosition, firedBy.PlayerActor, Enumerable.Empty<int>());
				impacted = true;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(target.CenterPosition, wr.Palette(palette));
		}

		void Finish(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
		}
	}
}
