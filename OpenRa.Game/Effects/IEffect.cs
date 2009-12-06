using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Effects
{
	interface IEffect
	{
		void Tick();
		IEnumerable<Tuple<Sprite, float2, int>> Render();
	}
}
