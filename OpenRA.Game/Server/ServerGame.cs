using System.Diagnostics;

namespace OpenRA.Server
{
	public class ServerGame
	{
		const int JankThreshold = 250;

		Stopwatch gameTimer;
		public long RunTime
		{
			get { return gameTimer.ElapsedMilliseconds; }
		}

		public readonly OrderBuffer OrderBuffer;
		public int CurrentNetFrame { get; protected set; }
		public long NextFrameTick { get; protected set; }
		public int NetTimestep { get; protected set; }

		public int MillisToNextNetFrame
		{
			get { return (int)(NextFrameTick - RunTime); }
			set { NextFrameTick = RunTime + value; }
		}

		public ServerGame(int worldTimeStep)
		{
			CurrentNetFrame = 1;
			NetTimestep = worldTimeStep * Game.NetTickScale;
			NextFrameTick = NetTimestep;
			gameTimer = Stopwatch.StartNew();
			OrderBuffer = new OrderBuffer();
		}

		public void TryTick(IFrameOrderDispatcher dispatcher)
		{
			var now = RunTime;
			if (now >= NextFrameTick)
			{
				OrderBuffer.DispatchOrders(dispatcher);

				CurrentNetFrame++;
				if (now - NextFrameTick > JankThreshold)
					NextFrameTick = now + NetTimestep;
				else
					NextFrameTick += NetTimestep;
			}
		}
	}
}
