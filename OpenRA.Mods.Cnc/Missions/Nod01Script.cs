#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.Cnc;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Activities;
using OpenRA.Network;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Missions
{
    class Nod01ScriptInfo : TraitInfo<Nod01Script>, Requires<SpawnMapActorsInfo> { }

    class Nod01Script : IWorldLoaded, ITick
    {
        static readonly string[] Objectives =
		{
			"Assasinate Nikoomba.",
            "Level the village."
		};

        int currentObjective;

        Player gdi;
        Player nod;
        //actors and the likes go here
        Actor nikoomba;
        Actor vil01;
        Actor vil02;
        Actor vil03;
        Actor vil04;
        Actor vil05;
        Actor vil06;
        Actor vil07;
        Actor vil08;
        Actor vil09;
        Actor vil10;
        Actor vil11;
        Actor vil12;
        Actor vil13;
        Actor civ01;
        Actor civ02;
        Actor civ03;
        Actor civ04;
        Actor civ05;
        Actor civ06;
        Actor civ07;
        //waypoints
        Actor nr1;
        Actor nr2;
        Actor gr1;

        World world;

        //in the allies01 script stuff was here not needed for me so far
        const string NRName = "E1";
        const string GRName = "E2";
        const string GRName2 = "JEEP";

        void DisplayObjective()
        {
            Game.AddChatLine(Color.LimeGreen, "Objective", Objectives[currentObjective]);
        }

        void MissionFailed(string text)
        {
            if (nod.WinState != WinState.Undefined)
            {
                return;
            }
            nod.WinState = WinState.Lost;
            foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == nod && !a.IsDead()))
            {
                actor.Kill(actor);
            }
            Game.AddChatLine(Color.Red, "Mission failed", text);
            Sound.Play("fail1.aud");
        }

        void MissionAccomplished(string text)
        {
            if (nod.WinState != WinState.Undefined)
            {
                return;
            }
            nod.WinState = WinState.Won;
            Game.AddChatLine(Color.Green, "Mission accomplished", text);
            Sound.Play("accom1.aud");
        }

        public void Tick(Actor self)
        {
            if (nod.WinState != WinState.Undefined)
            {
                return;
            }
            // display current objective every so often
            if (world.FrameNumber % 1500 == 1)
            {
                DisplayObjective();
            }
            //spawns nod reinf
            if (world.FrameNumber == 700)
            {
                NODReinforceNthA();
                Sound.Play("reinfor1.aud");
            }
            if (world.FrameNumber == 1400)
            {
                NODReinforceNthB();
                Sound.Play("reinfor1.aud");
            }
            // objectives
            if (currentObjective == 0)
            {
                if (nikoomba.Destroyed)
                {
                    currentObjective++;
                    DisplayObjective();
                    GDIReinforceNth();
                }
            }
            else if (currentObjective == 1)
            {
                if (vil01.Destroyed && vil02.Destroyed && vil03.Destroyed && vil04.Destroyed && vil05.Destroyed && vil06.Destroyed && 
                    vil07.Destroyed && vil08.Destroyed && vil09.Destroyed && vil10.Destroyed && vil11.Destroyed && vil12.Destroyed && 
                    vil13.Destroyed && civ01.Destroyed && civ02.Destroyed && civ03.Destroyed && civ04.Destroyed && civ05.Destroyed && 
                    civ06.Destroyed && civ07.Destroyed)
                {
                    MissionAccomplished("Nikoomba was killed and the Village was Destroyed.");
                }
            }
        }

        IEnumerable<Actor> UnitsNearActor(Actor actor, int range)
        {
            return world.FindUnitsInCircle(actor.CenterLocation, Game.CellSize * range)
                .Where(a => a.IsInWorld && a != world.WorldActor && !a.Destroyed && a.HasTrait<IMove>() && !a.Owner.NonCombatant);
        }

        void NODReinforceNthA()
        {
            nr1 = world.CreateActor(true, NRName, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr1.Location) });
            nr1 = world.CreateActor(true, NRName, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr1.Location) });
            //nr1.QueueActivity(new Move.Move(nr1.Location - new CVec(0, 2)));
        }

        void NODReinforceNthB()
        {
            nr2 = world.CreateActor(true, NRName, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr2.Location) });
            nr2 = world.CreateActor(true, NRName, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr2.Location) });
            //nr1.QueueActivity(new Move.Move(nr1.Location - new CVec(0, 2)));
        }

        void GDIReinforceNth()
        {
            gr1 = world.CreateActor(true, GRName, new TypeDictionary { new OwnerInit(gdi), new LocationInit(gr1.Location) });
            gr1 = world.CreateActor(true, GRName, new TypeDictionary { new OwnerInit(gdi), new LocationInit(gr1.Location) });
            gr1 = world.CreateActor(true, GRName2, new TypeDictionary { new OwnerInit(gdi), new LocationInit(gr1.Location) });
            //nr1.QueueActivity(new Move.Move(nr1.Location - new CVec(0, 2)));
        }

        public void WorldLoaded(World w)
        {
            world = w;
            gdi = w.Players.Single(p => p.InternalName == "GDI");
            nod = w.Players.Single(p => p.InternalName == "NOD");
            var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
            nikoomba = actors["Nikoomba"];
            vil01 = actors["Vil01"];
            vil02 = actors["Vil02"];
            vil03 = actors["Vil03"];
            vil04 = actors["Vil04"];
            vil05 = actors["Vil05"];
            vil06 = actors["Vil06"];
            vil07 = actors["Vil07"];
            vil08 = actors["Vil08"];
            vil09 = actors["Vil09"];
            vil10 = actors["Vil10"];
            vil11 = actors["Vil11"];
            vil12 = actors["Vil12"];
            vil13 = actors["Vil13"];
            civ01 = actors["Civ01"];
            civ02 = actors["Civ02"];
            civ03 = actors["Civ03"];
            civ04 = actors["Civ04"];
            civ05 = actors["Civ05"];
            civ06 = actors["Civ06"];
            civ07 = actors["Civ07"];
            nr1 = actors["NODReinforceNthA"];
            nr2 = actors["NODReinforceNthB"];
            gr1 = actors["GDIReinforceNth"];
            Game.MoveViewport(nr1.Location.ToFloat2());
			Action afterFMV = () =>
			{
				Sound.PlayMusic(Rules.Music["aoi"]);
			};
			Game.RunAfterDelay(0, () => Media.PlayFMVFullscreen(w, "nod1pre.vqa", () =>
			                            Media.PlayFMVFullscreen(w, "nod1.vqa", afterFMV)));
        }
    }
}
