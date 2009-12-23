using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	enum PowerState { Normal, Low, Critical };

	class Player
	{
		public Actor PlayerActor;
		public PaletteType Palette;
		public int Kills;
		public string PlayerName;
		public string InternalName;
		public Race Race;
		public readonly int Index;
		public int Cash;
		public int Ore;
		public int OreCapacity;
		public int DisplayCash;
		public int PowerProvided;
		public int PowerDrained;

		public bool IsReady;

		public Player( Actor playerActor, int index, PaletteType palette, string playerName, Race race, string internalName )
		{
			this.PlayerActor = playerActor;
			this.Index = index;
			this.Palette = palette;
			this.InternalName = internalName;
			this.PlayerName = playerName;
			this.Race = race;
			this.Cash = 10000;
			this.Ore = 0;
			this.DisplayCash = 0;
			this.PowerProvided = this.PowerDrained = 0;
		}

		void UpdatePower()
		{
			var oldBalance = PowerProvided - PowerDrained;

			PowerProvided = 0;
			PowerDrained = 0;

			var myBuildings = Game.world.Actors
				.Where(a => a.Owner == this && a.traits.Contains<Building>());

			foreach (var a in myBuildings)
			{
				var bi = a.Info as BuildingInfo;
				if (bi.Power > 0)		/* todo: is this how real-ra scales it? */
					PowerProvided += (a.Health * bi.Power) / bi.Strength;
				else 
					PowerDrained -= bi.Power;
			}

			if (PowerProvided - PowerDrained < 0)
				if (PowerProvided - PowerDrained  != oldBalance)
					GiveAdvice("lopower1.aud");
		}
				
		public float GetSiloFullness()
		{
			return (float)Ore / OreCapacity;
		}

		public PowerState GetPowerState()
		{
			if (PowerProvided >= PowerDrained) return PowerState.Normal;
			if (PowerProvided > PowerDrained / 2) return PowerState.Low;
			return PowerState.Critical;
		}

		void UpdateOreCapacity()
		{
			OreCapacity = Game.world.Actors
				.Where(a => a.Owner == this && a.traits.Contains<StoresOre>())
				.Select(a => a.Info as BuildingInfo)
				.Where(b => b != null)
				.Sum(b => b.Storage);
		}

		void GiveAdvice(string advice)
		{
			if (this != Game.LocalPlayer) return;
			// todo: store the condition or something.
			// repeat after Rules.General.SpeakDelay, as long as the condition holds.
			Sound.Play(advice);
		}

		public void GiveCash( int num ) { Cash += num; }
		public void GiveOre(int num)
		{
			Ore += num;

			if (Ore > OreCapacity)
				Ore = OreCapacity;		// trim off the overflow.

			if (Ore > .8 * OreCapacity)
				GiveAdvice("silond1.aud");		// silos needed
		}

		public bool TakeCash( int num )
		{
			if (Cash + Ore < num) return false;
			if (Ore <= num)
			{
				num -= Ore;
				Ore = 0;
				Cash -= num;
			}
			else
				Ore -= num;
			
			return true;
		}

		const int displayCashDeltaPerFrame = 50;

		public void Tick()
		{
			UpdatePower();
			UpdateOreCapacity();

			if (this == Game.LocalPlayer)
			{
				var totalMoney = Cash + Ore;

				if (DisplayCash < totalMoney)
				{
					DisplayCash += Math.Min(displayCashDeltaPerFrame,
						totalMoney - DisplayCash);
					Sound.Play("cashup1.aud");
				}
				else if (DisplayCash > totalMoney)
				{
					DisplayCash -= Math.Min(displayCashDeltaPerFrame,
						DisplayCash - totalMoney);
					Sound.Play("cashdn1.aud");
				}
			}
		}
	}
}
