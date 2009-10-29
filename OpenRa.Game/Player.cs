using System;
using System.Collections.Generic;

namespace OpenRa.Game
{
	class Player
	{
		public int Palette;
		public int Kills;
		public string PlayerName;
		public TechTree.TechTree TechTree = new OpenRa.TechTree.TechTree();

		public Player( int palette, string playerName, OpenRa.TechTree.Race race )
		{
			this.Palette = palette;
			this.PlayerName = playerName;
			TechTree.CurrentRace = race;
		}

		public float GetSiloFullness()
		{
			return 0.5f;		/* todo: work this out the same way as RA */
		}

		public void GiveCash( int num )
		{
			// TODO: increase cash
		}

		public bool TakeCash( int num )
		{
			// TODO: decrease cash.
			// returns: if enough cash was available, true
			return true;
		}

		public void Tick()
		{
			foreach( var p in production )
			{
				if( p.Value != null )
					p.Value.Tick( this );
			}
		}

		// Key: Production category. Categories are: building, infantry, vehicle, boat, plane (and one per super, if they're done in here)
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
			production[ category ] = null;
		}

		public void BeginProduction( string group, ProductionItem item )
		{
			if( production[ group ] != null ) return;
			production[ group ] = item;
		}
	}

	class ProductionItem
	{
		public readonly string Item;

		public readonly int TotalTime;
		public readonly int TotalCost;
		public int RemainingTime { get; private set; }
		public int RemainingCost { get; private set; }

		public bool Paused = false, Done = false;

		public ProductionItem( string item, int time, int cost )
		{
			Item = item;
			RemainingTime = TotalTime = time;
			RemainingCost = TotalCost = cost;
		}

		public void Tick( Player player )
		{
			if( Paused || Done ) return;

			var costThisFrame = RemainingCost / RemainingTime;
			if( costThisFrame == 0 && !player.TakeCash( costThisFrame ) ) return;

			RemainingCost -= costThisFrame;
			RemainingTime -= 1;
			if( RemainingTime > 0 ) return;

			// item finished; do whatever needs done.
			Done = true;
		}
	}
}
