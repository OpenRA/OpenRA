#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Air;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class DefaultShellmapScriptInfo : TraitInfo<DefaultShellmapScript> { }

	class DefaultShellmapScript: IWorldLoaded, ITick
	{
		Dictionary<string, Actor> Actors;
		static WPos ViewportOrigin;
		WorldRenderer worldRenderer;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			worldRenderer = wr;
			var b = w.Map.Bounds;
			ViewportOrigin = new CPos(b.Left + b.Width/2, b.Top + b.Height/2).CenterPosition;
			worldRenderer.Viewport.Center(ViewportOrigin);

			Actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			Sound.SoundVolumeModifier = 0.25f;
		}

		int ticks = 0;
		float speed = 4f;
		public void Tick(Actor self)
		{
			var t = (ticks + 45) % (360f * speed) * (Math.PI / 180) * 1f / speed;
			var offset = new float2(15360, 10240) * float2.FromAngle((float)t);
			worldRenderer.Viewport.Center(ViewportOrigin + new WVec((int)offset.X, (int)offset.Y, 0));

			if (ticks == 50)
			{
				Scripting.RASpecialPowers.Chronoshift(self.World, new List<Pair<Actor, CPos>>()
				{
					Pair.New(Actors["ca1"], new CPos(96, 70)),
					Pair.New(Actors["ca2"], new CPos(98, 72))
				}, Actors["pdox"], -1, false);
			}

			if (ticks == 100)
				Actors["mslo1"].Trait<NukePower>().Activate(Actors["mslo1"], new Order() { TargetLocation = new CPos(98, 52) });
			if (ticks == 140)
				Actors["mslo2"].Trait<NukePower>().Activate(Actors["mslo2"], new Order() { TargetLocation = new CPos(95, 54) });
			if (ticks == 180)
				Actors["mslo3"].Trait<NukePower>().Activate(Actors["mslo3"], new Order() { TargetLocation = new CPos(95, 49) });

			if (ticks == 430)
			{
				Actors["mig1"].Trait<AttackPlane>().AttackTarget(Target.FromActor(Actors["greeceweap"]), false, true);
				Actors["mig2"].Trait<AttackPlane>().AttackTarget(Target.FromActor(Actors["greeceweap"]), false, true);
			}

			ticks++;
		}
	}
}
