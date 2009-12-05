using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	interface IEffect
	{
		void Tick();
		IEnumerable<Tuple<Sprite, float2, int>> Render();
		Player Owner { get; }
	}
}
