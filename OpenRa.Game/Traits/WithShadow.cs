using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class WithShadow : IRenderModifier
	{
		public WithShadow(Actor self) {}

		public IEnumerable<Tuple<Sprite, float2, int>> ModifyRender(Actor self, IEnumerable<Tuple<Sprite, float2, int>> r)
		{
			var unit = self.traits.Get<Unit>();
			var shadowSprites = r.Select( a => Tuple.New( a.a, a.b, 8 ));
			var flyingSprites = r.Select( a => Tuple.New( a.a, a.b - new float2( 0, unit.Altitude ), a.c ));
			return shadowSprites.Concat(flyingSprites);
		}
	}
}
