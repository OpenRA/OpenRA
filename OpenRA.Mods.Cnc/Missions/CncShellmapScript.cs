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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CncShellmapScriptInfo : TraitInfo<CncShellmapScript> { }

	class CncShellmapScript : IWorldLoaded, ITick
	{
		static CPos viewportOrigin;
		Dictionary<string, Actor> actors;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var b = w.Map.Bounds;
			viewportOrigin = new CPos(b.Left + b.Width / 2, b.Top + b.Height / 2);
			Game.MoveViewport(viewportOrigin.ToFloat2());

			actors = w.WorldActor.Trait<SpawnMapActors>().Actors;

			SetViewport();
		}

		void SetViewport()
		{
			var t = (ticks + 45) % (360f * speed) * (Math.PI / 180) * 1f / speed;
			var loc = viewportOrigin.ToFloat2() + (new float2(-15, 4) * float2.FromAngle((float)t));
			Game.viewport.Center(loc);
		}

		int ticks = 0;
		float speed = 4f;

		public void Tick(Actor self)
		{
			SetViewport();

			if (ticks == 0)
			{
				LoopTrack(actors["boat1"], actors["tl1"].Location, actors["tr1"].Location);
				LoopTrack(actors["boat3"], actors["tl1"].Location, actors["tr1"].Location);
				LoopTrack(actors["boat2"], actors["tl3"].Location, actors["tr3"].Location);
				LoopTrack(actors["boat4"], actors["tl3"].Location, actors["tr3"].Location);
				CreateUnitsInTransport(actors["lst1"], new string[] { "htnk" });
				CreateUnitsInTransport(actors["lst2"], new string[] { "mcv" });
				CreateUnitsInTransport(actors["lst3"], new string[] { "htnk" });
				LoopTrack(actors["lst1"], actors["tl2"].Location, actors["tr2"].Location);
				LoopTrack(actors["lst2"], actors["tl2"].Location, actors["tr2"].Location);
				LoopTrack(actors["lst3"], actors["tl2"].Location, actors["tr2"].Location);
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
					new OwnerInit(transport.Owner),
					new FacingInit(f.Facing),
				}));
		}

		void LoopTrack(Actor self, CPos left, CPos right)
		{
			var mobile = self.Trait<Mobile>();
			self.QueueActivity(mobile.ScriptedMove(left));
			self.QueueActivity(new SimpleTeleport(right));
			self.QueueActivity(new CallFunc(() => LoopTrack(self, left, right)));
		}
	}
}
