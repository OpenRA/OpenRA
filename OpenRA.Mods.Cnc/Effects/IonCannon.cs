#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Effects
{
	public class IonCannon : IEffect
	{
		Target target;
		Animation anim;
		Actor firedBy;

		public IonCannon(Actor firedBy, World world, int2 location)
		{
			this.firedBy = firedBy;
			target = Target.FromCell(location);
			anim = new Animation("ionsfx");
			anim.PlayThen("idle", () => Finish(world));
		}

		public void Tick(World world) { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,
				target.CenterLocation - new float2(.5f * anim.Image.size.X, anim.Image.size.Y - Game.CellSize),
				"effect", (int)target.CenterLocation.Y);
		}

		void Finish( World world )
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(firedBy, "IonCannon", target.CenterLocation, 0);
		}
	}
}
