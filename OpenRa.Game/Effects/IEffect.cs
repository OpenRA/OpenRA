using System.Collections.Generic;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Effects
{
	interface IEffect
	{
		void Tick();
		IEnumerable<Renderable> Render();
	}
}
