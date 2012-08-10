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

		Actor chinookHusk;
		Actor sam1;
		Actor sam2;
		Actor sam3;
		Actor sam4;
		Actor tanya;
		Actor einstein;

		Actor allies2BasePoint;

		Player allies1;
		Player allies2;
		Player soviets;

		World world;

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
			w.WorldActor.Trait<Shroud>().Explore(w, sam1.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam2.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam3.Location, 2);
			w.WorldActor.Trait<Shroud>().Explore(w, sam4.Location, 2);
			if (w.LocalPlayer != null)
			{
				Game.MoveViewport((w.LocalPlayer == allies1 ? chinookHusk.Location : allies2BasePoint.Location).ToFloat2());
			}
			var widget = new CountdownTimerWidget("Reinforcements arrive in", 3200, Ui.Root.RenderBounds);
			widget.OnExpired += EvaTimerExpired;
			widget.OnOneMinuteRemaining += EvaOneMinuteRemaining;
			widget.OnTwoMinutesRemaining += EvaTwoMinutesRemaining;
			widget.OnThreeMinutesRemaining += EvaThreeMinutesRemaining;
			widget.OnFourMinutesRemaining += EvaFourMinutesRemaining;
			widget.OnFiveMinutesRemaining += EvaFiveMinutesRemaining;
			Ui.Root.AddChild(widget);
			Sound.Play("timergo1.aud");
		}

		void EvaTimerExpired() { Sound.Play("timerno1.aud"); }
		void EvaOneMinuteRemaining() { Sound.Play("1minr.aud"); }
		void EvaTwoMinutesRemaining() { Sound.Play("2minr.aud"); }
		void EvaThreeMinutesRemaining() { Sound.Play("3minr.aud"); }
		void EvaFourMinutesRemaining() { Sound.Play("4minr.aud"); }
		void EvaFiveMinutesRemaining() { Sound.Play("5minr.aud"); }
	}

	class CountdownTimerWidget : Widget
	{
		public string Header { get; set; }
		public int TicksLeft { get; set; }
		Rectangle renderBounds;
		int ticks;

		[ObjectCreator.UseCtor]
		public CountdownTimerWidget(string header, int ticksLeft, Rectangle renderBounds)
		{
			Header = header;
			TicksLeft = ticksLeft;
			this.renderBounds = renderBounds;
		}

		public event Action OnExpired;
		public event Action OnOneMinuteRemaining;
		public event Action OnTwoMinutesRemaining;
		public event Action OnThreeMinutesRemaining;
		public event Action OnFourMinutesRemaining;
		public event Action OnFiveMinutesRemaining;
		public event Action OnTenMinutesRemaining;
		public event Action OnTwentyMinutesRemaining;
		public event Action OnThirtyMinutesRemaining;
		public event Action OnFortyMinutesRemaining;

		const int Expired = 0;
		const int OneMinute = 1500;
		const int TwoMinutes = OneMinute * 2;
		const int ThreeMinutes = OneMinute * 3;
		const int FourMinutes = OneMinute * 4;
		const int FiveMinutes = OneMinute * 5;
		const int TenMinutes = OneMinute * 10;
		const int TwentyMinutes = OneMinute * 20;
		const int ThirtyMinutes = OneMinute * 30;
		const int FortyMinutes = OneMinute * 40;

		public override void Tick()
		{
			if (!IsVisible())
			{
				return;
			}
			ticks++;
			if (TicksLeft > 0)
			{
				TicksLeft--;
				switch (TicksLeft)
				{
					case Expired: OnExpired(); break;
					case OneMinute: OnOneMinuteRemaining(); break;
					case TwoMinutes: OnTwoMinutesRemaining(); break;
					case ThreeMinutes: OnThreeMinutesRemaining(); break;
					case FourMinutes: OnFourMinutesRemaining(); break;
					case FiveMinutes: OnFiveMinutesRemaining(); break;
					case TenMinutes: OnTenMinutesRemaining(); break;
					case TwentyMinutes: OnTwentyMinutesRemaining(); break;
					case ThirtyMinutes: OnThirtyMinutesRemaining(); break;
					case FortyMinutes: OnFortyMinutesRemaining(); break;
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
			var rb = renderBounds;
			Game.Debug(rb.ToString());
			font.DrawTextWithContrast(text, new float2((rb.Left + rb.Width) / 2f, (rb.Top + rb.Height) / 2f), TicksLeft == 0 && ticks % 60 >= 30 ? Color.Red : Color.White, Color.Black, 1);
		}
	}
}
