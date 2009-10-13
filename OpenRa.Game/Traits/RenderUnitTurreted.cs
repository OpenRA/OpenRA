using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.Traits
{
	class RenderUnitTurreted : RenderUnit
	{
		public Animation turretAnim;

		public RenderUnitTurreted(Actor self)
			: base(self)
		{
			turretAnim = new Animation(self.unitInfo.Name);
			turretAnim.PlayFetchIndex("turret", () => self.traits.Get<Turreted>().turretFacing);
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			var mobile = self.traits.Get<Mobile>();
			yield return Centered(anim.Image, self.CenterLocation);
			yield return Centered(turretAnim.Image, self.CenterLocation);
		}

		public override void Tick(Actor self, Game game, int dt)
		{
			base.Tick(self, game, dt);
			turretAnim.Tick(dt);
		}
	}
}
