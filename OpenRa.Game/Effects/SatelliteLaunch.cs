using System.Collections.Generic;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Effects
{
	class SatelliteLaunch : IEffect
	{
		int frame = 0;
		Actor a;
		Animation doors = new Animation("atek");
		float2 doorOffset = new float2(-4,0);

		public SatelliteLaunch(Actor a)
		{
			this.a = a;
			doors.PlayThen("active",
				() => Game.world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick()
		{
			doors.Tick();
			
			if (++frame == 19)
			{
				Game.world.AddFrameEndTask(w => w.Add(new GpsSatellite(a.CenterLocation - .5f * doors.Image.size + doorOffset)));
			}
		}
		
		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(doors.Image,
				a.CenterLocation - .5f * doors.Image.size + doorOffset, PaletteType.Gold);
		}
	}
}
