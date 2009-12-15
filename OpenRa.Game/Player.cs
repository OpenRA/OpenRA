using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	enum PowerState { Normal, Low, Critical };

	class Player
	{
		public int Palette;
		public int Kills;
		public string PlayerName;
		public Race Race;
		public readonly int Index;
		public int Cash;
		public int Ore;
		public int DisplayCash;
		public int powerProvided;
		public int powerDrained;

		public bool IsReady;

		public Player( int index, int palette, string playerName, Race race )
		{
			this.Index = index;
			this.Palette = palette;
			this.PlayerName = playerName;
			this.Race = race;
			this.Cash = 10000;
			this.Ore = 0;
			this.DisplayCash = 0;
			this.powerProvided = this.powerDrained = 0;

			foreach( var cat in Rules.Categories.Keys )
				ProductionInit( cat );
		}

		void UpdatePower()
		{
			var oldBalance = powerProvided - powerDrained;

			powerProvided = 0;
			powerDrained = 0;

			var myBuildings = Game.world.Actors
				.Where(a => a.Owner == this && a.traits.Contains<Building>());

			foreach (var a in myBuildings)
			{
				var bi = a.Info as BuildingInfo;
				if (bi.Power > 0)		/* todo: is this how real-ra scales it? */
					powerProvided += (a.Health * bi.Power) / bi.Strength;
				else 
					powerDrained -= bi.Power;
			}

			if (powerProvided - powerDrained < 0)
				if (powerProvided - powerDrained  != oldBalance)
					GiveAdvice("lopower1.aud");
		}

		public float GetSiloFullness()
		{
			return (float)Ore / GetOreCapacity();
		}

		public PowerState GetPowerState()
		{
			if (powerProvided >= powerDrained) return PowerState.Normal;
			if (powerProvided > powerDrained / 2) return PowerState.Low;
			return PowerState.Critical;
		}

		public int GetOreCapacity()
		{
			return Game.world.Actors
				.Where(a => a.Owner == this)
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

			var capacity = GetOreCapacity();
			if (Ore > capacity)
				Ore = capacity;		// trim off the overflow.

			if (Ore > .8 * capacity)
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

			foreach( var p in production )
				if( p.Value != null )
					p.Value.Tick( this );

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

		// Key: Production category. Categories are: Building, Infantry, Vehicle, Ship, Plane (and one per super, if they're done in here)
		readonly Dictionary<string, ProductionItem> production = new Dictionary<string, ProductionItem>();

		public void ProductionInit( string category )
		{
			production.Add( category, null );
		}

		public ProductionItem Producing( string category )
		{
			return production[ category ];
		}

		public void CancelProduction( string category )
		{
			var item = production[ category ];
			if( item == null ) return;
			GiveCash( item.TotalCost - item.RemainingCost ); // refund what's been paid so far.
			FinishProduction( category );
		}

		public void FinishProduction( string category )
		{
			production[ category ] = null;
		}

		public void BeginProduction( string group, ProductionItem item )
		{
			if( production[ group ] != null ) return;
			production[ group ] = item;
		}
	}
}
