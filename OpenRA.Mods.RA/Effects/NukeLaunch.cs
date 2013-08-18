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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	public class NukeLaunch : IEffect
	{
		readonly Player firedBy;
		Animation anim;
		WPos pos;
		CPos targetLocation;
		bool goingUp = true;
		string weapon;

		public NukeLaunch(Player firedBy, Actor silo, string weapon, WPos launchPos, CPos targetLocation)
		{
			this.firedBy = firedBy;
			this.targetLocation = targetLocation;
			this.weapon = weapon;
			anim = new Animation(weapon);
			anim.PlayRepeating("up");

			pos = launchPos;
			var weaponRules = Rules.Weapons[weapon.ToLowerInvariant()];
			if (weaponRules.Report != null && weaponRules.Report.Any())
				Sound.Play(weaponRules.Report.Random(firedBy.World.SharedRandom), pos);
			if (silo == null)
				StartDescent(firedBy.World);
		}

		void StartDescent(World world)
		{
			pos = targetLocation.CenterPosition + new WVec(0, 0, 1024*firedBy.World.Map.Bounds.Height);
			anim.PlayRepeating("down");
			goingUp = false;
		}

		public void Tick(World world)
		{
			anim.Tick();

			var delta = new WVec(0,0,427);
			if (goingUp)
			{
				pos += delta;
				if (pos.Z >= world.Map.Bounds.Height*1024)
					StartDescent(world);
			}
			else
			{
				pos -= delta;
				if (pos.Z <= 0)
					Explode(world);
			}
		}

		void Explode(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(firedBy.PlayerActor, weapon, pos);
			world.WorldActor.Trait<ScreenShaker>().AddEffect(20, pos, 5);

			foreach (var a in world.ActorsWithTrait<NukePaletteEffect>())
				a.Trait.Enable();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(pos, wr.Palette("effect"));
		}
	}
}
