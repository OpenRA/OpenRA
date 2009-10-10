using System;
using System.Collections.Generic;
using System.Windows.Forms;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	class World
	{
		List<Actor> actors = new List<Actor>();
		List<Action<World>> frameEndActions = new List<Action<World>>();

		public readonly Game game;

		public World(Game game) { this.game = game; }

		public void Add(Actor a) { actors.Add(a); }
		public void Remove( Actor a ) { actors.Remove( a ); }
		public void AddFrameEndTask( Action<World> a ) { frameEndActions.Add( a ); }

		int lastTime = Environment.TickCount + 2000;

		public void Update()
		{
			int t = Environment.TickCount;
			int dt = t - lastTime;
			if( dt >= 40 )
			{
				lastTime += 40;

				foreach( Actor a in actors )
					a.Tick( game, 40 );

				Renderer.waterFrame += 0.05f;
			}

			foreach (Action<World> a in frameEndActions) a(this);
			frameEndActions.Clear();
		}

		public IEnumerable<Actor> Actors { get { return actors; } }
	}
}
