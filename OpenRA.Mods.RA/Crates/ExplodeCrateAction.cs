using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Effects;

namespace OpenRA.Mods.RA
{
	class ExplodeCrateActionInfo : CrateActionInfo
	{
		public string Weapon = null;
		public override object Create(Actor self) { return new ExplodeCrateAction(self, this); }
	}

	class ExplodeCrateAction : CrateAction
	{
		public ExplodeCrateAction(Actor self, ExplodeCrateActionInfo info)
			: base(self, info) {}

		public override void Activate(Actor collector)
		{
			//self.World.AddFrameEndTask(
			//	w => w.Add(new Bullet((info as ExplodeCrateActionInfo).Weapon, self.Owner,
			//		self, self.CenterLocation.ToInt2(), self.CenterLocation.ToInt2(),
			//		0, 0)));
			base.Activate(collector);
		}
	}
}
