using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class RenderUnitReload : RenderUnit
	{
		public RenderUnitReload(Actor self)
			: base(self) { }

		public override void Tick(Actor self)
		{
			base.Tick(self);
			var attack = self.traits.WithInterface<AttackBase>().FirstOrDefault();
			if (attack != null)
				anim.ReplaceAnim(attack.IsReloading() ? "empty-idle" : "idle");
		}
	}
}
