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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
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

		Actor chinookHusk;
		Actor allies2BasePoint;
		Actor reinforcementsEntryPoint;

		World world;

		Player allies1;
		Player allies2;
		Player soviets;

		void DisplayObjective()
		{
			Game.AddChatLine(Color.LimeGreen, "Objective", objectives[currentObjective]);
			Sound.Play("bleep6.aud");
		}

		void MissionFailed(Actor self, string text)
		{
			if (allies1.WinState != WinState.Undefined)
			{
				return;
			}
			allies1.WinState = allies2.WinState = WinState.Lost;
			Game.AddChatLine(Color.Red, "Mission failed", text);
			Sound.Play("misnlst1.aud");
		}

		void MissionAccomplished(Actor self, string text)
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
			// display current objective every so often
			if (self.World.FrameNumber % 3500 == 1)
			{
				DisplayObjective();
			}
			if (currentObjective == 0)
			{
				if (sam1.Destroyed && sam2.Destroyed && sam3.Destroyed && sam4.Destroyed)
				{
					currentObjective++;
					DisplayObjective();
					StartChinookTimer();
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

		CountdownTimerWidget reinforcementsTimer;
		const string ReinforcementsTimerHeader = "Reinforcements arrive in";
		const int ReinforcementsTimerTicks = 1500 * 12;
		static readonly float2 reinforcementsTimerPosition = new float2(128, 64);
		static readonly string[] reinforcements = { "1tnk", "1tnk", "jeep", "mcv" };

		CountdownTimerWidget chinookTimer;
		const string ChinookTimerHeader = "Extraction in";
		const int ChinookTimerTicks = 1500 * 6;
		static readonly float2 chinookTimerPosition = new float2(128, 96);

		void StartReinforcementsTimer()
		{
			reinforcementsTimer.IsVisible = () => true;
			Sound.Play("timergo1.aud");
		}

		void StartChinookTimer()
		{
			chinookTimer.IsVisible = () => true;
			Sound.Play("timergo1.aud");
		}

		void ReinforcementsTimerExpired()
		{
			reinforcementsTimer.IsVisible = () => false;
			SendReinforcements();
		}

		void ChinookTimerExpired()
		{
			chinookTimer.IsVisible = () => false;
			SendChinook();
		}

		void SendReinforcements()
		{
			Sound.Play("reinfor1.aud");
			for (int i = 0; i < reinforcements.Length; i++)
			{
				var actor = world.CreateActor(reinforcements[i], new TypeDictionary { new LocationInit(reinforcementsEntryPoint.Location + new CVec(i, 0)), new FacingInit(0), new OwnerInit(allies2) });
				actor.QueueActivity(new Move.Move(allies2BasePoint.Location));
			}
		}

		void SendChinook()
		{
			
		}

		public void WorldLoaded(World w)
		{
			world = w;
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
			allies2BasePoint = actors["Allies2BasePoint"];
			reinforcementsEntryPoint = actors["ReinforcementsEntryPoint"];
			w.WorldActor.Trait<Shroud>().Explore(w, sam1.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam2.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam3.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam4.Location, 2);
			reinforcementsTimer = new CountdownTimerWidget(ReinforcementsTimerHeader, ReinforcementsTimerTicks, reinforcementsTimerPosition)
			{
				IsVisible = () => false,
				OnExpired = ReinforcementsTimerExpired
			};
			Ui.Root.AddChild(reinforcementsTimer);
			chinookTimer = new CountdownTimerWidget(ChinookTimerHeader, ChinookTimerTicks, chinookTimerPosition)
			{
				IsVisible = () => false,
				OnExpired = ChinookTimerExpired
			};
			Ui.Root.AddChild(chinookTimer);
			Game.MoveViewport(((w.LocalPlayer ?? allies1) == allies1 ? chinookHusk.Location : allies2BasePoint.Location).ToFloat2());
			StartReinforcementsTimer();
		}
	}

	public class CountdownTimerWidget : Widget
	{
		public string Header { get; set; }
		public int TicksLeft { get; set; }
		public float2 Position { get; set; }

		public CountdownTimerWidget(string header, int ticksLeft, float2 position)
		{
			Header = header;
			TicksLeft = ticksLeft;
			Position = position;
			OnOneMinuteRemaining = () => Sound.Play("1minr.aud");
			OnTwoMinutesRemaining = () => Sound.Play("2minr.aud");
			OnThreeMinutesRemaining = () => Sound.Play("3minr.aud");
			OnFourMinutesRemaining = () => Sound.Play("4minr.aud");
			OnFiveMinutesRemaining = () => Sound.Play("5minr.aud");
			OnTenMinutesRemaining = () => Sound.Play("10minr.aud");
			OnTwentyMinutesRemaining = () => Sound.Play("20minr.aud");
			OnThirtyMinutesRemaining = () => Sound.Play("30minr.aud");
			OnFortyMinutesRemaining = () => Sound.Play("40minr.aud");
		}

		public Action OnExpired;
		public Action OnOneMinuteRemaining { get; set; }
		public Action OnTwoMinutesRemaining { get; set; }
		public Action OnThreeMinutesRemaining { get; set; }
		public Action OnFourMinutesRemaining { get; set; }
		public Action OnFiveMinutesRemaining { get; set; }
		public Action OnTenMinutesRemaining { get; set; }
		public Action OnTwentyMinutesRemaining { get; set; }
		public Action OnThirtyMinutesRemaining { get; set; }
		public Action OnFortyMinutesRemaining { get; set; }

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
					case 1500 * 00: OnExpired(); break;
					case 1500 * 01: OnOneMinuteRemaining(); break;
					case 1500 * 02: OnTwoMinutesRemaining(); break;
					case 1500 * 03: OnThreeMinutesRemaining(); break;
					case 1500 * 04: OnFourMinutesRemaining(); break;
					case 1500 * 05: OnFiveMinutesRemaining(); break;
					case 1500 * 10: OnTenMinutesRemaining(); break;
					case 1500 * 20: OnTwentyMinutesRemaining(); break;
					case 1500 * 30: OnThirtyMinutesRemaining(); break;
					case 1500 * 40: OnFortyMinutesRemaining(); break;
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
