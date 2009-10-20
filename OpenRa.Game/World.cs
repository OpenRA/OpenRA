using System;
using System.Collections.Generic;
using System.Windows.Forms;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	class World
	{
		List<Actor> actors = new List<Actor>();
		List<IEffect> bullets = new List<IEffect>();
		List<Action<World>> frameEndActions = new List<Action<World>>();
		readonly Game game;
		int lastTime = Environment.TickCount;
		const int timestep = 40;

		public World(Game game) { this.game = game; }

		public void Add(Actor a) { actors.Add(a); ActorAdded(a); }
		public void Remove(Actor a) { actors.Remove(a); ActorRemoved(a); }

		public void Add(Bullet b) { bullets.Add(b); }
		public void Remove(Bullet b) { bullets.Remove(b); }

		public void AddFrameEndTask( Action<World> a ) { frameEndActions.Add( a ); }

		public event Action<Actor> ActorAdded = _ => { };
		public event Action<Actor> ActorRemoved = _ => { };

		public void ResetTimer()
		{
			lastTime = Environment.TickCount;
		}

		public void Update()
		{
			int t = Environment.TickCount;
			int dt = t - lastTime;
			if( dt >= timestep )
			{
				lastTime += timestep;

				foreach( var a in actors )
					a.Tick(game);
				foreach (var b in bullets)
					b.Tick(game);

				Renderer.waterFrame += 0.00125f * timestep;
			}

			foreach (Action<World> a in frameEndActions) a(this);
			frameEndActions.Clear();
		}

		public IEnumerable<Actor> Actors { get { return actors; } }
		public IEnumerable<IEffect> Bullets { get { return bullets; } }
	}
}
