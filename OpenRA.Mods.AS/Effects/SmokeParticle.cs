#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Effects
{
	class SmokeParticle : IEffect
	{
		readonly Actor invoker;
		readonly World world;
		readonly ISmokeParticleInfo smoke;
		readonly Animation anim;
		readonly WVec[] gravity;
		readonly bool visibleThroughFog;
		readonly bool scaleSizeWithZoom;
		readonly bool canDamage;

		WPos pos;
		int lifetime;
		int explosionInterval;

		public SmokeParticle(Actor invoker, ISmokeParticleInfo smoke, WPos pos, bool visibleThroughFog = false, bool scaleSizeWithZoom = false)
		{
			this.invoker = invoker;
			world = invoker.World;
			this.pos = pos;
			this.smoke = smoke;
			gravity = smoke.Gravity;
			this.scaleSizeWithZoom = scaleSizeWithZoom;
			this.visibleThroughFog = visibleThroughFog;
			anim = new Animation(world, smoke.Image, () => 0);
			anim.PlayRepeating(smoke.Sequence);
			world.ScreenMap.Add(this, pos, anim.Image);
			lifetime = smoke.Duration.Length == 2
				? world.SharedRandom.Next(smoke.Duration[0], smoke.Duration[1])
				: smoke.Duration[0];

			canDamage = smoke.Weapon != null;
		}

		public void Tick(World world)
		{
			if (--lifetime < 0)
			{
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });
				return;
			}

			anim.Tick();

			var offset = gravity.Length == 2
				? new WVec(world.SharedRandom.Next(gravity[0].X, gravity[1].X), world.SharedRandom.Next(gravity[0].Y, gravity[1].Y),
					world.SharedRandom.Next(gravity[0].Z, gravity[1].Z))
				: gravity[0];

			pos += offset;

			world.ScreenMap.Update(this, pos, anim.Image);

			if (canDamage && --explosionInterval < 0)
			{
				smoke.Weapon.Impact(Target.FromPos(pos), invoker, new int[0]);
				explosionInterval = smoke.Weapon.ReloadDelay;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (world.FogObscures(pos) && !visibleThroughFog)
				return SpriteRenderable.None;

			var zoom = scaleSizeWithZoom ? 1f / wr.Viewport.Zoom : 1f;
			return anim.Render(pos, WVec.Zero, 0, wr.Palette(smoke.Palette), zoom);
		}
	}
}
