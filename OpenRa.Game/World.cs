using System;
using System.Collections.Generic;
using System.Windows.Forms;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class World
	{
		List<Actor> actors = new List<Actor>();
		List<IEffect> effects = new List<IEffect>();
		List<Action<World>> frameEndActions = new List<Action<World>>();
		int lastTime = Environment.TickCount;
		const int timestep = 40;

		public void Add(Actor a) { actors.Add(a); ActorAdded(a); }
		public void Remove(Actor a) { actors.Remove(a); ActorRemoved(a); }

		public void Add(IEffect b) { effects.Add(b); }
		public void Remove(IEffect b) { effects.Remove(b); }

		public void AddFrameEndTask( Action<World> a ) { frameEndActions.Add( a ); }

		public event Action<Actor> ActorAdded = _ => { };
		public event Action<Actor> ActorRemoved = a =>
		{
			a.Health = 0;		/* make sure everyone sees it as dead */
			foreach (var nr in a.traits.WithInterface<INotifyRemoved>())
				nr.Removed(a);
		};

		public void ResetTimer()
		{
			lastTime = Environment.TickCount;
		}

		public void Update()
		{
			int t = Environment.TickCount;
			int dt = t - lastTime;
			if (dt >= timestep)
			{
				lastTime += timestep;

				foreach (var a in actors) a.Tick();
				foreach (var e in effects) e.Tick();

				Renderer.waterFrame += 0.00125f * timestep;
				Game.viewport.Tick();
			}

			foreach (Action<World> a in frameEndActions) a(this);
			frameEndActions.Clear();
		}

		public IEnumerable<Actor> Actors { get { return actors; } }
		public IEnumerable<IEffect> Effects { get { return effects; } }

		uint nextAID = 0;
		internal uint NextAID()
		{
			return nextAID++;
		}
	}
}
