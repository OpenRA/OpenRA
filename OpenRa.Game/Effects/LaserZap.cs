using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Graphics;
using OpenRa.Traits;
using System.Drawing;

namespace OpenRa.Effects
{
	class LaserZap : IEffect
	{
		readonly int2 from, to;
		readonly int radius;
		int timeUntilRemove = 10; // # of frames
		int totalTime = 10;
		Color color;
		
		public LaserZap(int2 from, int2 to, int radius, Color color)
		{
			this.from = from;
			this.to = to;
			this.color = color;
			this.radius = radius;
		}

		public void Tick(World world)
		{
			if (timeUntilRemove <= 0)
				world.AddFrameEndTask(w => w.Remove(this));
			--timeUntilRemove;
		}

		public IEnumerable<Renderable> Render()
		{
			int alpha = (int)((1-(float)(totalTime-timeUntilRemove)/totalTime)*255);
			Color rc = Color.FromArgb(alpha,color);
			
			for (int i = -radius; i < radius; i++)
				Game.world.WorldRenderer.lineRenderer.DrawLine(from + new int2(i, 0), to + new int2(i, 0), rc, rc);
			
			yield break;
		}
	}
}
