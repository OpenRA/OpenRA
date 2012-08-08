#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	class Allies02ScriptInfo : TraitInfo<Allies02Script>, Requires<SpawnMapActorsInfo> { }

	class Allies02Script : IWorldLoaded, ITick
	{
		static readonly string[] objectives =
		{
			"Destroy the SAM sites. Tanya and Einstein must survive.",
			"Wait for the helicopter and extract Einstein. Tanya and Einstein must survive."
		};

		int currentObjective;

		Actor chinookHusk;
		Actor sam1;
		Actor sam2;
		Actor sam3;
		Actor sam4;
		Actor tanya;
		Actor einstein;

		Player allies1;
		Player allies2;
		Player soviets;

		void DisplayObjective()
		{
			Game.AddChatLine(Color.LimeGreen, "Objective", objectives[currentObjective]);
			Sound.Play("bleep6.aud", 5);
		}

		void MissionFailed(Actor self, string text)
		{
			if (allies1.WinState != WinState.Undefined)
			{
				return;
			}
			allies1.WinState = allies2.WinState = WinState.Lost;
			Game.AddChatLine(Color.Red, "Mission failed", text);
			Sound.Play("misnlst1.aud", 5);
		}

		void MissionAccomplished(Actor self, string text)
		{
			if (allies1.WinState != WinState.Undefined)
			{
				return;
			}
			allies1.WinState = allies2.WinState = WinState.Won;
			Game.AddChatLine(Color.Blue, "Mission accomplished", text);
			Sound.Play("misnwon1.aud", 5);
		}

		public void Tick(Actor self)
		{
			// display current objective every so often
			if (self.World.FrameNumber % 1500 == 1)
			{
				DisplayObjective();
			}
			if (currentObjective == 0)
			{
				if (sam1.Destroyed && sam2.Destroyed && sam3.Destroyed && sam4.Destroyed)
				{
					currentObjective++;
					DisplayObjective();
				}
			}
			else if (currentObjective == 1)
			{
				
			}
			if (tanya.Destroyed)
			{
				MissionFailed(self, "Tanya was killed.");
			}
			if (einstein.Destroyed)
			{
				MissionFailed(self, "Einstein was killed.");
			}
		}

		public void WorldLoaded(World w)
		{
			allies1 = w.Players.Single(p => p.InternalName == "Allies1");
			allies2 = w.Players.Single(p => p.InternalName == "Allies2");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");
			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			chinookHusk = actors["ChinookHusk"];
			sam1 = actors["SAM1"];
			sam2 = actors["SAM2"];
			sam3 = actors["SAM3"];
			sam4 = actors["SAM4"];
			tanya = actors["Tanya"];
			einstein = actors["Einstein"];
			w.WorldActor.Trait<Shroud>().Explore(w, sam1.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam2.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam3.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam4.Location, 2);
			Game.MoveViewport(chinookHusk.Location.ToFloat2());
		}
	}
}
