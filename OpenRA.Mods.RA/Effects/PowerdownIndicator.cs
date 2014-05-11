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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class PowerdownIndicator : IEffect
	{
		readonly Actor a;
		readonly Animation anim;

		public PowerdownIndicator(Actor a)
		{
			this.a = a;

			anim = new Animation(a.World, "poweroff");
			anim.PlayRepeating("offline");
		}

		public void Tick(World world)
		{
			if (!a.IsInWorld || a.IsDead() || !a.Trait<CanPowerDown>().Disabled)
				world.AddFrameEndTask(w => w.Remove(this));

			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (a.Destroyed || wr.world.FogObscures(a))
				return SpriteRenderable.None;

			return anim.Render(a.CenterPosition, wr.Palette("chrome"));
		}
	}
}
