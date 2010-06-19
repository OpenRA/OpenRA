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
		public override object Create(ActorInitializer init) { return new ExplodeCrateAction(init.self, this); }
	}

	class ExplodeCrateAction : CrateAction
	{
		public ExplodeCrateAction(Actor self, ExplodeCrateActionInfo info)
			: base(self, info) {}

		public override void Activate(Actor collector)
		{
			Combat.DoExplosion(self, (info as ExplodeCrateActionInfo).Weapon, collector.CenterLocation.ToInt2(), 0);
			base.Activate(collector);
		}
	}
}
