using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.Traits
{
	class RenderWarFactory : RenderBuilding
	{
		public Animation roof;
		bool doneBuilding;

		public RenderWarFactory(Actor self)
			: base(self)
		{
			roof = new Animation(self.unitInfo.Image ?? self.unitInfo.Name);
			anim.PlayThen("make", () =>
			{
				doneBuilding = true;
				anim.Play("idle");
				roof.Play("idle-top");
			});
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			if (doneBuilding)
				return base.Render(self).Concat(
					new[] { Pair.New(roof.Image, 24f * (float2)self.Location) });
			else
				return base.Render(self);
		}

		public override void Tick(Actor self, Game game, int dt)
		{
			base.Tick(self, game, dt);
			roof.Tick(dt);
		}
	}
}
