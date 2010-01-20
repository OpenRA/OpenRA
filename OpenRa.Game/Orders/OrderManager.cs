using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRa.Orders
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

		public IEnumerable<IOrderSource> Sources { get { return sources; } }

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

		void ProcessOrders(int frame, bool save)
		{
			var orders = sources
				.SelectMany(s => s.OrdersForFrame(frame))
				.SelectMany(x => x.ToOrderList())
				.OrderBy(o => o.Player.Index)
				.ToList();

			foreach (var o in orders)
				UnitOrders.ProcessOrder(o);

			if (save && savingReplay != null)
				savingReplay.WriteFrameData(orders, frame);
		}

		public void TickImmediate()
		{
			var localOrders = Game.controller.GetRecentOrders(true);
			if (localOrders.Count > 0)
				foreach (var p in sources)
					p.SendLocalOrders(0, localOrders);

			ProcessOrders(0, false);
		}

		public void Tick()
		{
			var localOrders = Game.controller.GetRecentOrders(false);

			foreach( var p in sources )
				p.SendLocalOrders( frameNumber + FramesAhead, localOrders );

			ProcessOrders(frameNumber, true);
			
			++frameNumber;

			// sanity check on the framenumber. This is 2^31 frames maximum, or multiple *years* at 40ms/frame.
			if( ( frameNumber & 0x80000000 ) != 0 )
				throw new InvalidOperationException( "(OrderManager) Frame number too large" );
		}
	}
}
