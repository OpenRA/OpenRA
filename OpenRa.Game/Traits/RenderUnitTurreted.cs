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
			turretAnim.PlayFetchIndex("turret", () => self.traits.Get<Turreted>().turretFacing / 8);
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			var mobile = self.traits.Get<Mobile>();

			yield return Centered(anim.Image, self.CenterLocation);
			yield return Centered(turretAnim.Image, self.CenterLocation 
				+ Util.GetTurretPosition(self, self.unitInfo.PrimaryOffset).ToFloat2());
			if (self.unitInfo.SecondaryOffset != null)
				yield return Centered(turretAnim.Image, self.CenterLocation
					+ Util.GetTurretPosition(self, self.unitInfo.SecondaryOffset).ToFloat2());
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			turretAnim.Tick();
		}
	}
}
