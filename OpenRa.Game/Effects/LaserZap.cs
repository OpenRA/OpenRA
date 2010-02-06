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
			
			float2 unit = 1.0f/(from - to).Length*(from - to).ToFloat2();
			float2 norm = new float2(-unit.Y, unit.X);
			
			for (int i = -radius; i < radius; i++)
				Game.world.WorldRenderer.lineRenderer.DrawLine(from + i * norm, to + i * norm, rc, rc);
			
			yield break;
		}
	}
}
