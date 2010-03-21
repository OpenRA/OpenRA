using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Effects;

namespace OpenRA.Mods.RA
{
	class ExplodeCrateActionInfo : ITraitInfo
	{
		public string Weapon = null;
		public int SelectionShares = 5;
		public string Effect = null;
		public string Notification = null;
		public object Create(Actor self) { return new ExplodeCrateAction(self, this); }
	}

	class ExplodeCrateAction : ICrateAction
	{
		Actor self;
		ExplodeCrateActionInfo info;

		public ExplodeCrateAction(Actor self, ExplodeCrateActionInfo info)
		{
			this.self = self;
			this.info = info;
		}

		public int GetSelectionShares(Actor collector)
		{
			return info.SelectionShares;
		}

		public void Activate(Actor collector)
		{
			Sound.PlayToPlayer(collector.Owner, self.Info.Traits.Get<ExplodeCrateActionInfo>().Notification);

			self.World.AddFrameEndTask(
				w => w.Add(new Bullet(info.Weapon, self.Owner,
					self, self.CenterLocation.ToInt2(), self.CenterLocation.ToInt2(),
					0, 0)));
		}
	}
}
