using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRa.Game
{
	class OrderManager
	{
		Stream savingReplay;
		List<IOrderSource> sources;
		int frameNumber = 0;

		public int FramesAhead = 3;

		public bool GameStarted { get { return frameNumber != 0; } }
		public bool IsNetplay { get { return sources.OfType<NetworkOrderSource>().Any(); } }

		public void StartGame()
		{
			if (GameStarted) return;

			frameNumber = 1;
			foreach (var p in this.sources)
				for (int i = frameNumber; i <= FramesAhead; i++)
					p.SendLocalOrders(i, new List<Order>());
		}

		public int FrameNumber { get { return frameNumber; } }

		public OrderManager( IEnumerable<IOrderSource> sources )
		{
			this.sources = sources.ToList();
			if (!IsNetplay)
				StartGame();
		}

		public OrderManager( IEnumerable<IOrderSource> sources, string replayFilename )
			: this( sources )
		{
			savingReplay = new FileStream( replayFilename, FileMode.Create );
		}

		public bool IsReadyForNextFrame
		{
			get
			{
				foreach( var p in sources )
					if( !p.IsReadyForFrame( frameNumber ) )
						return false;
				return true;
			}
		}

		public void TickImmediate()
		{
			var localOrders = Game.controller.GetRecentOrders(true);
			if (localOrders.Count > 0)
				foreach (var p in sources)
					p.SendLocalOrders(0, localOrders);

			var immOrders = sources.SelectMany( p => p.OrdersForFrame(0) ).OrderBy(o => o.Player.Index).ToList();
			foreach (var order in immOrders)
				UnitOrders.ProcessOrder(order);
		}

		public void Tick()
		{
			var localOrders = Game.controller.GetRecentOrders(false);

			foreach( var p in sources )
				p.SendLocalOrders( frameNumber + FramesAhead, localOrders );

			var allOrders = sources.SelectMany(p => p.OrdersForFrame(frameNumber)).OrderBy(o => o.Player.Index).ToList();

			foreach (var order in allOrders)
				UnitOrders.ProcessOrder(order);

			if( savingReplay != null )
				savingReplay.WriteFrameData( allOrders, frameNumber );

			++frameNumber;

			// sanity check on the framenumber. This is 2^31 frames maximum, or multiple *years* at 40ms/frame.
			if( ( frameNumber & 0x80000000 ) != 0 )
				throw new InvalidOperationException( "(OrderManager) Frame number too large" );
		}
	}
}
