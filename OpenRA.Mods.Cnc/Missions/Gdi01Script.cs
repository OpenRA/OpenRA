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
using OpenRA.Widgets;
using OpenRA.Traits.Activities;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	class Gdi01ScriptInfo : TraitInfo<Gdi01Script> { }

	class Gdi01Script: ILoadWorldHook, ITick
	{		
		Dictionary<string, Actor> Actors;
		Dictionary<string, Player> Players;
		
		public void WorldLoaded(World w)
		{
			Players = w.WorldActor.Trait<CreateMapPlayers>().Players;
			Actors = w.WorldActor.Trait<SpawnMapActors>().Actors;		
			Game.MoveViewport((.5f * (w.Map.TopLeft + w.Map.BottomRight).ToFloat2()).ToInt2());
			
			var playerRoot = Widget.RootWidget.OpenWindow("FMVPLAYER");
			var player = playerRoot.GetWidget<VqaPlayerWidget>("PLAYER");
			w.DisableTick = true;
			
			player.Load("gdi1.vqa");
			player.PlayThen(() =>
			{
				player.Load("landing.vqa");
				player.PlayThen(() =>
				{
					Widget.RootWidget.CloseWindow();
					w.DisableTick = false;
					Sound.PlayMusic(Rules.Music["aoi"].Filename);
					started = true;
				});
			});
		}
		
		int ticks = 0;
		bool started = false;
		public void Tick(Actor self)
		{
			if (!started)
				return;
			
			if (ticks == 0)
			{
				Actors["gunboat"].QueueActivity(new Move(new int2(50,59),1));
			}
			ticks++;
			
			if (ticks == 25*5)
			{
				Sound.PlayToPlayer(self.World.LocalPlayer,"reinfor1.aud");
				
				// Pathfinder does stupid crap, so hardcode the path we want
				var path = new List<int2>()
				{
					new int2(53,61),
					new int2(53,60),
					new int2(53,59),
					new int2(53,58),
					new int2(53,57),
				};
				
				DoReinforcements(self.World, new int2(54,61),new int2(54,57), new int2(53,53), new string[] {"e1","e1","e1"});
			}
		}
		
		void DoReinforcements(World world, int2 startPos, int2 endPos, int2 unload, string[] items)
		{
			world.AddFrameEndTask(w =>
			{
				var a = w.CreateActor("lst", new TypeDictionary 
				{
					new LocationInit( startPos ),
					new OwnerInit( Players["GoodGuy"] ),
					new FacingInit( 0 ),
				});
				
				var cargo = a.Trait<Cargo>();
				foreach (var i in items)
					cargo.Load(a, world.CreateActor(false, i.ToLowerInvariant(), new TypeDictionary { new OwnerInit( Players["GoodGuy"] ) }));
				
				a.CancelActivity();
				a.QueueActivity(new Move(endPos, 0));
				a.QueueActivity(new CallFunc(() =>
				{
					while (!cargo.IsEmpty(a))
					{
						var b = cargo.Unload(a);
						world.AddFrameEndTask(w2 =>
						{
							w2.Add(b);
							b.TraitsImplementing<IMove>().FirstOrDefault().SetPosition(b, a.Location);
							b.QueueActivity(new Move(unload, 2));
						});
					}
				}));
				a.QueueActivity(new Wait(25));
				a.QueueActivity(new Move(startPos,0));
				a.QueueActivity(new RemoveSelf());
			});
		}
	}
}
