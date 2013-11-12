#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Missions;
using OpenRA.Mods.RA.Move;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Missions
{
	class Nod01ScriptInfo : TraitInfo<Nod01Script>, Requires<SpawnMapActorsInfo> { }

	class Nod01Script : IHasObjectives, IWorldLoaded, ITick
	{
		public event Action<bool> OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return new[] { killnikoomba, levelvillage }; } }

		Objective killnikoomba = new Objective(ObjectiveType.Primary, KillNikoombaText, ObjectiveStatus.InProgress);
		Objective levelvillage = new Objective(ObjectiveType.Primary, LevelVillageText, ObjectiveStatus.Inactive);

		const string KillNikoombaText = "Find Nikoomba. Once found he must be assasinated.";
		const string LevelVillageText = "Nikoomba has met his demise, now level the village.";

		Player nod;

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

		Actor nr1;
		Actor nr2;
		Actor nr3;

		World world;

		const string ReInfE1 = "E1";
		const string ReInfE3 = "E3";

		void MissionFailed(string text)
		{
			MissionUtils.CoopMissionFailed(world, text, nod);
		}

		void MissionAccomplished(string text)
		{
			MissionUtils.CoopMissionAccomplished(world, text, nod);
		}

		public void Tick(Actor self)
		{
			if (nod.WinState != WinState.Undefined) return;

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

			if (killnikoomba.Status == ObjectiveStatus.InProgress)
			{
				if (nikoomba.Destroyed)
				{
					killnikoomba.Status = ObjectiveStatus.Completed;
					levelvillage.Status = ObjectiveStatus.InProgress;
					OnObjectivesUpdated(true);
					NODReinforceNWstA();
					Sound.Play("reinfor1.aud");
				}
			}

			if (levelvillage.Status == ObjectiveStatus.InProgress)
			{
				if (vil01.Destroyed && vil02.Destroyed && vil03.Destroyed && vil04.Destroyed && vil05.Destroyed && vil06.Destroyed &&
					vil07.Destroyed && vil08.Destroyed && vil09.Destroyed && vil10.Destroyed && vil11.Destroyed && vil12.Destroyed &&
					vil13.Destroyed && civ01.Destroyed && civ02.Destroyed && civ03.Destroyed && civ04.Destroyed && civ05.Destroyed &&
					civ06.Destroyed && civ07.Destroyed)
				{
					levelvillage.Status = ObjectiveStatus.Completed;
					OnObjectivesUpdated(true);
					Sound.StopMusic();
					MissionAccomplished("Nikoomba was killed and the village was destroyed.");
				}
			}

			var unitsAndBuildings = world.Actors.Where(a => !a.IsDead() && a.IsInWorld && (a.HasTrait<Mobile>() || (a.HasTrait<Building>() && !a.HasTrait<Wall>())));

			if (!unitsAndBuildings.Any(a => a.Owner == nod))
			{
				Action afterFMV = () =>
				{
					Sound.StopMusic();
					MissionFailed("The Nod forces in the area have been wiped out.");
				};
				Game.RunAfterDelay(0, () => Media.PlayFMVFullscreen(world, "nodlose.vqa", afterFMV));
			}
		}

		void NODReinforceNthA()
		{
			nr1 = world.CreateActor(true, ReInfE1, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr1.Location) });
			nr1.QueueActivity(nr1.Trait<Mobile>().ScriptedMove(nr1.Location - new CVec(0, -2)));
			nr1 = world.CreateActor(true, ReInfE1, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr1.Location) });
			nr1.QueueActivity(nr1.Trait<Mobile>().ScriptedMove(nr1.Location - new CVec(0, -2)));
		}

		void NODReinforceNthB()
		{
			nr2 = world.CreateActor(true, ReInfE1, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr2.Location) });
			nr2.QueueActivity(nr2.Trait<Mobile>().ScriptedMove(nr2.Location - new CVec(0, -2)));
			nr2 = world.CreateActor(true, ReInfE1, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr2.Location) });
			nr2.QueueActivity(nr2.Trait<Mobile>().ScriptedMove(nr2.Location - new CVec(0, -2)));
		}

		void NODReinforceNWstA()
		{
			nr3 = world.CreateActor(true, ReInfE3, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr3.Location) });
			nr3.QueueActivity(nr3.Trait<Mobile>().ScriptedMove(nr3.Location - new CVec(0, -5)));
			nr3 = world.CreateActor(true, ReInfE3, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr3.Location) });
			nr3.QueueActivity(nr3.Trait<Mobile>().ScriptedMove(nr3.Location - new CVec(0, -5)));
			nr3 = world.CreateActor(true, ReInfE3, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr3.Location) });
			nr3.QueueActivity(nr3.Trait<Mobile>().ScriptedMove(nr3.Location - new CVec(0, -5)));
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			nod = w.Players.Single(p => p.InternalName == "NOD");
			nod.PlayerActor.Trait<PlayerResources>().Cash = 0;
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
			nr3 = actors["NODReinforceNWstA"];

			wr.Viewport.Center(nr1.Location.CenterPosition);
			Game.RunAfterDelay(0, () => Media.PlayFMVFullscreen(w, "nod1pre.vqa", () =>
										Media.PlayFMVFullscreen(w, "nod1.vqa", MissionUtils.PlayMissionMusic)));
		}
	}
}
