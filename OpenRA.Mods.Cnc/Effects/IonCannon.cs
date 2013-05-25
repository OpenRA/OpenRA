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
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Effects
{
	public class IonCannon : IEffect
	{
		Target target;
		Animation anim;
		Actor firedBy;

		public IonCannon(Actor firedBy, World world, CPos location)
		{
			this.firedBy = firedBy;
			target = Target.FromCell(location);
			anim = new Animation("ionsfx");
			anim.PlayThen("idle", () => Finish(world));
		}

		public void Tick(World world) { anim.Tick(); }

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield return new SpriteRenderable(anim.Image,
				target.CenterLocation.ToFloat2() - new float2(0, 0.5f*anim.Image.size.Y - Game.CellSize),
				wr.Palette("effect"), (int)target.CenterLocation.Y);
		}

		void Finish( World world )
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(firedBy, "IonCannon", target.CenterLocation, 0);
		}
	}
}
