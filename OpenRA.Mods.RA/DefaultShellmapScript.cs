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
using System.Linq;
using OpenRA.Traits;
using System;
using OpenRA.Mods.RA.Air;

namespace OpenRA.Mods.RA
{
	class DefaultShellmapScriptInfo : TraitInfo<DefaultShellmapScript> { }

	class DefaultShellmapScript: IWorldLoaded, ITick
	{		
		Dictionary<string, Actor> Actors;
		private static int2 ViewportOrigin;
		
		public void WorldLoaded(World w)
		{
			int2 loc = (.5f * (w.Map.TopLeft + w.Map.BottomRight).ToFloat2()).ToInt2();
			Game.MoveViewport(loc);

			ViewportOrigin = loc;

			Actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			Sound.SoundVolumeModifier = 0f;
		}
		
		int ticks = 0;
		float speed = 4f;
		public void Tick(Actor self)
		{
			var loc = new float2(
				(float)(System.Math.Sin((ticks + 45) % (360f * speed) * (Math.PI / 180) * 1f / speed) * 15f + ViewportOrigin.X),
				(float)(System.Math.Cos((ticks + 45) % (360f * speed) * (Math.PI / 180) * 1f / speed) * 10f + ViewportOrigin.Y));

			Game.MoveViewport(loc);
			
			if (ticks == 250)
			{
				Actors["pdox"].Trait<Chronosphere>().Teleport(Actors["ca1"], new int2(90, 70));
				Actors["pdox"].Trait<Chronosphere>().Teleport(Actors["ca2"], new int2(92, 71));
			}
			if (ticks == 100)
				Actors["mslo1"].Trait<NukeSilo>().Attack(new int2(98, 52));
			if (ticks == 140)
				Actors["mslo2"].Trait<NukeSilo>().Attack(new int2(95, 54));
			if (ticks == 180)
				Actors["mslo3"].Trait<NukeSilo>().Attack(new int2(95, 49));

			if (ticks == 430)
			{
				Actors["mig1"].Trait<AttackPlane>().AttackTarget(Actors["mig1"], Actors["greeceweap"], true);
				Actors["mig2"].Trait<AttackPlane>().AttackTarget(Actors["mig2"], Actors["greeceweap"], true);
				Actors["mig3"].Trait<AttackPlane>().AttackTarget(Actors["mig3"], Actors["greeceweap"], true);
			}
			
			ticks++;
		}
	}
	

}
