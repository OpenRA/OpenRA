using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Effects;

namespace OpenRA.Mods.RA.Crates
{
	class ExplodeCrateActionInfo : ITraitInfo
	{
		public string Weapon = null;
		public int SelectionShares = 5;

		public object Create(Actor self) { return new ExplodeCrateAction(self, this); }
	}

	class ExplodeCrateAction : ICrateAction
	{
		Actor self;
		ExplodeCrateActionInfo info;

		public ExplodeCrateAction(Actor self, ExplodeCrateActionInfo info)
		{
			this.self = self;
		}

		public int SelectionShares
		{
			get { return info.SelectionShares; }
		}

		public void Activate(Actor collector)
		{
			self.World.AddFrameEndTask(
				w => w.Add(new Bullet(info.Weapon, self.Owner,
					self, self.CenterLocation.ToInt2(), self.CenterLocation.ToInt2(),
					0, 0)));
		}
	}
}
