#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.RA.Air;
using OpenRA.Traits;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA
{
	class CncShellmapScriptInfo : TraitInfo<CncShellmapScript> { }

	class CncShellmapScript: IWorldLoaded, ITick
	{		
		Dictionary<string, Actor> Actors;
		static int2 ViewportOrigin;
		Map Map;

		public void WorldLoaded(World w)
		{
			Map = w.Map;
			var b = w.Map.Bounds;
			ViewportOrigin = new int2(b.Left + b.Width/2, b.Top + b.Height/2);
			Game.MoveViewport(ViewportOrigin);

			Actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			Sound.SoundVolumeModifier = 0.25f;
		}
		
		int ticks = 0;
		float speed = 4f;
		public void Tick(Actor self)
		{
			var loc = new float2(
				(float)(-System.Math.Sin((ticks + 45) % (360f * speed) * (Math.PI / 180) * 1f / speed) * 15f + ViewportOrigin.X),
				(float)(0.4f*System.Math.Cos((ticks + 45) % (360f * speed) * (Math.PI / 180) * 1f / speed) * 10f + ViewportOrigin.Y));
			Game.MoveViewport(loc);
			
			if (ticks == 0)
			{
				var w = Map.Waypoints;
				LoopTrack(Actors["boat1"], w["tl1"], w["tr1"]);
				LoopTrack(Actors["boat3"], w["tl1"], w["tr1"]);
				LoopTrack(Actors["boat2"], w["tl3"], w["tr3"]);
				LoopTrack(Actors["boat4"], w["tl3"], w["tr3"]);
				CreateUnitsInTransport(Actors["lst1"], new string[] {"htnk"});
				CreateUnitsInTransport(Actors["lst2"], new string[] {"mcv"});
				CreateUnitsInTransport(Actors["lst3"], new string[] {"htnk"});
				LoopTrack(Actors["lst1"], w["tl2"], w["tr2"]);
				LoopTrack(Actors["lst2"], w["tl2"], w["tr2"]);
				LoopTrack(Actors["lst3"], w["tl2"], w["tr2"]);
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
