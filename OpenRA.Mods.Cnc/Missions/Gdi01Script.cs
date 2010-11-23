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
using System.Linq;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc
{
	class Gdi01ScriptInfo : TraitInfo<Gdi01Script>, ITraitPrerequisite<OpenWidgetAtGameStartInfo> { }

	class Gdi01Script: IWorldLoaded, ITick
	{		
		Dictionary<string, Actor> Actors;
		Dictionary<string, Player> Players;
		Map Map;
				
		public void WorldLoaded(World w)
		{
			Map = w.Map;
			Players = w.players.Values.ToDictionary(p => p.InternalName);
			Actors = w.WorldActor.Trait<SpawnMapActors>().Actors;		
			var b = w.Map.Bounds;
			Game.MoveViewport(new int2(b.Left + b.Width/2, b.Top + b.Height/2));
			
			Scripting.Media.PlayFMVFullscreen(w, "gdi1.vqa", 
			    () => Scripting.Media.PlayFMVFullscreen(w, "landing.vqa", () =>
				{
					Sound.PlayMusic(Rules.Music["aoi"].Filename);
					started = true;
				}));
		}
		
		public void OnVictory(World w)
		{
			started = false;
			Sound.PlayToPlayer(Players["GoodGuy"], "accom1.aud");
			Players["GoodGuy"].WinState = WinState.Won;
			
			w.WorldActor.CancelActivity();
			w.WorldActor.QueueActivity(new Wait(125));
			w.WorldActor.QueueActivity(new CallFunc(() => Scripting.Media.PlayFMVFullscreen(w, "consyard.vqa", () =>
			{
				Sound.StopMusic();
				Game.Disconnect();
			})));
		}
		
		public void OnLose(World w)
		{
			started = false;
			Sound.PlayToPlayer(Players["GoodGuy"], "fail1.aud");
			Players["GoodGuy"].WinState = WinState.Lost;
			
			w.WorldActor.CancelActivity();
			w.WorldActor.QueueActivity(new Wait(125));
			w.WorldActor.QueueActivity(new CallFunc(() => Scripting.Media.PlayFMVFullscreen(w, "gameover.vqa", () =>
			{
				Sound.StopMusic();
				Game.Disconnect();
			})));
		}
		
		int ticks = 0;
		bool started = false;
		
		int lastBadCount = -1;
		public void Tick(Actor self)
		{
			if (!started)
				return;
			
			if (ticks == 0)
			{
				SetGunboatPath();
				self.World.AddFrameEndTask(w =>
				{
					//Initial Nod reinforcements
					foreach (var i in new[]{ "e1", "e1" })
					{
						var a = self.World.CreateActor(i.ToLowerInvariant(), new TypeDictionary
						{
							new OwnerInit( Players["BadGuy"] ),
							new FacingInit( 0 ),
							new LocationInit ( Map.Waypoints["nod0"] ),
						});
						var mobile = a.Trait<Mobile>();
						a.QueueActivity( mobile.MoveTo( Map.Waypoints["nod1"], 2 ) );
						a.QueueActivity( mobile.MoveTo( Map.Waypoints["nod2"], 2 ) );
						a.QueueActivity( mobile.MoveTo( Map.Waypoints["nod3"], 2 ) );
						// Todo: Queue hunt order
					}
				});
			}
			// GoodGuy win conditions
			// BadGuy is dead
			int badcount = self.World.Queries.OwnedBy[Players["BadGuy"]].Count(a => !a.IsDead());
			if (badcount != lastBadCount)
			{
				Game.Debug("{0} badguys remain".F(badcount));
				lastBadCount = badcount;
				
				if (badcount == 0)
					OnVictory(self.World);
			}
			
			//GoodGuy lose conditions
			if (self.World.Queries.OwnedBy[Players["GoodGuy"]].Count( a => !a.IsDead()) == 0)
				OnLose(self.World);
			
			// GoodGuy reinforcements
			if (ticks == 25*5)
			{
				ReinforceFromSea(self.World, 
				                 Map.Waypoints["lstStart"],
				                 Map.Waypoints["lstEnd"],
				                 new int2(53,53),
				                 new string[] {"e1","e1","e1"});
			}
			
			if (ticks == 25*15)
			{
				ReinforceFromSea(self.World, 
				                 Map.Waypoints["lstStart"],
				                 Map.Waypoints["lstEnd"],
				                 new int2(53,53),
				                 new string[] {"e1","e1","e1"});
			}
			
			if (ticks == 25*30)
			{
				ReinforceFromSea(self.World, 
				                 Map.Waypoints["lstStart"],
				                 Map.Waypoints["lstEnd"],
				                 new int2(53,53),
				                 new string[] {"jeep"});
			}
			
			if (ticks == 25*60)
			{
				ReinforceFromSea(self.World, 
				                 Map.Waypoints["lstStart"],
				                 Map.Waypoints["lstEnd"],
				                 new int2(53,53),
				                 new string[] {"jeep"});
			}
			
			ticks++;
		}
		
		void SetGunboatPath()
		{
			var self = Actors[ "Gunboat" ];
			var mobile = self.Trait<Mobile>();
			self.QueueActivity(mobile.ScriptedMove( Map.Waypoints["gunboatLeft"] ));
			self.QueueActivity(mobile.ScriptedMove( Map.Waypoints["gunboatRight"] ));
			self.QueueActivity(new CallFunc(() => SetGunboatPath()));
		}
		
		void ReinforceFromSea(World world, int2 startPos, int2 endPos, int2 unload, string[] items)
		{
			world.AddFrameEndTask(w =>
			{
				Sound.PlayToPlayer(w.LocalPlayer,"reinfor1.aud");				

				var a = w.CreateActor("lst", new TypeDictionary 
				{
					new LocationInit( startPos ),
					new OwnerInit( Players["GoodGuy"] ),
					new FacingInit( 0 ),
				});

				var mobile = a.Trait<Mobile>();
				var cargo = a.Trait<Cargo>();
				foreach (var i in items)
					cargo.Load(a, world.CreateActor(false, i.ToLowerInvariant(), new TypeDictionary
					{
						new OwnerInit( Players["GoodGuy"] ),
						new FacingInit( 0 ),
					}));
				
				a.CancelActivity();
				a.QueueActivity(mobile.ScriptedMove(endPos));
				a.QueueActivity(new CallFunc(() =>
				{
					while (!cargo.IsEmpty(a))
					{
						var b = cargo.Unload(a);
						world.AddFrameEndTask(w2 =>
						{
							if (b.Destroyed) return;
							w2.Add(b);
							b.TraitsImplementing<IMove>().FirstOrDefault().SetPosition(b, a.Location);
							b.QueueActivity(mobile.MoveTo(unload, 2));
						});
					}
				}));
				a.QueueActivity(new Wait(25));
				a.QueueActivity(mobile.ScriptedMove(startPos));
				a.QueueActivity(new RemoveSelf());
			});
		}
	}
}
