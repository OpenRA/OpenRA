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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Effects
{
	public class GravityBombInfo : IProjectileInfo
	{
		public readonly string Image = null;
		public IEffect Create(ProjectileArgs args) { return new GravityBomb(this, args); }
	}

	public class GravityBomb : IEffect
	{
		Animation anim;
		int altitude;
		ProjectileArgs Args;

		public GravityBomb(GravityBombInfo info, ProjectileArgs args)
		{
			Args = args;
			altitude = args.srcAltitude;

			anim = new Animation(info.Image);
			if (anim.HasSequence("open"))
				anim.PlayThen("open", () => anim.PlayRepeating("idle"));
			else
				anim.PlayRepeating("idle");
		}

		public void Tick(World world)
		{
			if (--altitude <= Args.destAltitude)
			{
				world.AddFrameEndTask(w => w.Remove(this));
				Combat.DoImpacts(Args);
			}

			anim.Tick();
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, 
				Args.dest - new int2(0, altitude) - .5f * anim.Image.size, "effect");
		}
	}
}
