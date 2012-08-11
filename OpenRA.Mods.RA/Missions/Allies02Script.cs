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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Traits;
using OpenRA.Widgets;

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

		Actor sam1;
		Actor sam2;
		Actor sam3;
		Actor sam4;
		Actor tanya;
		Actor einstein;
		Actor engineer;

		Actor engineerMiss;

		Actor chinookHusk;
		Actor allies2BasePoint;
		Actor reinforcementsEntryPoint;
		Actor extractionLZEntryPoint;
		Actor extractionLZ;

		Actor einsteinChinook;

		World world;

		Player allies1;
		Player allies2;
		Player soviets;

		static readonly string[] reinforcements = { "1tnk", "1tnk", "jeep", "mcv" };
		const string ChinookName = "tran";
		const string SignalFlareName = "flare";
		const string EngineerName = "e6";
		const int EngineerMissClearRange = 5;

		void DisplayObjective()
		{
			Game.AddChatLine(Color.LimeGreen, "Objective", objectives[currentObjective]);
			Sound.Play("bleep6.aud");
		}

		void MissionFailed(string text)
		{
			if (allies1.WinState != WinState.Undefined)
			{
				return;
			}
			allies1.WinState = allies2.WinState = WinState.Lost;
			Game.AddChatLine(Color.Red, "Mission failed", text);
			Sound.Play("misnlst1.aud");
		}

		void MissionAccomplished(string text)
		{
			if (allies1.WinState != WinState.Undefined)
			{
				return;
			}
			allies1.WinState = allies2.WinState = WinState.Won;
			Game.AddChatLine(Color.Blue, "Mission accomplished", text);
			Sound.Play("misnwon1.aud");
		}

		public void Tick(Actor self)
		{
			if (world.FrameNumber % 3500 == 1)
			{
				DisplayObjective();
			}
			if (world.FrameNumber == 200)
			{
				SendReinforcements();
			}
			if (!engineerMiss.Destroyed && engineer == null && AlliesControlMiss())
			{
				SpawnEngineerAtMiss();
				engineerMiss.QueueActivity(new Demolish(engineerMiss, 0));
			}
			if (currentObjective == 0)
			{
				if (sam1.Destroyed && sam2.Destroyed && sam3.Destroyed && sam4.Destroyed)
				{
					currentObjective++;
					DisplayObjective();
					SpawnSignalFlare();
					Sound.Play("flaren1.aud");
					StartChinookTimer();
				}
			}
			else if (currentObjective == 1)
			{
				if (einsteinChinook != null && !einsteinChinook.IsDead() && !world.Map.IsInMap(einsteinChinook.Location) && einsteinChinook.Trait<Cargo>().Passengers.Contains(einstein))
				{
					MissionAccomplished("Einstein was rescued.");
				}
			}
			if (tanya.Destroyed)
			{
				MissionFailed("Tanya was killed.");
			}
			if (einstein.Destroyed)
			{
				MissionFailed("Einstein was killed.");
			}
		}

		void InitializeSovietAI()
		{
			if (!Game.IsHost || world.LocalPlayer == null)
			{
				return;
			}
			var logic = world.LocalPlayer.PlayerActor.TraitsImplementing<IBot>().First(b => b.Info.Name == "Soviet AI");
			logic.Activate(soviets);
		}

		void SpawnSignalFlare()
		{
			world.CreateActor(SignalFlareName, new TypeDictionary { new OwnerInit(allies1), new LocationInit(extractionLZ.Location) });
		}

		void StartChinookTimer()
		{
			var timer = new CountdownTimerWidget("Extraction arrives in", 1500 * 6, ChinookTimerExpired, new float2(128, 96));
			Ui.Root.AddChild(timer);
		}

		void ChinookTimerExpired(CountdownTimerWidget timer)
		{
			Sound.Play("reinfor1.aud");
			timer.Visible = false;
			SendChinook();
		}

		void SendReinforcements()
		{
			Sound.Play("reinfor1.aud");
			foreach (var unit in reinforcements)
			{
				var actor = world.CreateActor(unit, new TypeDictionary
				{
					new LocationInit(reinforcementsEntryPoint.Location),
					new FacingInit(0),
					new OwnerInit(allies2)
				});
				actor.QueueActivity(new Move.Move(allies2BasePoint.Location));
			}
		}

		void SendChinook()
		{
			einsteinChinook = world.CreateActor(ChinookName, new TypeDictionary { new OwnerInit(allies1), new LocationInit(extractionLZEntryPoint.Location) });
			einsteinChinook.QueueActivity(new HeliFly(extractionLZ.CenterLocation));
			einsteinChinook.QueueActivity(new Turn(0));
			einsteinChinook.QueueActivity(new HeliLand(true));
			einsteinChinook.QueueActivity(new WaitFor(() => einsteinChinook.Trait<Cargo>().Passengers.Contains(einstein)));
			einsteinChinook.QueueActivity(new Wait(150));
			einsteinChinook.QueueActivity(new HeliFly(extractionLZEntryPoint.CenterLocation));
			einsteinChinook.QueueActivity(new RemoveSelf());
		}

		IEnumerable<Actor> UnitsNearActor(Actor actor, int range)
		{
			return world.FindUnitsInCircle(actor.CenterLocation, Game.CellSize * range)
				.Where(a => a.IsInWorld && a != world.WorldActor && !a.Destroyed && a.HasTrait<IMove>() && !a.Owner.NonCombatant);
		}

		bool AlliesControlMiss()
		{
			var units = UnitsNearActor(engineerMiss, EngineerMissClearRange);
			return units.Any() && units.All(a => a.Owner == allies1);
		}

		void SpawnEngineerAtMiss()
		{
			engineer = world.CreateActor(EngineerName, new TypeDictionary { new OwnerInit(allies1), new LocationInit(engineerMiss.Location) });
			engineer.QueueActivity(new Move.Move(engineerMiss.Location + new CVec(5, 0)));
		}

		public void WorldLoaded(World w)
		{
			world = w;
			allies1 = w.Players.Single(p => p.InternalName == "Allies1");
			allies2 = w.Players.Single(p => p.InternalName == "Allies2");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");
			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			sam1 = actors["SAM1"];
			sam2 = actors["SAM2"];
			sam3 = actors["SAM3"];
			sam4 = actors["SAM4"];
			tanya = actors["Tanya"];
			einstein = actors["Einstein"];
			chinookHusk = actors["ChinookHusk"];
			allies2BasePoint = actors["Allies2BasePoint"];
			reinforcementsEntryPoint = actors["ReinforcementsEntryPoint"];
			extractionLZ = actors["ExtractionLZ"];
			extractionLZEntryPoint = actors["ExtractionLZEntryPoint"];
			engineerMiss = actors["EngineerMiss"];
			w.WorldActor.Trait<Shroud>().Explore(w, sam1.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam2.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam3.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam4.Location, 2);
			Game.MoveViewport(((w.LocalPlayer ?? allies1) == allies1 ? chinookHusk.Location : allies2BasePoint.Location).ToFloat2());
			InitializeSovietAI();
		}
	}

	public class CountdownTimerWidget : Widget
	{
		public string Header { get; set; }
		public int TicksLeft { get; set; }
		public float2 Position { get; set; }

		public CountdownTimerWidget(string header, int ticksLeft, Action<CountdownTimerWidget> onExpired, float2 position)
		{
			Header = header;
			TicksLeft = ticksLeft;
			OnExpired = onExpired;
			Position = position;
			OnOneMinuteRemaining = t => Sound.Play("1minr.aud");
			OnTwoMinutesRemaining = t => Sound.Play("2minr.aud");
			OnThreeMinutesRemaining = t => Sound.Play("3minr.aud");
			OnFourMinutesRemaining = t => Sound.Play("4minr.aud");
			OnFiveMinutesRemaining = t => Sound.Play("5minr.aud");
			OnTenMinutesRemaining = t => Sound.Play("10minr.aud");
			OnTwentyMinutesRemaining = t => Sound.Play("20minr.aud");
			OnThirtyMinutesRemaining = t => Sound.Play("30minr.aud");
			OnFortyMinutesRemaining = t => Sound.Play("40minr.aud");
		}

		public Action<CountdownTimerWidget> OnExpired { get; set; }
		public Action<CountdownTimerWidget> OnOneMinuteRemaining { get; set; }
		public Action<CountdownTimerWidget> OnTwoMinutesRemaining { get; set; }
		public Action<CountdownTimerWidget> OnThreeMinutesRemaining { get; set; }
		public Action<CountdownTimerWidget> OnFourMinutesRemaining { get; set; }
		public Action<CountdownTimerWidget> OnFiveMinutesRemaining { get; set; }
		public Action<CountdownTimerWidget> OnTenMinutesRemaining { get; set; }
		public Action<CountdownTimerWidget> OnTwentyMinutesRemaining { get; set; }
		public Action<CountdownTimerWidget> OnThirtyMinutesRemaining { get; set; }
		public Action<CountdownTimerWidget> OnFortyMinutesRemaining { get; set; }

		public override void Tick()
		{
			if (!IsVisible())
			{
				return;
			}
			if (TicksLeft > 0)
			{
				TicksLeft--;
				switch (TicksLeft)
				{
					case 1500 * 00: OnExpired(this); break;
					case 1500 * 01: OnOneMinuteRemaining(this); break;
					case 1500 * 02: OnTwoMinutesRemaining(this); break;
					case 1500 * 03: OnThreeMinutesRemaining(this); break;
					case 1500 * 04: OnFourMinutesRemaining(this); break;
					case 1500 * 05: OnFiveMinutesRemaining(this); break;
					case 1500 * 10: OnTenMinutesRemaining(this); break;
					case 1500 * 20: OnTwentyMinutesRemaining(this); break;
					case 1500 * 30: OnThirtyMinutesRemaining(this); break;
					case 1500 * 40: OnFortyMinutesRemaining(this); break;
				}
			}
		}

		public override void Draw()
		{
			if (!IsVisible())
			{
				return;
			}
			var font = Game.Renderer.Fonts["Bold"];
			var text = "{0}: {1}".F(Header, WidgetUtils.FormatTime(TicksLeft));
			font.DrawTextWithContrast(text, Position, TicksLeft == 0 && Game.LocalTick % 60 >= 30 ? Color.Red : Color.White, Color.Black, 1);
		}
	}
}
