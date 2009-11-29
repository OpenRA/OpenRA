using System.Collections.Generic;
using System;

namespace OpenRa.Game
{
	class Player
	{
		public int Palette;
		public int Kills;
		public string PlayerName;
		public Race Race;
		public readonly int Index;
		public int Cash;
		public int DisplayCash;
		int powerProvided;
		int powerDrained;

		public Player( int index, int palette, string playerName, Race race )
		{
			this.Index = index;
			this.Palette = palette;
			this.PlayerName = playerName;
			this.Race = race;
			this.Cash = 10000;
			this.DisplayCash = 0;
			this.powerProvided = this.powerDrained = 0;

			foreach( var cat in Rules.Categories.Keys )
				ProductionInit( cat );
		}

		public void ChangePower(int dPower)
		{
			if (dPower > 0)
				powerProvided += dPower;
			if (dPower < 0)
				powerDrained -= dPower;
		}

		public int GetTotalPower()
		{
			return powerProvided - powerDrained;
		}

		public float GetSiloFullness()
		{
			return 0.5f;		/* todo: work this out the same way as RA */
		}

		public void GiveCash( int num )
		{
			Cash += num;
		}

		public bool TakeCash( int num )
		{
			if (Cash < num) return false;
			Cash -= num;
			return true;
		}

		const int displayCashDeltaPerFrame = 50;

		public void Tick()
		{
			foreach( var p in production )
				if( p.Value != null )
					p.Value.Tick( this );

			if (this == Game.LocalPlayer)
			{
				if (DisplayCash < Cash)
				{
					DisplayCash += Math.Min(displayCashDeltaPerFrame, 
						Cash - DisplayCash);
					Game.PlaySound("cashup1.aud", false);
				}
				else if (DisplayCash > Cash)
				{
					DisplayCash -= Math.Min(displayCashDeltaPerFrame,
						DisplayCash - Cash);
					Game.PlaySound("cashdn1.aud", false);
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
