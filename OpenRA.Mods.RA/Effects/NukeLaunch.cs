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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	public class NukeLaunch : IEffect
	{
		readonly Player firedBy;
		Animation anim;
		PPos pos;
		CPos targetLocation;
		int altitude;
		bool goingUp = true;
		string weapon;

		public NukeLaunch(Player firedBy, Actor silo, string weapon, PVecInt spawnOffset, CPos targetLocation)
		{
			this.firedBy = firedBy;
			this.targetLocation = targetLocation;
			this.weapon = weapon;
			anim = new Animation(weapon);
			anim.PlayRepeating("up");

			if (silo == null)
			{
				altitude = firedBy.World.Map.Bounds.Height*Game.CellSize;
				StartDescent(firedBy.World);
			}
			else
				pos = silo.CenterLocation + spawnOffset;
		}

		void StartDescent(World world)
		{
			pos = OpenRA.Traits.Util.CenterOfCell(targetLocation);
			anim.PlayRepeating("down");
			goingUp = false;
		}

		public void Tick(World world)
		{
			anim.Tick();

			if (goingUp)
			{
				altitude += 10;
				if (altitude >= world.Map.Bounds.Height*Game.CellSize)
					StartDescent(world);
			}
			else
			{
				altitude -= 10;
				if (altitude <= 0)
					Explode(world);
			}
		}

		void Explode(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(firedBy.PlayerActor, weapon, pos, 0);
			world.WorldActor.Trait<ScreenShaker>().AddEffect(20, pos.ToFloat2(), 5);

			foreach (var a in world.ActorsWithTrait<NukePaletteEffect>())
				a.Trait.Enable();
		}

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			yield return new Renderable(anim.Image, pos.ToFloat2() - 0.5f * anim.Image.size - new float2(0, altitude),
				wr.Palette("effect"), (int)pos.Y);
		}
	}
}
