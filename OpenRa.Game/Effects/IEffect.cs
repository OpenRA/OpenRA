using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	public interface IEffect
	{
		void Tick( World world );
		IEnumerable<Renderable> Render();
	}
}
