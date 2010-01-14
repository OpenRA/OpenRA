using System;
using System.Linq;
using System.Collections.Generic;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

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
		public int Cash = 10000;
		public int Ore = 0;
		public int OreCapacity;
		public int DisplayCash = 0;
		public int PowerProvided = 0;
		public int PowerDrained = 0;

		public bool IsReady;

		public Shroud Shroud = new Shroud();
		public Dictionary<string, SupportPower> SupportPowers;

		public Player( int index, PaletteType palette, string playerName, Race race, string internalName )
		{
			Game.world.Add(this.PlayerActor = new Actor("Player", new int2(int.MaxValue, int.MaxValue), this));
			this.Index = index;
			this.Palette = palette;
			this.InternalName = internalName;
			this.PlayerName = playerName;
			this.Race = race;

			SupportPowers = Rules.SupportPowerInfo.ToDictionary( 
				spi => spi.Key, 
				spi => new SupportPower(spi.Key, spi.Value, this));
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
				var p = a.traits.Get<Building>().GetPowerUsage();
				if (p > 0)
					PowerProvided += p;
				else 
					PowerDrained -= p;
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
				.Select(a => a.Info.Traits.Get<StoresOreInfo>())
				.Sum(b => b.Capacity);
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

			foreach (var sp in SupportPowers.Values)
				sp.Tick();

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
