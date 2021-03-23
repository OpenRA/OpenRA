#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Effects
{
	public class DropPodImpact : IProjectile
	{
		readonly Target target;
		readonly Animation entryAnimation;
		readonly Player firedBy;
		readonly string entryPalette;
		readonly WeaponInfo weapon;
		readonly WPos launchPos;

		int weaponDelay;
		bool impacted = false;

		public DropPodImpact(Player firedBy, WeaponInfo weapon, World world, WPos launchPos, in Target target,
			int delay, string entryEffect, string entrySequence, string entryPalette)
		{
			this.target = target;
			this.firedBy = firedBy;
			this.weapon = weapon;
			this.entryPalette = entryPalette;
			weaponDelay = delay;
			this.launchPos = launchPos;

			entryAnimation = new Animation(world, entryEffect);
			entryAnimation.PlayThen(entrySequence, () => Finish(world));

			if (weapon.Report != null && weapon.Report.Any())
				Game.Sound.Play(SoundType.World, weapon.Report, world, launchPos);
		}

		public void Tick(World world)
		{
			entryAnimation.Tick();

			if (!impacted && weaponDelay-- <= 0)
			{
				var warheadArgs = new WarheadArgs
				{
					Weapon = weapon,
					Source = target.CenterPosition,
					SourceActor = firedBy.PlayerActor,
					WeaponTarget = target
				};

				weapon.Impact(target, warheadArgs);
				impacted = true;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return entryAnimation.Render(launchPos, wr.Palette(entryPalette));
		}

		void Finish(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
		}
	}
}
