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
		readonly Game game;
		int lastTime = Environment.TickCount;
		const int timestep = 40;

		public World(Game game) { this.game = game; }

		public void Add(Actor a) { actors.Add(a); }
		public void Remove( Actor a ) { actors.Remove( a ); }
		public void AddFrameEndTask( Action<World> a ) { frameEndActions.Add( a ); }

		public void Update()
		{
			int t = Environment.TickCount;
			int dt = t - lastTime;
			if( dt >= timestep )
			{
				lastTime += timestep;

				foreach( Actor a in actors )
					a.Tick(game, timestep);

				Renderer.waterFrame += 0.00125f * timestep;
			}

			foreach (Action<World> a in frameEndActions) a(this);
			frameEndActions.Clear();
		}

		public IEnumerable<Actor> Actors { get { return actors; } }
	}
}
