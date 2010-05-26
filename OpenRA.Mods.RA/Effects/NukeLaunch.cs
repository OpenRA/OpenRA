#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class NukeInfo : IProjectileInfo
	{
		public readonly string Image = null;
		public IEffect Create(ProjectileArgs args) { return null; }
	}

	class NukeLaunch : IEffect
	{
		readonly Actor silo;
		Animation anim;
		float2 pos;
		int2 targetLocation;
		readonly int targetAltitude = 400;
		int altitude;
		bool goingUp = true;

		public NukeLaunch(Actor silo, string weapon, int2 targetLocation)
		{
			this.silo = silo;
			this.targetLocation = targetLocation;
			anim = new Animation("nuke");
			anim.PlayRepeating("up");
			
			if (silo == null)
			{
				altitude = Game.world.Map.Height*Game.CellSize;
				StartDescent(Game.world);
			}
			else
				pos = silo.CenterLocation;
		}

		void StartDescent(World world)
		{
			pos = OpenRA.Traits.Util.CenterOfCell(targetLocation);
			anim = new Animation("nuke");
			anim.PlayRepeating("down");
			goingUp = false;
		}
		
		public void Tick(World world)
		{
			anim.Tick();

			if (goingUp)
			{
				altitude += 10;
				if (altitude >= world.Map.Height*Game.CellSize)
					StartDescent(world);
			}
			else
			{
				altitude -= 10;
				if (altitude <= 0)
				{
					// Trigger screen desaturate effect
					foreach (var a in Game.world.Queries.WithTrait<NukePaletteEffect>())
						a.Trait.Enable();
					
					Explode(world);
				}
			}
		}

		void Explode(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(silo, "Atomic", pos.ToInt2(), 0);
			world.WorldActor.traits.Get<ScreenShaker>().AddEffect(20, pos, 5);
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - 0.5f * anim.Image.size - new float2(0, altitude), "effect");
		}
	}
}
