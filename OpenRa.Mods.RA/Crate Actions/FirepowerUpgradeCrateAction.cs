using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Traits;
using OpenRa.Mods.RA.Effects;

namespace OpenRa.Mods.RA
{
	class FirepowerUpgradeCrateActionInfo : ITraitInfo
	{
		public float Multiplier = 2.0f;
		public int SelectionShares = 10;
		public object Create(Actor self) { return new FirepowerUpgradeCrateAction(self); }
	}

	class FirepowerUpgradeCrateAction : ICrateAction
	{
		Actor self;
		public FirepowerUpgradeCrateAction(Actor self)
		{
			this.self = self;
		}

		public int SelectionShares
		{
			get { return self.Info.Traits.Get<FirepowerUpgradeCrateActionInfo>().SelectionShares; }
		}

		public void Activate(Actor collector)
		{
			Sound.PlayToPlayer(collector.Owner, "firepo1.aud");
			collector.World.AddFrameEndTask(w =>
			{
				var multiplier = self.Info.Traits.Get<FirepowerUpgradeCrateActionInfo>().Multiplier;
				collector.traits.Add(new FirepowerUpgrade(multiplier));
				w.Add(new CrateEffect(collector, "fpower"));
			});
		}
	}

	class FirepowerUpgrade : IFirepowerModifier
	{
		float multiplier;
		public FirepowerUpgrade(float multiplier) { this.multiplier = multiplier; }
		public float GetFirepowerModifier() { return multiplier; }
	}
}
