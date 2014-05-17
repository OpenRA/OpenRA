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
		readonly Target target;
		readonly Animation anim;
		readonly Player firedBy;
		readonly string palette;
		readonly string weapon;

		public IonCannon(Player firedBy, string weapon, World world, CPos location, string effect, string palette)
		{
			this.firedBy = firedBy;
			this.weapon = weapon;
			this.palette = palette;
			target = Target.FromCell(location);
			anim = new Animation(world, effect);
			anim.PlayThen("idle", () => Finish(world));
		}

		public void Tick(World world) { anim.Tick(); }

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(target.CenterPosition, wr.Palette(palette));
		}

		void Finish(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoExplosion(firedBy.PlayerActor, weapon, target.CenterPosition);
		}
	}
}
