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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CncShellmapScriptInfo : TraitInfo<CncShellmapScript> { }

	class CncShellmapScript: IWorldLoaded, ITick
	{
		Dictionary<string, Actor> Actors;
		static int2 ViewportOrigin;

		public void WorldLoaded(World w)
		{
			var b = w.Map.Bounds;
			ViewportOrigin = new int2(b.Left + b.Width/2, b.Top + b.Height/2);
			Game.MoveViewport(ViewportOrigin);

			Actors = w.WorldActor.Trait<SpawnMapActors>().Actors;

			SetViewport();
		}

		void SetViewport()
		{
			var t = (ticks + 45) % (360f * speed) * (Math.PI / 180) * 1f / speed;
			var loc = ViewportOrigin + new float2(-15,4) * float2.FromAngle( (float)t );
			Game.viewport.Center(loc);
		}

		int ticks = 0;
		float speed = 4f;

		public void Tick(Actor self)
		{
			SetViewport();

			if (ticks == 0)
			{
				LoopTrack(Actors["boat1"], Actors["tl1"].Location, Actors["tr1"].Location);
				LoopTrack(Actors["boat3"], Actors["tl1"].Location, Actors["tr1"].Location);
				LoopTrack(Actors["boat2"], Actors["tl3"].Location, Actors["tr3"].Location);
				LoopTrack(Actors["boat4"], Actors["tl3"].Location, Actors["tr3"].Location);
				CreateUnitsInTransport(Actors["lst1"], new string[] {"htnk"});
				CreateUnitsInTransport(Actors["lst2"], new string[] {"mcv"});
				CreateUnitsInTransport(Actors["lst3"], new string[] {"htnk"});
				LoopTrack(Actors["lst1"], Actors["tl2"].Location, Actors["tr2"].Location);
				LoopTrack(Actors["lst2"], Actors["tl2"].Location, Actors["tr2"].Location);
				LoopTrack(Actors["lst3"], Actors["tl2"].Location, Actors["tr2"].Location);
			}

			ticks++;
		}

		void CreateUnitsInTransport(Actor transport, string[] cargo)
		{
			var f = transport.Trait<IFacing>();
			var c = transport.Trait<Cargo>();
			foreach (var i in cargo)
				c.Load(transport, transport.World.CreateActor(false, i.ToLowerInvariant(), new TypeDictionary
				{
					new OwnerInit( transport.Owner ),
					new FacingInit( f.Facing ),
				}));
		}

		void LoopTrack(Actor self, int2 left, int2 right)
		{
			var mobile = self.Trait<Mobile>();
			self.QueueActivity(mobile.ScriptedMove(left));
			self.QueueActivity(new Teleport(right));
			self.QueueActivity(new CallFunc(() => LoopTrack(self,left,right)));
		}
	}
}
