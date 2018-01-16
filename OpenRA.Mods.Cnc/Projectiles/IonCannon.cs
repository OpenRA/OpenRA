#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Effects
{
	public class IonCannon : IProjectile
	{
		readonly Target target;
		readonly Animation anim;
		readonly Player firedBy;
		readonly string palette;
		readonly WeaponInfo weapon;

		int weaponDelay;
		bool impacted = false;

		public IonCannon(Player firedBy, WeaponInfo weapon, World world, WPos launchPos, CPos location, string effect, string sequence, string palette, int delay)
		{
			this.firedBy = firedBy;
			this.weapon = weapon;
			this.palette = palette;
			weaponDelay = delay;
			target = Target.FromCell(world, location);
			anim = new Animation(world, effect);
			anim.PlayThen(sequence, () => Finish(world));

			if (weapon.Report != null && weapon.Report.Any())
				Game.Sound.Play(SoundType.World, weapon.Report.Random(firedBy.World.SharedRandom), launchPos);
		}

		public void Tick(World world)
		{
			anim.Tick();
			if (!impacted && weaponDelay-- <= 0)
			{
				weapon.Impact(target, firedBy.PlayerActor, Enumerable.Empty<int>());
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
